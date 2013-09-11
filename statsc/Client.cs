//
// Author(s):
//     Pavlos Touboulidis <pav@pav.gr>
//
// Created on 2013-9-10
//
using System;
using System.Net;
using System.Text;

namespace statsc
{
	/// <summary>
	/// StatsD client.
	/// See also https://github.com/etsy/statsd/blob/master/docs/metric_types.md
	/// and https://github.com/b/statsd_spec.
	/// </summary>
	/// <remarks>
	/// Members of this class are thread safe.
	/// </remarks>
	public class Client
	{
		/// <summary>
		/// Ethernet connections (like Intranets) may use higher MTU:
		///  * Fast ethernet: 1432
		///  * Gigabit ethernet: 8932 (Jumbo frames)
		///  * Internet: 512
		/// </summary>
		public const int DefaultMaxPayloadLength = 512;

		private UdpClient udp;

		private string publicMetricsNamespace, internalMetricsNamespace;
		/// <summary>
		/// Gets the metrics namespace (prefix) used in this instance.
		/// </summary>
		/// <value>The metrics namespace.</value>
		public string MetricsNamespace
		{
			get { return this.publicMetricsNamespace; }
			private set
			{
				this.publicMetricsNamespace = value;
				this.internalMetricsNamespace = value;
				if ((this.internalMetricsNamespace.Length > 0) && (this.internalMetricsNamespace[this.internalMetricsNamespace.Length - 1] != '.'))
					this.internalMetricsNamespace = this.internalMetricsNamespace + ".";
			}
		}
		private BufferPool pool;

		private Batch batch;
		private object batchLock = new object();

		/// <summary>
		/// Initializes a new instance of the <see cref="statsc.Client"/> class.
		/// </summary>
		/// <param name="serverEndPoint">Server end point.</param>
		/// <param name="metricsNamespace">The namespace (prefix) to use for all metrics sent by this instance.</param>
		/// <param name="maxPayloadLength">The maximum length of a UDP packet.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="serverEndPoint"/> is <c>null</c>.</exception>
		/// <exception cref="System.Net.Sockets.SocketException">Thrown when the <paramref name="hostNameOrAddress"/> value cannot be resolved.</exception>
		public Client(IPEndPoint serverEndPoint, string metricsNamespace, int maxPayloadLength = DefaultMaxPayloadLength)
		{
			if (serverEndPoint == null)
				throw new ArgumentNullException("serverEndPoint");
			if (metricsNamespace == null)
				throw new ArgumentNullException("metricsNamespace");

			this.MetricsNamespace = metricsNamespace;
			this.pool = new BufferPool(maxPayloadLength, 10);

			this.udp = new UdpClient(maxPayloadLength, this.pool);
			this.udp.Connect(serverEndPoint);
		}

		/// <summary>
		/// Close this instance and the Udp socket used internally.
		/// </summary>
		public void Close()
		{
			this.udp.Close();
		}

		/// <summary>
		/// Increments or decrements a value on the server. At each flush the current count is sent and reset to 0.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		/// <param name="sampleRate">Sample rate, used if not all events are sent, for example 1 out of 10 = 0.1.</param>
		public void Counter(string name, long value, double sampleRate)
		{
			string s = Metrics.FormatCounter(string.Concat(this.internalMetricsNamespace, name), value, sampleRate);
			SendMetric(s);
		}
		/// <summary>
		/// Increments or decrements a value on the server. At each flush the current count is sent and reset to 0.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		public void Counter(string name, long value)
		{
			string s = Metrics.FormatCounter(string.Concat(this.internalMetricsNamespace, name), value);
			SendMetric(s);
		}

		/// <summary>
		/// Arbitrary values, an instantaneous measurement of a value.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		public void Gauge(string name, ulong value)
		{
			string s = Metrics.FormatGauge(string.Concat(this.internalMetricsNamespace, name), value);
			SendMetric(s);
		}
		/// <summary>
		/// Modify a Gauge's value (non-standard)
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="sign">Sign ("+" or "-").</param>
		/// <param name="value">Value.</param>
		/// <remarks>
		/// As of 2013-09-10, this is an extension to the spec.
		/// It is supported by etsy/statsd.
		/// </remarks>
		public void GaugeDelta(string name, string sign, ulong value)
		{
			string s = Metrics.FormatGaugeDelta(string.Concat(this.internalMetricsNamespace, name), sign, value);
			SendMetric(s);
		}

