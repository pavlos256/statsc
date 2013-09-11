# StatsC

An asynchronous StatsD client with built-in support for batching.

## Spec
StatsC follows the [StatsD spec](https://github.com/b/statsd_spec) and implements the Gauge Delta & the Set extensions from [etsy/statsd](https://github.com/etsy/statsd/blob/master/docs/metric_types.md).

## Requirements
StatsC requires .NET 4 or Mono.
NUnit is required for the unit tests.

## Examples

### Immediate mode
	:::csharp
	var sc = new statsc.Client(new DnsEndPoint("hostname", 4567), "app-namespace", 512);
	sc.Counter("counter1", 5);
	sc.Counter("counter2", 1, 0.5d);

	sc.Gauge("gauge1", ulong.MaxValue);
	sc.GaugeDelta("gauge1", "-", 10);

	sc.Meter("meter1");
	sc.Meter("meter1");

	sc.Timer("timer1", TimeSpan.FromMilliseconds(10));
	sc.Timer("timer1", 20);

	sc.Set("set1", "something");
	sc.Set("set1", "else");

	client.Dispose();

	// Does nothing
	client.Counter("counter1", 5);

### Batching mode
	:::csharp
	// Keep messages in internal buffer for up to 1 second
	// and send them with one packet, if possible.
	sc.SetBatching(TimeSpan.FromSeconds(1d));

	sc.Counter("counter1", 5);
	sc.Counter("counter2", 1, 0.5d);
	Thread.Sleep(1000);

	// The following metric will cause the batch to be sent
	// because the max batch duration has been exceeded.
	// If the following metric can fit in one packet, it will
	// be included as well.
	sc.Gauge("gauge1", ulong.MaxValue);

	// This will start a new batch and will be sent later.
	sc.GaugeDelta("gauge1", "-", 10);

	sc.Meter("meter1");
	sc.Meter("meter1");

	sc.Timer("timer1", TimeSpan.FromMilliseconds(10));
	sc.Timer("timer1", 20);

	// Turn batching mode off and send any pending data.
	sc.SetBatching(TimeSpan.Zero);

	// Each will be sent in different packets
	sc.Set("set1", "something");
	sc.Set("set1", "else");

### NullClient & IBatchingClient
A dummy client with empty implementation and a common `IClient` (or `IBatchingClient`) interface is included for convenience in cases where stats reporting should be turned off.

	:::csharp
	IBatchingClient sc;
	if (!string.IsNullOrEmpty(statsdHostName))
		sc = new statsc.Client(new DnsEndPoint(statsdHostName, 4567), "app-namespace", 512);
	else
		sc = new statsc.NullClient("app-namespace");

	// The following do nothing
	sc.Counter("counter1", 5);
	sc.Counter("counter2", 1, 0.5d);

## License

StatsC is provided under the common [MIT license](http://opensource.org/licenses/mit-license.php). In short, it is free to be used in any project, commercial or not, as long 
as the license and copyright notice is kept. See the LICENSE file for details.

Copyright © 2013, Pavlos Touboulidis.
