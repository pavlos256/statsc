//
// Author(s):
//     Pavlos Touboulidis <pav@pav.gr>
//
// Created on 2013-9-10
//
using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Text;

namespace tests
{
	public class UdpListener
	{
		Socket socket;
		byte[] buffer;
		List<string> inbox;
		object inboxLock = new object();
		volatile bool stop;

		public UdpListener(int port, int socketReceiveBufferSize)
		{
			this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			this.socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
			this.socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);

			this.buffer = new byte[socketReceiveBufferSize];
		}

		public void Start()
		{
			this.stop = false;
			this.inbox = new List<string>();
			ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadProc), this);
		}

		public void Stop()
		{
			this.stop = true;
			try
			{
				this.socket.Close();
			}
			catch
			{
			}
		}

		public List<string> GetMessages()
		{
			lock (this.inboxLock)
			{
				var old = this.inbox;
				this.inbox = new List<string>();
				return old;
			}
		}

		static void ThreadProc(object stateInfo)
		{
			var listener = (UdpListener)stateInfo;

			while (!listener.stop)
			{
				try
				{
					int len = listener.socket.Receive(listener.buffer);
					if (len == 0)
						break;
					
					string s = Encoding.UTF8.GetString(listener.buffer, 0, len);
					lock (listener.inboxLock)
					{
						listener.inbox.Add(s);
					}
				}
				catch (SocketException ex)
				{
					Console.WriteLine("UdpListener: " + ex.GetType().Name + ": " + ex.Message);
					break;
				}
				catch (ObjectDisposedException ex)
				{
					Console.WriteLine("UdpListener: " + ex.GetType().Name + ": " + ex.Message);
					break;
				}
				catch (Exception ex)
				{
					Console.WriteLine("UdpListener: " + ex.GetType().Name + ": " + ex.Message);
					break;
				}
			}
		}
	}
}