		/// <summary>
		/// The amount of time something took.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		public void Timer(string name, ulong value)
		{
			string s = Metrics.FormatTimer(string.Concat(this.internalMetricsNamespace, name), value);
			SendMetric(s);
		}
		/// <summary>
		/// The amount of time something took.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		public void Timer(string name, TimeSpan value)
		{
			string s = Metrics.FormatTimer(string.Concat(this.internalMetricsNamespace, name), value);
			SendMetric(s);
		}

		/// <summary>
		/// A meter measures the rate of events over time, calculated at the server. They may also be thought of as increment-only counters.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		public void Meter(string name, ulong value)
		{
			string s = Metrics.FormatMeter(string.Concat(this.internalMetricsNamespace, name), value);
			SendMetric(s);
		}
		/// <summary>
		/// A meter measures the rate of events over time, calculated at the server. They may also be thought of as increment-only counters.
		/// </summary>
		/// <param name="name">Name.</param>
		public void Meter(string name)
		{
			string s = Metrics.FormatMeter(string.Concat(this.internalMetricsNamespace, name));
			SendMetric(s);
		}

		/// <summary>
		/// A "set" collects unique values, ignoring duplicates, and flushes the count of those unique values.
		/// It's similar to a <see cref="System.Collections.Generic.HashSet{T}.Count"/>.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		public void Set(string name, string value)
		{
			string s = Metrics.FormatSet(string.Concat(this.internalMetricsNamespace, name), value);
			SendMetric(s);
		}

		private void SendMetric(string text)
		{
			if (this.batch == null)
			{
				var buffer = this.pool.CheckOut();
				try
				{
					int bytesWritten = Encoding.UTF8.GetBytes(text, 0, text.Length, buffer.Array, buffer.Offset);
					this.udp.Send(new ArraySegment<byte>(buffer.Array, buffer.Offset, bytesWritten), buffer);
				}
				catch (ArgumentException)
				{
					// text is too long according to the configured maximum payload
				}
				catch
				{
				}
			}
			else
			{
				ArraySegment<byte> bufferToSend = new ArraySegment<byte>(), bufferToCheckIn = new ArraySegment<byte>();
				bool batchReady;
				lock (this.batchLock)
				{
					batchReady = this.batch.Add(text, ref bufferToSend, ref bufferToCheckIn);
				}
				if (batchReady)
				{
					this.udp.Send(bufferToSend, bufferToCheckIn);
				}
			}
		}

		/// <summary>
		/// Turns batching mode on or off.
		/// </summary>
		/// <param name="maxBatchingDuration">Max batching duration, use <see cref="TimeSpan.Zero"/> to turn batching off.</param>
		/// <remarks>
		/// Batching will try to fit as many metric messages in one buffer as possible, to avoid many small sends.
		/// When in batching mode, it will keep appending messages to the internal buffer until the buffer is full
		/// (<see cref="Client.DefaultMaxPayloadLength"/>), or more than <paramref name="maxBatchingDuration"/> time
		/// has elapsed since the beginning of the current batch.
		/// Note that there is no background thread or timer checking if the time has elapsed; the check is performed
		/// whenever a new metric message is queued to be sent.
		/// </remarks>
		public void SetBatching(TimeSpan maxBatchingDuration)
		{
			bool turnOn = maxBatchingDuration > TimeSpan.Zero;

			lock (this.batchLock)
			{
				if (this.batch == null)
				{
					if (turnOn)
					{
						// Turn batching on
						this.batch = new Batch(maxBatchingDuration, this.pool);
					}
				}
				else
				{
					if (!turnOn)
					{
						// Turn batching off
						ArraySegment<byte> bufferToSend = new ArraySegment<byte>(), bufferToCheckIn = new ArraySegment<byte>();
						if (this.batch.Add(null, ref bufferToSend, ref bufferToCheckIn))
						{
							this.udp.Send(bufferToSend, bufferToCheckIn);
						}
						this.batch.Dispose();
						this.batch = null;
					}
				}
			}
		}

