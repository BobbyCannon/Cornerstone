#region References

using System;
using System.Diagnostics;
using System.Threading;

#endregion

namespace Cornerstone.Threading;

/// <inheritdoc />
public class ReaderWriterLockTiny : IReaderWriterLock
{
	#region Constants

	/// <summary>
	/// Represents an upgraded write lock.
	/// </summary>
	private const int UpgradedWriteLock = 1000001;

	/// <summary>
	/// Represents a standard write lock.
	/// </summary>
	private const int WriteLock = 1000000;

	#endregion

	#region Fields

	private bool _awaitingWriteLock;
	private int _lock;
	private int _ownerId;

	#endregion

	#region Properties

	/// <inheritdoc />
	public bool IsAwaitingWriteLock => _awaitingWriteLock && (_lock < WriteLock);

	/// <inheritdoc />
	public bool IsReadLockHeld => _lock is > 0 and < WriteLock;

	/// <inheritdoc />
	public bool IsWriteLockHeld => _lock >= WriteLock;

	#endregion

	#region Methods

	/// <inheritdoc />
	public void EnterReadLock()
	{
		var w = new SpinWait();
		var tmpLock = _lock;

		while ((tmpLock >= WriteLock)
				|| _awaitingWriteLock
				|| (tmpLock != Interlocked.CompareExchange(ref _lock, tmpLock + 1, tmpLock)))
		{
			w.SpinOnce();
			tmpLock = _lock;
		}
	}

	/// <inheritdoc />
	public void EnterUpgradeableReadLock()
	{
		var w = new SpinWait();

		// Go ahead and take up the owner slot
		while (0 != Interlocked.CompareExchange(ref _ownerId, Environment.CurrentManagedThreadId, 0))
		{
			w.SpinOnce();
		}

		var tmpLock = _lock;

		while ((tmpLock >= WriteLock)
				|| _awaitingWriteLock
				|| (tmpLock != Interlocked.CompareExchange(ref _lock, tmpLock + 1, tmpLock)))
		{
			w.SpinOnce();
			tmpLock = _lock;
		}
	}

	/// <inheritdoc />
	public void EnterWriteLock()
	{
		var w = new SpinWait();

		if (_ownerId == Environment.CurrentManagedThreadId)
		{
			try
			{
				_awaitingWriteLock = true;

				// Wait for us to be the last reader before getting the writer lock
				while (1 != Interlocked.CompareExchange(ref _lock, UpgradedWriteLock, 1))
				{
					w.SpinOnce();
				}
			}
			finally
			{
				_awaitingWriteLock = false;
			}
			return;
		}

		// Wait to get the writer slot
		while (0 != Interlocked.CompareExchange(ref _ownerId, Environment.CurrentManagedThreadId, 0))
		{
			w.SpinOnce();
		}

		try
		{
			_awaitingWriteLock = true;

			// Now try and grab the lock, we have to wait for readers to complete
			while (0 != Interlocked.CompareExchange(ref _lock, WriteLock, 0))
			{
				w.SpinOnce();
			}
		}
		finally
		{
			_awaitingWriteLock = false;
		}
	}

	/// <inheritdoc />
	public void ExitReadLock()
	{
		Interlocked.Decrement(ref _lock);
	}

	/// <inheritdoc />
	public void ExitUpgradeableReadLock()
	{
		if (_ownerId != Environment.CurrentManagedThreadId)
		{
			Debug.Assert(true, "Incorrect thread trying to downgrade.");
			return;
		}

		Interlocked.Decrement(ref _lock);

		_ownerId = 0;
	}

	/// <inheritdoc />
	public void ExitWriteLock()
	{
		// See if the lock is an upgrade one
		if (_lock == UpgradedWriteLock)
		{
			// if so just downgrade the lock back to a single reader and keep ownership
			_lock = 1;
		}
		else
		{
			// release the lock and the ownership
			_lock = 0;
			_ownerId = 0;
		}
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return $"lock: {_lock}, owner: {_ownerId}";
	}

	#endregion
}