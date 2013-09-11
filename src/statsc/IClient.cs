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
	/// Interface for a StatsD client.
	/// See the specification at https://github.com/b/statsd_spec
	/// and https://github.com/etsy/statsd/blob/master/docs/metric_types.md
	/// </summary>
	public interface IClient : IDisposable
	{
		/// <summary>
		/// Gets the metrics namespace (prefix) used in this instance.
		/// </summary>
		/// <value>The metrics namespace.</value>
		string MetricsNamespace { get; }

		/// <summary>
		/// Increments or decrements a value on the server. At each flush the current count is sent and reset to 0.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		/// <param name="sampleRate">Sample rate, used if not all events are sent, for example 1 out of 10 = 0.1.</param>
		void Counter(string name, long value, double sampleRate);

		/// <summary>
		/// Increments or decrements a value on the server. At each flush the current count is sent and reset to 0.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		void Counter(string name, long value);

		/// <summary>
		/// Arbitrary values, an instantaneous measurement of a value.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		void Gauge(string name, ulong value);

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
		void GaugeDelta(string name, string sign, ulong value);

		/// <summary>
		/// The amount of time something took.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		void Timer(string name, ulong value);

		/// <summary>
		/// The amount of time something took.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		void Timer(string name, TimeSpan value);

		/// <summary>
		/// A meter measures the rate of events over time, calculated at the server. They may also be thought of as increment-only counters.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		void Meter(string name, ulong value);

		/// <summary>
		/// A meter measures the rate of events over time, calculated at the server. They may also be thought of as increment-only counters.
		/// </summary>
		/// <param name="name">Name.</param>
		void Meter(string name);

		/// <summary>
		/// A "set" collects unique values, ignoring duplicates, and flushes the count of those unique values.
		/// It's similar to a <see cref="System.Collections.Generic.HashSet{T}.Count"/>.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="value">Value.</param>
		void Set(string name, string value);
	}
}

