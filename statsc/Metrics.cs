//
// Author(s):
//     Pavlos Touboulidis <pav@pav.gr>
//
// Created on 2013-9-10
//
using System;
using System.Globalization;

namespace statsc
{
	internal static class Metrics
	{
		public static string Format(string metricName, string value, string type, string sampleRate)
		{
			return string.Concat(metricName, ":", value, "|", type, "@", sampleRate);
		}
		public static string Format(string metricName, string value, string type)
		{
			return string.Concat(metricName, ":", value, "|", type);
		}

		// [c] Counter
		public static string FormatCounter(string name, long value, double sampleRate = 1.0f)
		{
			return Format(name, value.ToString(), "c", sampleRate.ToString(CultureInfo.InvariantCulture));
		}

		// [g] Gauge
		public static string FormatGauge(string name, ulong value)
		{
			return Format(name, value.ToString(), "g");
		}
		public static string FormatGaugeDelta(string name, string sign, ulong value)
		{
			return Format(name, string.Concat(sign, value.ToString()), "g");
		}

		// [ms] Timer
		public static string FormatTimer(string name, ulong value)
		{
			return Format(name, value.ToString(), "ms");
		}
		public static string FormatTimer(string name, TimeSpan value)
		{
			return Format(name, ((int)Math.Round(value.TotalMilliseconds)).ToString(), "ms");
		}

		// [m] Meter
		public static string FormatMeter(string name, ulong value)
		{
			return Format(name, value.ToString(), "m");
		}
		public static string FormatMeter(string name)
		{
			return Format(name, "1", "m");
		}

		// [s] Set
		public static string FormatSet(string name, string value)
		{
			return Format(name, value, "s");
		}
	}
}

