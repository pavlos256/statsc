// 
// Author(s):
//     Pavlos Touboulidis <pav@pav.gr>
// 
// Created on 2011-11-25
// 
using System;
using System.Net;

namespace statsc.Udp
{
	/// <summary>
	/// Holds the options used by the <see cref="Client"/>.
	/// </summary>
	public class ClientOptions
	{
		/// <summary>
		/// The practical maximum for UDP is 65535 - 8 (UDP header) - 20 (IP header) = 65507.
		///
		/// IPv4 and IPv6 define minimum reassembly buffer size, the minimum datagram size
		/// that we are guaranteed any implementation must support.
		/// For IPv4, this is 576 bytes. IPv6 raises this to 1,500 bytes.
		///
		/// So the largest safe datagram on the internet is 576 - 28 = 548.
		/// However:
		/// "It is true that a typical IPv4 header is 20 bytes, and the UDP header is 8 bytes.
		/// However it is possible to include IP options which can increase the size of the
		/// IP header to as much as 60 bytes. In addition, sometimes it is necessary for
		/// intermediate nodes to encapsulate datagrams inside of another protocol such as
		/// IPsec (used for VPNs and the like) in order to route the packet to its destination.
		/// So if you do not know the MTU on your particular network path, it is best to leave
		/// a reasonable margin for other header information that you may not have anticipated.
		/// A 512-byte UDP payload is generally considered to do that, although even that does
		/// not leave quite enough space for a maximum size IP header.
		/// 
		/// Ethernet connections (like Intranets) may use higher MTU:
		///  * Fast ethernet: 1432
		///  * Gigabit ethernet: 8932 (Jumbo frames)
		///  * Internet: 512
		/// </summary>
		public const int DefaultReceiveBufferSize = 512;

		/// <summary>
		/// This is the maximum datagram size we support. The default is 512, the maximum is 65507.
		/// </summary>
		/// <see cref="DefaultReceiveBufferSize"/>
		public int SocketReceiveBufferSize { get; private set; }
		
		/// <summary>
		/// Initializes a new instance of the <see cref="ClientOptions"/> class.
		/// </summary>
		/// <param name='receiveBufferSize'>
		/// The size of the receive buffer. It is recommended to use the default value,
		/// <see cref="DefaultReceiveBufferSize"/>.
		/// </param>
		public ClientOptions(int receiveBufferSize = DefaultReceiveBufferSize)
		{
			this.SocketReceiveBufferSize = receiveBufferSize;
		}
	}
}