		#region Batch
		class Batch : IDisposable
		{
			private DateTime batchStartUtc;
			private TimeSpan maxBatchingDuration;
			private BufferPool pool;
			private ArraySegment<byte> buffer;
			private int usedInBuffer;

			public Batch(TimeSpan maxBatchingDuration, BufferPool pool)
			{
				this.maxBatchingDuration = maxBatchingDuration;
				this.batchStartUtc = DateTime.UtcNow;
				this.pool = pool;
				this.buffer = this.pool.CheckOut();
			}

			#region IDisposable Members
			/// <summary>
			/// Releases unmanaged resources and performs other cleanup operations before the
			/// <see cref="Batch"/> is reclaimed by garbage collection.
			/// </summary>
			~Batch()
			{
				Dispose(false);
			}
			/// <summary>
			/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
			/// If the thread is started, it tries to stop it and blocks until it stops.
			/// </summary>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}
			/// <summary>
			/// Releases unmanaged and - optionally - managed resources
			/// </summary>
			/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
			protected virtual void Dispose(bool disposing)
			{
				if (disposing)	// free managed resources
				{
					this.pool.CheckIn(this.buffer);
				}
				// free native resources
			}
			#endregion

			private bool Flush(DateTime utcNow, ref ArraySegment<byte> bufferToSend, ref ArraySegment<byte> bufferToCheckIn)
			{
				if (this.usedInBuffer > 0)
				{
					bufferToCheckIn = this.buffer;
					bufferToSend = new ArraySegment<byte>(this.buffer.Array, this.buffer.Offset, this.usedInBuffer);
					this.usedInBuffer = 0;
					this.batchStartUtc = utcNow;
					this.buffer = this.pool.CheckOut();

					return true;
				}

				return false;
			}

			public bool Add(string text, ref ArraySegment<byte> bufferToSend, ref ArraySegment<byte> bufferToCheckIn)
			{
				// If text is null or empty, just flush, otherwise add it
				if (!string.IsNullOrEmpty(text))
				{
					// Get length of text in bytes
					int dataSize = Encoding.UTF8.GetByteCount(text);

					bool needSeparator = this.usedInBuffer > 0;

					// If the data fits in what's left of the buffer (counting the separator)
					if (usedInBuffer + dataSize + (needSeparator ? 1 : 0) <= this.buffer.Count)
					{
						if (needSeparator)
						{
							// Put the separator in the buffer
							this.buffer.Array[this.buffer.Offset + this.usedInBuffer++] = (byte)'\n';
						}

						// Put the bytes in the buffer
						this.usedInBuffer += Encoding.UTF8.GetBytes(text, 0, text.Length, this.buffer.Array, this.buffer.Offset + this.usedInBuffer);

						// Check if the time has come to flush the buffer
						DateTime utcNow = DateTime.UtcNow;
						if (utcNow.Subtract(this.batchStartUtc) >= this.maxBatchingDuration)
						{
							this.Flush(utcNow, ref bufferToSend, ref bufferToCheckIn);

							// Buffer's ready
							return true;
						}
						else
						{
							// It's not time to send, we'll gather more data.
							return false;
						}
					}
					else
					{
						// The data does not fit in the current buffer.

						DateTime utcNow = DateTime.UtcNow;

						// If it's because we aleady have data in there
						if (this.Flush(utcNow, ref bufferToSend, ref bufferToCheckIn))
						{
							// Put the bytes in the new buffer
							this.usedInBuffer += Encoding.UTF8.GetBytes(text, 0, text.Length, this.buffer.Array, this.buffer.Offset + this.usedInBuffer);

							return true;
						}
						else
						{
							// It doesn't fit because it's too long. Ignore it silently.
							return false;
						}
					}
				}
				else
				{
					// "text" is empty, just flush the current buffer.
					DateTime utcNow = DateTime.UtcNow;
					return this.Flush(utcNow, ref bufferToSend, ref bufferToCheckIn);
				}
			}
		}
		#endregion
	}
}
