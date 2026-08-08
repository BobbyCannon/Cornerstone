#region References

using System;
using System.Threading;
using Cornerstone.Collections;
using Cornerstone.Presentation;

#endregion

namespace Cornerstone.Text;

/// <summary>
/// One-way double-buffered character ingress for high-rate producers (e.g. LLM tokens).
/// Producers append into a staging buffer; a single consumer drains once per tick into
/// a destination (typically the UI document buffer).
/// </summary>
/// <remarks>
/// Implements <see cref="IDispatchPending" /> for AppDispatcher bindings.
/// Pending is derived from staged character count. <see cref="ClearHasPending" /> does
/// not discard staged text — use <see cref="Drain" /> / <see cref="DrainTo" /> to consume,
/// or <see cref="Clear" /> to drop without applying.
/// </remarks>
public sealed class TextIngress : IDispatchPending
{
	#region Fields

	private int _pendingCount;
	private StringBuffer _read;

	private readonly object _sync;
	private StringBuffer _write;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates an ingress with default staging capacity.
	/// </summary>
	public TextIngress() : this(SpeedyList.DefaultCapacity)
	{
	}

	/// <summary>
	/// Creates an ingress with an explicit initial capacity for each buffer.
	/// </summary>
	/// <param name="initialCapacity"> Initial capacity for write and read buffers. </param>
	public TextIngress(int initialCapacity)
	{
		if (initialCapacity < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(initialCapacity));
		}

		_sync = new object();
		_write = new StringBuffer(initialCapacity);
		_read = new StringBuffer(initialCapacity);
		_pendingCount = 0;
	}

	#endregion

	#region Properties

	/// <summary>
	/// True when at least one character is waiting to be drained.
	/// Safe to read without taking the lock (volatile pending count).
	/// </summary>
	public bool HasPending => Volatile.Read(ref _pendingCount) > 0;

	/// <summary>
	/// Number of characters currently staged and not yet drained.
	/// </summary>
	public int PendingCount => Volatile.Read(ref _pendingCount);

	#endregion

	#region Methods

	/// <summary>
	/// Appends a single character (producer / any thread).
	/// </summary>
	public void Append(char value)
	{
		lock (_sync)
		{
			_write.Append(value);
			Volatile.Write(ref _pendingCount, _write.Count);
		}
	}

	/// <summary>
	/// Appends a string (producer / any thread).
	/// </summary>
	public void Append(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return;
		}

		Append(value.AsSpan());
	}

	/// <summary>
	/// Appends a span of characters (producer / any thread).
	/// </summary>
	public void Append(ReadOnlySpan<char> value)
	{
		if (value.IsEmpty)
		{
			return;
		}

		lock (_sync)
		{
			_write.Append(value);
			Volatile.Write(ref _pendingCount, _write.Count);
		}
	}

	/// <summary>
	/// Clears staged and drained buffers. Does not affect any prior drain destinations.
	/// </summary>
	public void Clear()
	{
		lock (_sync)
		{
			_write.Clear();
			_read.Clear();
			Volatile.Write(ref _pendingCount, 0);
		}
	}

	/// <summary>
	/// No-op for staged text. Pending is cleared by <see cref="Drain" /> / <see cref="DrainTo" />
	/// or by discarding via <see cref="Clear" />. Do not use this to skip a drain.
	/// </summary>
	public void ClearHasPending()
	{
		// Intentionally empty: pending is the staged character count, not a separate flag.
	}

	/// <summary>
	/// Swaps the write buffer out and invokes <paramref name="consumer" /> with the batch.
	/// Intended for the UI / dispatch apply path (once per tick).
	/// </summary>
	/// <param name="consumer"> Receives the pending characters; must not re-enter Append. </param>
	/// <returns> Number of characters drained. </returns>
	public int Drain(Action<ReadOnlySpan<char>> consumer)
	{
		if (consumer is null)
		{
			throw new ArgumentNullException(nameof(consumer));
		}

		if (!HasPending)
		{
			return 0;
		}

		int count;
		lock (_sync)
		{
			if (_write.Count == 0)
			{
				Volatile.Write(ref _pendingCount, 0);
				return 0;
			}

			// Swap the buffers
			// - write becomes the batch to consume
			// - former read (empty) becomes the current write.
			(_write, _read) = (_read, _write);
			count = _read.Count;
			Volatile.Write(ref _pendingCount, 0);
		}

		// Outside lock: consumer may touch UI / destination buffers.
		if (count > 0)
		{
			consumer(_read.AsSpan());
			_read.Clear();
		}

		return count;
	}

	/// <summary>
	/// Swaps the write buffer out and appends the batch to <paramref name="destination" />.
	/// </summary>
	/// <returns> Number of characters drained. </returns>
	public int DrainTo(IStringBuffer destination)
	{
		if (destination is null)
		{
			throw new ArgumentNullException(nameof(destination));
		}

		return Drain(destination.Append);
	}

	#endregion
}