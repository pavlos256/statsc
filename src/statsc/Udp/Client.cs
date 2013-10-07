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

		private void AsyncCompleted(object sender, SocketAsyncEventArgs e)
		{
			switch (e.LastOperation)
			{
				case SocketAsyncOperation.Send:
					ProcessSend(e);
					break;
					
				default:
					throw (new ArgumentException("Unhandled socket operation."));
			}
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
		/// Begins sending data.
		/// </summary>
		/// <param name="buffer">A byte array that contains the data to be sent.</param>
		/// <param name="offset">The zero-based position in the buffer parameter at which to begin sending data.</param>
		/// <param name="size">The number of bytes to send.</param>
		/// <param name="userToken">A user supplied parameter that will be available when the send operation completes.</param>
		public bool Send(byte[] buffer, int offset, int size, object userToken = null)
		{
			try
			{
				var socket = this.Socket;
				if (socket == null)
					return false;

				SocketAsyncEventArgs e = new SocketAsyncEventArgs();
				e.Completed += AsyncCompleted;
				e.UserToken = userToken;
				e.SetBuffer(buffer, offset, size);
				if (!socket.SendAsync(e))
					ProcessSend(e);
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
		/// Begins sending data.
		/// </summary>
		/// <param name="buffer">An array segment that contains the data to be sent.</param>
		/// <param name="userToken">A user supplied parameter that will be available when the send operation completes.</param>
		protected bool Send(ArraySegment<byte> buffer, object userToken = null)
		{
			return Send(buffer.Array, buffer.Offset, buffer.Count, userToken);
		}
		
		/// <summary>Begins sending data.</summary>
		/// <returns>Returns <c>true</c> on success, <c>false</c> on error.</returns>
		/// <param name='buffers'>The data to sent. WARNING: Only 1 element is supported.</param>
		/// <param name="userToken">A user supplied parameter that will be available when the send operation completes.</param>
		protected bool Send(IList<ArraySegment<byte>> buffers, object userToken = null)
		{
			if (buffers.Count != 1)
				throw (new ArgumentException("The list of buffers should have exactly one element.", "buffers"));

			foreach(var buffer in buffers)
				return Send(buffer, userToken);
			
			return false;
		}

		private void ProcessSend(SocketAsyncEventArgs e)
		{
			try
			{
				OnSendCompleted(e.BytesTransferred, e.SocketError, e.UserToken);
			}
			catch (SocketException)
			{
			}
			catch (ObjectDisposedException)
			{
			}
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
		/// Callback method called when a Send() method completes and the data have been sent.
		/// </summary>
		/// <param name="bytes">The number of bytes sent.</param>
		/// <param name="result">The result of the asynchronous socket operation.</param>
		/// <param name="userToken">A user token supplied when the Send method was invoked.</param>
		protected virtual void OnSendCompleted(int bytes, SocketError result, object userToken)
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
