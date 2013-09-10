// 
// Author(s):
//     Pavlos Touboulidis <pav@pav.gr>
// 
// Created on 2010-07-24
// 
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace statsc
{
	/// <summary>
	/// A manager to handle a pool of buffers.
	/// </summary>
	/// <remarks>
	/// Originally based on:
	/// http://codebetter.com/blogs/gregyoung/archive/2007/06/18/async-sockets-and-buffer-management.aspx
	/// 
	/// Reworked to use ConcurrentStack, no locks.
	/// 
	/// When used in an async call a buffer is pinned. Large numbers of pinned buffers
	/// cause problem with the GC (in particular it causes heap fragmentation).
	///
	/// This class maintains a set of large segments and gives clients pieces of these
	/// segments that they can use for their buffers. The alternative to this would be to
	/// create many small arrays which it then maintained. This methodology should be slightly
	/// better than the many small array methodology because in creating only a few very
	/// large objects it will force these objects to be placed on the LOH. Since the
	/// objects are on the LOH they are at this time not subject to compacting which would
	/// require an update of all GC roots as would be the case with lots of smaller arrays
	/// that were in the normal heap.
	/// </remarks>
	internal class BufferPool
	{
		private readonly int segmentsPerBlock;
		private readonly int segmentSize;
		private readonly int blockSize;
		private int blocksCount;
		private readonly ConcurrentStack<ArraySegment<byte>> segments;
#if DEBUG
		private readonly ConcurrentDictionary<ArraySegment<byte>, bool> segmentGuard = new ConcurrentDictionary<ArraySegment<byte>, bool>();
#endif
		
		#region Constructors
		/// <summary>
		/// Initializes a new instance of the class.
		/// </summary>
		/// <param name="segmentSize">The size of a segment in bytes.</param>
		/// <param name="segmentsPerBlock">The number of segments to create per block.</param>
		public BufferPool(int segmentSize, int segmentsPerBlock)
			: this(segmentSize, segmentsPerBlock, 1)
		{
		}

		/// <summary>
		/// Initializes a new instance of the class.
		/// </summary>
		/// <param name="segmentSize">The size of a chunk in bytes.</param>
		/// <param name="segmentsPerBlock">The number of chunks to create per segment.</param>
		/// <param name="initialBlocks">The initial number of segments to create.</param>
		public BufferPool(int segmentSize, int segmentsPerBlock, int initialBlocks)
		{
			this.segmentSize = segmentSize;
			this.segmentsPerBlock = segmentsPerBlock;
			this.blockSize = this.segmentsPerBlock * this.segmentSize;
			
			this.segments = new ConcurrentStack<ArraySegment<byte>>();
			for (int i = 0; i < initialBlocks; i++)
				CreateNewBlock();
		}
		#endregion
		
		/// <summary>
		/// The current number of buffers available to be checked-out.
		/// </summary>
		public int AvailableBuffers
		{
			get { return this.segments.Count; }
		}

		/// <summary>
		/// The number of buffers allocated by this pool.
		/// </summary>
		public int TotalBuffers
		{
			get { return this.blocksCount * this.segmentsPerBlock; }
		}
		
		/// <summary>
		/// The total size of all buffers.
		/// </summary>
		public int TotalBufferSize
		{
			get { return this.blocksCount * this.blockSize; }
		}
		
		/// <summary>
		/// The size of each buffer, in bytes.
		/// </summary>
		public int BufferSize
		{
			get { return (this.segmentSize); }
		}

		/// <summary>
		/// Returns the number of segments required to fit the specified number of bytes.
		/// </summary>
		/// <param name="bytesCount">The number of bytes.</param>
		/// <param name="bytesReturned">The number of bytes occupied by the returned number
		/// of segments (can be greater than <paramref name="bytesCount"/>).</param>
		/// <returns>Returns the number of segments required.</returns>
		public int GetNumberOfSegmentsRequiredForBytes(int bytesCount, out int bytesReturned)
		{
			int segmentsCount = bytesCount / this.segmentSize;
			if (bytesCount % this.segmentSize > 0)
				segmentsCount++;
			bytesReturned = segmentsCount * this.segmentSize;
			return (segmentsCount);
		}
		
		/// <summary>
		/// Creates a new block, makes segments available
		/// </summary>
		private void CreateNewBlock()
		{
			// Allocate a block
			byte[] block = new byte[this.blockSize];
			
			Interlocked.Increment(ref this.blocksCount);
			
			// Create segments and add them to the list
			// Push() from the end to the beginning so that Pop() will get the beginning first.
			for (int i = this.segmentsPerBlock - 1; i >= 0; i--)
			{
				ArraySegment<byte> segment = new ArraySegment<byte>(block, i * this.segmentSize, this.segmentSize);
				this.segments.Push(segment);
#if DEBUG
				this.segmentGuard.TryAdd(segment, false);
#endif
			}
		}
		
		/// <summary>
		/// Checks out a buffer from the pool.
		/// </summary>
		/// <remarks>
		/// It is the client's responsibility to return the buffer to the pool by
		/// calling <see cref="CheckIn(ArraySegment{byte})"/> on the buffer
		/// </remarks>
		/// <seealso cref="CheckIn(ArraySegment{byte})"/>
		/// <returns>A <see cref="ArraySegment{T}"/> that can be used as a buffer.</returns>
		public ArraySegment<byte> CheckOut()
		{
			ArraySegment<byte> segment;
			while (!this.segments.TryPop(out segment))
			{
				CreateNewBlock();
			}
#if DEBUG
			this.segmentGuard[segment] = true;				
#endif
			return segment;
		}
		
		/// <summary>
		/// Checks out a number of buffers (segments) from the pool.
		/// </summary>
		/// <param name="segmentsCount">The count of segments to check out.</param>
		/// <remarks>
		/// It is the client's responsibility to return the buffers to the pool.
		/// </remarks>
		/// <seealso cref="CheckIn(IList{ArraySegment{byte}})"/>
		/// <returns>Returns a list of buffers.</returns>
		public List<ArraySegment<byte>> CheckOut(int segmentsCount)
		{
			List<ArraySegment<byte>> list = new List<ArraySegment<byte>>(segmentsCount);
			CheckOut(segmentsCount, list);
			return (list);
		}
		
		/// <summary>
		/// Checks out a number of buffers (segments) from the pool.
		/// </summary>
		/// <param name="segmentsCount">The count of segments to check out.</param>
		/// <param name="target">An instance of <see cref="IList{T}"/> to add the checked out segments into.</param>
		/// <remarks>
		/// It is the client's responsibility to return the buffers to the pool.
		/// </remarks>
		/// <seealso cref="CheckIn(IList{ArraySegment{byte}})"/>
		public void CheckOut(int segmentsCount, IList<ArraySegment<byte>> target)
		{
			if (target == null)
				throw (new ArgumentNullException("target"));
			
			ArraySegment<byte> segment;
			for (int i=0; i<segmentsCount; i++)
			{
				while (!this.segments.TryPop(out segment))
				{
					CreateNewBlock();
				}
				target.Add(segment);
#if DEBUG
				this.segmentGuard[segment] = true;
#endif
			}
		}
		
		/// <summary>
		/// Returns a previously checked-out buffer to the control of the pool.
		/// </summary>
		/// <remarks>
		/// The segment returned has to have been checked out by calling <see cref="CheckOut()"/>.
		/// </remarks>
		/// <param name="segment">The <see cref="ArraySegment{T}"/> to return to the cache.</param>
		/// <seealso cref="CheckOut()"/>
		public void CheckIn(ArraySegment<byte> segment)
		{
#if DEBUG
			bool checkedOut;
			if (!this.segmentGuard.TryGetValue(segment, out checkedOut))
				throw (new ArgumentException("The checked-in segment does not belong to this pool."));
			if (!checkedOut)
				throw (new ArgumentException("The checked-in segment has already been checked-in."));
			this.segmentGuard[segment] = false;
#endif
			this.segments.Push(segment);
		}
		
		/// <summary>
		/// Returns previously checked-out buffers to the control of the pool.
		/// </summary>
		/// <remarks>
		/// The segments returned have to have been checked out by calling <see cref="CheckOut(int)"/> or <see cref="CheckOut(int, IList{ArraySegment{byte}})"/>.
		/// </remarks>
		/// <param name="segments">The <see cref="ArraySegment{T}"/>s to return to the cache.</param>
		/// <seealso cref="CheckOut(int)"/>
		/// <seealso cref="CheckOut(int, IList{ArraySegment{byte}})"/>
		public void CheckIn(IList<ArraySegment<byte>> segments)
		{
			if (segments == null)
				throw (new ArgumentNullException("segments"));

			foreach (ArraySegment<byte> segment in segments)
			{
#if DEBUG
				bool checkedOut;
				if (!this.segmentGuard.TryGetValue(segment, out checkedOut))
					throw (new ArgumentException("The checked-in segment does not belong to this pool."));
				if (!checkedOut)
					throw (new ArgumentException("The checked-in segment has already been checked-in."));
				this.segmentGuard[segment] = false;
#endif
				this.segments.Push(segment);
			}
		}
	}
}
