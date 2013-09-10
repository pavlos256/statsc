//
// Author(s):
//     Pavlos Touboulidis <pav@pav.gr>
//
// Created on 2013-9-10
//
using System;
using NUnit.Framework;
using System.Threading;
using System.Collections.Generic;

namespace tests
{
	[TestFixture()]
	public class Test
	{
		const int MaxUdpPacketSize = 512;
		const int Port = 45678;
		const string NS = "tests";

		UdpListener server;
		statsc.Client client;

		[SetUp]
		public void Setup()
		{
			Console.WriteLine("Setting up...");
			server = new UdpListener(Port, MaxUdpPacketSize);
			server.Start();
			client = new statsc.Client(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, Port), NS, MaxUdpPacketSize);
			Console.WriteLine("Set up done.");
		}

		[TearDown]
		public void TearDown()
		{
			Console.WriteLine("Tearing down...");
			client.Close();
			server.Stop();
			Console.WriteLine("Tear down done.");
		}

		void SendStuff()
		{
			client.Counter("counter1", 5);
			client.Counter("counter2", 1, 0.5d);
			Thread.Sleep(50);

			client.Gauge("gauge1", ulong.MaxValue);
			client.GaugeDelta("gauge1", "-", 10);
			Thread.Sleep(50);

			client.Meter("meter1");
			client.Meter("meter1");
			Thread.Sleep(50);

			client.Set("set1", "something");
			client.Set("set1", "else");
			Thread.Sleep(50);

			client.Timer("timer1", TimeSpan.FromMilliseconds(10));
			client.Timer("timer1", TimeSpan.FromMilliseconds(20));
		}

		void TestStuff(List<string> messages)
		{
			Assert.IsNotNull(messages);
			Assert.AreEqual(10, messages.Count, "Received messages count differs");

			int i = 0;
			Assert.AreEqual(NS + ".counter1:5|c", messages[i], string.Format("Message {0} differs", ++i));
			Assert.AreEqual(NS + ".counter2:1|c@0.5", messages[i], string.Format("Message {0} differs", ++i));

			Assert.AreEqual(NS + ".gauge1:" + ulong.MaxValue.ToString() + "|g", messages[i], string.Format("Message {0} differs", ++i));
			Assert.AreEqual(NS + ".gauge1:-10|g", messages[i], string.Format("Message {0} differs", ++i));

			Assert.AreEqual(NS + ".meter1:1|m", messages[i], string.Format("Message {0} differs", ++i));
			Assert.AreEqual(NS + ".meter1:1|m", messages[i], string.Format("Message {0} differs", ++i));

			Assert.AreEqual(NS + ".set1:something|s", messages[i], string.Format("Message {0} differs", ++i));
			Assert.AreEqual(NS + ".set1:else|s", messages[i], string.Format("Message {0} differs", ++i));

			Assert.AreEqual(NS + ".timer1:10|ms", messages[i], string.Format("Message {0} differs", ++i));
			Assert.AreEqual(NS + ".timer1:20|ms", messages[i], string.Format("Message {0} differs", ++i));
		}

		[Test()]
		public void Test1Simple()
		{
			server.GetMessages();

			SendStuff();

			Thread.Sleep(500);

			TestStuff(server.GetMessages());
		}

		[Test()]
		public void Test2Batch()
		{
			server.GetMessages();

			client.SetBatching(TimeSpan.FromMilliseconds(25));

			SendStuff();

			client.SetBatching(TimeSpan.Zero);

			Thread.Sleep(500);

			var list = server.GetMessages();

			Assert.IsNotNull(list);
			Assert.AreEqual(5, list.Count, "Received messages count differs");


			List<string> split = new List<string>();
			int i = 0;
			var parts = list[i++].Split('\n');
			Assert.AreEqual(3, parts.Length, string.Format("Parts length differs at batch {0}", i));
			split.AddRange(parts);
			
			parts = list[i++].Split('\n');
			Assert.AreEqual(2, parts.Length, string.Format("Parts length differs at batch {0}", i));
			split.AddRange(parts);
			
			parts = list[i++].Split('\n');
			Assert.AreEqual(2, parts.Length, string.Format("Parts length differs at batch {0}", i));
			split.AddRange(parts);
			
			parts = list[i++].Split('\n');
			Assert.AreEqual(2, parts.Length, string.Format("Parts length differs at batch {0}", i));
			split.AddRange(parts);
			
			parts = list[i++].Split('\n');
			Assert.AreEqual(1, parts.Length, string.Format("Parts length differs at batch {0}", i));
			split.AddRange(parts);

			TestStuff(split);
		}
		
		[Test()]
		public void Test3BatchTooLarge()
		{
			server.GetMessages();

			client.SetBatching(TimeSpan.FromMilliseconds(1000));

			SendStuff();
			SendStuff();
			SendStuff();

			client.SetBatching(TimeSpan.Zero);

			Thread.Sleep(500);

			var list = server.GetMessages();

			Assert.IsNotNull(list);
			Assert.AreEqual(2, list.Count, "Received messages count differs");


			List<string> split = new List<string>();
			int i = 0;
			var parts = list[i++].Split('\n');
			Assert.AreEqual(23, parts.Length, string.Format("Parts length differs at batch {0}", i));
			split.AddRange(parts);

			parts = list[i++].Split('\n');
			Assert.AreEqual(7, parts.Length, string.Format("Parts length differs at batch {0}", i));
			split.AddRange(parts);

			TestStuff(split.GetRange(0, 10));
			TestStuff(split.GetRange(10, 10));
			TestStuff(split.GetRange(20, 10));
		}
	}
}
