// 
// Author(s):
//     Pavlos Touboulidis <pav@pav.gr>
// 
// Created on 2011-11-25
// 
using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;

namespace statsc.Udp
{
	/// <summary>
	/// A generic UDP/IP client helper.
	/// </summary>
	public class Client
	{
		/// <summary>The socket.</summary>
		protected Socket Socket { get; private set; }
		/// <summary>The client's options.</summary>
		protected ClientOptions Options { get; private set; }
		private object socketLock = new object();

		/// <summary>
		/// Initializes a new instance of the <see cref="UdpClient"/> class.
		/// </summary>
		/// <param name='options'>
		/// Options.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown when an argument passed to a method is invalid because it is <see langword="null" /> .
		/// </exception>
		public Client(ClientOptions options)
		{
			if (options == null)
				throw new ArgumentNullException("options");
			this.Options = options;
		}
		
		/// <summary>
		/// Connects to the specified <paramref name="remoteEndPoint"/> (both <see cref="IPEndPoint"/> and <see cref="DnsEndPoint"/> are supported).
		/// </summary>
		/// <param name='remoteEndPoint'>
		/// The <see cref="IPEndPoint"/> or <see cref="DnsEndPoint"/> to connect to.
		/// </param>
		/// <exception cref="InvalidOperationException">
		/// Is thrown if called while the underlying socket is busy (connected or trying to connect).
		/// </exception>
		public void Connect(EndPoint remoteEndPoint)
		{
			// HACK: Blocking dns resolve
			// This does not work on Mono 2.10.8 when remoteEndPoint is a DnsEndPoint.
			// It throws NotImplementedException at Connect -> remoteEndPoint.Serialize
			{
				var dep = remoteEndPoint as DnsEndPoint;
				if (dep != null)
				{
					// Throws SocketException if not found
					var addresses = Dns.GetHostAddresses(dep.Host);
					remoteEndPoint = new IPEndPoint(addresses[0], dep.Port);
				}
			}

			Socket socket = null;

			if (this.Socket == null)
			{
				lock (this.socketLock)
				{
					if (this.Socket == null)
					{
						this.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
						socket = this.Socket;
						socket.SendBufferSize = this.Options.SocketReceiveBufferSize;
					}
				}
			}
			
			if (socket == null)
				throw (new InvalidOperationException("Socket is busy."));

			try
			{
				socket.Bind(new IPEndPoint(IPAddress.Any, 0));
				socket.Connect(remoteEndPoint);

				OnConnected();
			}
			catch
			{
				socket.Close();
				lock (this.socketLock)
				{
					this.Socket = null;
				}
				throw;
			}
		}

		/// <summary>
		/// Sends data.
		/// </summary>
		/// <param name="buffer">A byte array that contains the data to be sent.</param>
		/// <param name="offset">The zero-based position in the buffer parameter at which to begin sending data.</param>
		/// <param name="size">The number of bytes to send.</param>
		public bool Send(byte[] buffer, int offset, int size)
		{
			try
			{
				var socket = this.Socket;
				if (socket == null)
					return false;

				SocketError errorCode;
				if (socket.Send(buffer, offset, size, SocketFlags.None, out errorCode) != size)
					return false;
				if (errorCode != SocketError.Success)
					return false;

				return true;
			}
			catch (SocketException)
			{
				Close();
			}
			catch (ObjectDisposedException)
			{
				Close();
			}
			return false;
		}

		/// <summary>
		/// Sends data.
		/// </summary>
		/// <param name="buffer">The data to send.</param>
		public bool Send(ArraySegment<byte> buffer)
		{
			return this.Send(buffer.Array, buffer.Offset, buffer.Count);
		}

		/// Sends data.
		/// <returns>Returns <c>true</c> on success, <c>false</c> on error.</returns>
		/// <param name='buffers'>The data to send.</param>
		public bool Send(IList<ArraySegment<byte>> buffers)
		{
			try
			{
				var socket = this.Socket;
				if (socket == null)
					return false;

				SocketError errorCode;
				socket.Send(buffers, SocketFlags.None, out errorCode);
				if (errorCode != SocketError.Success)
					return false;

				return true;
			}
			catch (SocketException)
			{
				Close();
			}
			catch (ObjectDisposedException)
			{
				Close();
			}
			return false;
		}

		/// <summary>
		/// Close this instance.
		/// </summary>
		public void Close()
		{
			Socket socket = null;
			lock (this.socketLock)
			{
				socket = this.Socket;
				this.Socket = null;
			}
			if (socket == null)
				return;
			
			try
			{
				socket.Close();
			}
			catch (SocketException)
			{
			}
			catch (ObjectDisposedException)
			{
			}
			
			OnConnectionClosed();
		}
		
		/// <summary>
		/// Callback method called when a connection is made.
		/// </summary>
		protected virtual void OnConnected()
		{
		}
		
		/// <summary>
		/// Callback method called when the socket has closed.
		/// </summary>
		/// <remarks>The socket has been cleared when this method is called.</remarks>
		protected virtual void OnConnectionClosed()
		{
		}
	}
}
