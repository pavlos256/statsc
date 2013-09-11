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
	/// Interface for a <see cref="IClient"/> that supports batching mode.
	/// </summary>
	public interface IBatchingClient : IClient
	{
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
		void SetBatching(TimeSpan maxBatchingDuration);
	}
}

