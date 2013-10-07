//
// Author(s):
//     Pavlos Touboulidis <pav@pav.gr>
//
// Created on 2013-9-10
//
using System;

namespace statsc
{
	internal class UdpClient : Udp.Client
	{
		BufferPool pool;

		public UdpClient(int maxPayloadLength, BufferPool pool)
			: base(new Udp.ClientOptions(maxPayloadLength))
		{
			this.pool = pool;
		}

		public void Send(ArraySegment<byte> exactBuffer, ArraySegment<byte> poolBuffer)
		{
			try
			{
				if (!base.Send(exactBuffer, poolBuffer))
					this.pool.CheckIn(poolBuffer);
			}
			catch
			{
				// Shouldn't happen because "base.Send" takes care of possible thrown exceptions
			}
		}

		protected override void OnSendCompleted(int bytes, System.Net.Sockets.SocketError result, object userToken)
		{
			var poolBuffer = (ArraySegment<byte>)userToken;
			this.pool.CheckIn(poolBuffer);
		}
	}
}

