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

		public new bool Send(ArraySegment<byte> buffer, object token)
		{
			try
			{
				return base.Send(buffer, token);
			}
			catch
			{
				// Shouldn't happen because "base.Send" takes care of possible thrown exceptions
				return false;
			}
		}

		protected override void OnDataSent(int bytes, object userToken)
		{
			var buffer = (ArraySegment<byte>)userToken;
			this.pool.CheckIn(buffer);
		}
	}
}

