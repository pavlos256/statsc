//
// Author(s):
//     Pavlos Touboulidis <pav@pav.gr>
//
// Created on 2013-9-11
//
using System;

namespace statsc
{
	/// <summary>
	/// A placeholder StatsD client class that does nothing.
	/// </summary>
	public class NullClient : IBatchingClient
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="statsc.NullClient"/> class.
		/// </summary>
		/// <param name="metricsNamespace">The namespace (prefix) to use for all metrics sent by this instance.</param>
		public NullClient(string metricsNamespace)
		{
			if (metricsNamespace == null)
				throw new ArgumentNullException("metricsNamespace");

			this.MetricsNamespace = metricsNamespace;
		}

		#region IBatchingClient implementation
		/// <summary>
		/// Turns batching mode on or off.
		/// </summary>
		/// <param name="maxBatchingDuration">Max batching duration.</param>
		public void SetBatching(TimeSpan maxBatchingDuration)
		{
		}
		#endregion

		#region IClient implementation
		/// <summary>
		/// Increments or decrements a value on the server. At each flush the current count is sent and reset to 0.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		/// <param name="sampleRate">Sample rate, used if not all events are sent, for example 1 out of 10 = 0.1.</param>
		public void Counter(string name, long value, double sampleRate)
		{
		}
		/// <summary>
		/// Increments or decrements a value on the server. At each flush the current count is sent and reset to 0.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		public void Counter(string name, long value)
		{
		}
		/// <summary>
		/// Arbitrary values, an instantaneous measurement of a value.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		public void Gauge(string name, ulong value)
		{
		}
		/// <summary>
		/// Modify a Gauge's value (non-standard)
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="sign">Sign ("+" or "-").</param>
		/// <param name="value">Value.</param>
		/// <remarks>As of 2013-09-10, this is an extension to the spec.
		/// It is supported by etsy/statsd.</remarks>
		public void GaugeDelta(string name, string sign, ulong value)
		{
		}
		/// <summary>
		/// The amount of time something took.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		public void Timer(string name, ulong value)
		{
		}
		/// <summary>
		/// The amount of time something took.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		public void Timer(string name, TimeSpan value)
		{
		}
		/// <summary>
		/// A meter measures the rate of events over time, calculated at the server. They may also be thought of as
		/// increment-only counters.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		public void Meter(string name, ulong value)
		{
		}
		/// <summary>
		/// A meter measures the rate of events over time, calculated at the server. They may also be thought of as
		/// increment-only counters.
		/// </summary>
		/// <param name="name">Name.</param>
		public void Meter(string name)
		{
		}
		/// <summary>
		/// Set the specified name and value.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		public void Set(string name, string value)
		{
		}
		/// <summary>
		/// Gets the metrics namespace (prefix) used in this instance.
		/// </summary>
		/// <value>The metrics namespace.</value>
		public string MetricsNamespace { get; private set; }
		#endregion

		#region IDisposable implementation
		/// <summary>
		/// Releases all resource used by the <see cref="statsc.NullClient"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="statsc.NullClient"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="statsc.NullClient"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the <see cref="statsc.NullClient"/> so the garbage
		/// collector can reclaim the memory that the <see cref="statsc.NullClient"/> was occupying.</remarks>
		public void Dispose()
		{
		}
		#endregion
	}
}

