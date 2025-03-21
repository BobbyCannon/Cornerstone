#region References

using System;
using System.Threading;

#endregion

namespace Cornerstone.Threading;

/// <inheritdoc />
public class ReaderWriterLockTiny : IReaderWriterLock
{
	#region Constants

	public const int UpgradeableLockFlag = 0x0200_0000;

	/// <summary>
	/// Represents an upgraded read lock without a write lock
	/// </summary>
	private const int UpgradeableReadLock = 0x0200_0001;

	/// <summary>
	/// Represents an upgraded write lock.
	/// </summary>
	private const int UpgradedWriteLock = 0x0600_0001;

	/// <summary>
	/// Represents a standard write lock.
	/// </summary>
	private const int WriteLock = 0x0400_0000;

	#endregion

	#region Fields

	private int _awaitingWriteLock;
	private int _lock;
	private int _ownerId;

	#endregion

	#region Properties

	/// <inheritdoc />
	public bool IsAwaitingWriteLock => (_awaitingWriteLock > 0) && (_lock < WriteLock);

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
		var tempLock = _lock;

		while ((tempLock >= WriteLock)
				|| (_awaitingWriteLock > 0)
				|| (tempLock != Interlocked.CompareExchange(ref _lock, tempLock + 1, tempLock)))
		{
			w.SpinOnce();
			tempLock = _lock;
		}
	}

	/// <inheritdoc />
	public void EnterUpgradeableReadLock()
	{
		var w = new SpinWait();

		// Get ownership of the lock
		while (0 != Interlocked.CompareExchange(ref _ownerId, Environment.CurrentManagedThreadId, 0))
		{
			w.SpinOnce();
		}

		var tempLock = _lock;

		while ((tempLock >= WriteLock)
				|| (_awaitingWriteLock > 0)
				|| (tempLock != Interlocked.CompareExchange(ref _lock, (tempLock + 1) | UpgradeableLockFlag, tempLock)))
		{
			w.SpinOnce();
			tempLock = _lock;
		}
	}

	/// <inheritdoc />
	public void EnterWriteLock()
	{
		var w = new SpinWait();

		// Are we in an upgradeable read lock?
		if (_ownerId == Environment.CurrentManagedThreadId)
		{
			try
			{
				// Try to get an awaiting write lock
				while (1 != Interlocked.CompareExchange(ref _awaitingWriteLock, 1, 0))
				{
					w.SpinOnce();
				}

				// Wait for us to be the last reader before getting the writer lock
				while (UpgradeableReadLock != Interlocked.CompareExchange(ref _lock, UpgradedWriteLock, UpgradeableReadLock))
				{
					w.SpinOnce();
				}
			}
			finally
			{
				Interlocked.Exchange(ref _awaitingWriteLock, 0);
			}
			return;
		}

		// Get ownership of the lock
		while (0 != Interlocked.CompareExchange(ref _ownerId, Environment.CurrentManagedThreadId, 0))
		{
			w.SpinOnce();
		}

		try
		{
			// Try to get an awaiting write lock
			while (1 != Interlocked.CompareExchange(ref _awaitingWriteLock, 1, 0))
			{
				w.SpinOnce();
			}

			// Now try and grab the lock, we have to wait for readers to complete
			while (0 != Interlocked.CompareExchange(ref _lock, WriteLock, 0))
			{
				w.SpinOnce();
			}
		}
		finally
		{
			Interlocked.Exchange(ref _awaitingWriteLock, 0);
		}
	}

	/// <inheritdoc />
	public void ExitReadLock()
	{
		var w = new SpinWait();
		var tempLock = _lock;

		if (tempLock >= WriteLock)
		{
			throw new InvalidOperationException("Incorrect read lock exit while in a write lock.");
		}

		while (GetReaderLockCount(tempLock) > GetMinimumNumberOfReaders(tempLock))
		{
			if (tempLock == Interlocked.CompareExchange(ref _lock, tempLock - 1, tempLock))
			{
				return;
			}

			w.SpinOnce();
			tempLock = _lock;
		}

		throw new InvalidOperationException("Incorrect read lock exit...");
	}

	/// <inheritdoc />
	public void ExitUpgradeableReadLock()
	{
		if (_ownerId != Environment.CurrentManagedThreadId)
		{
			throw new InvalidOperationException("Incorrect thread trying to downgrade.");
		}

		var w = new SpinWait();
		var tempLock = _lock;

		while ((_ownerId > 0) && (GetReaderLockCount(tempLock) >= 1))
		{
			if (tempLock == Interlocked.CompareExchange(ref _lock, (tempLock - 1) & ~UpgradeableLockFlag, tempLock))
			{
				Interlocked.Exchange(ref _ownerId, 0);
				return;
			}

			w.SpinOnce();
			tempLock = _lock;
		}

		throw new InvalidOperationException("Incorrect exiting for upgradeable read lock.");
	}

	/// <inheritdoc />
	public void ExitWriteLock()
	{
		if (_ownerId != Environment.CurrentManagedThreadId)
		{
			throw new InvalidOperationException("Incorrect thread trying to release lock.");
		}

		// See if the lock is an upgrade one
		if (_lock == UpgradedWriteLock)
		{
			// if so just downgrade the lock back to a single reader and keep ownership
			Interlocked.Exchange(ref _lock, UpgradeableReadLock);
		}
		else if (_lock == WriteLock)
		{
			// release the lock and the ownership
			Interlocked.Exchange(ref _lock, 0);
			Interlocked.Exchange(ref _ownerId, 0);
		}
		else
		{
			throw new InvalidOperationException("Incorrect state to release lock.");
		}
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return $"lock: {_lock}, owner: {_ownerId}";
	}

	private static int GetMinimumNumberOfReaders(int tempLock)
	{
		return (tempLock & UpgradeableLockFlag) == UpgradeableLockFlag ? 1 : 0;
	}

	private static int GetReaderLockCount(int value)
	{
		return value & ~UpgradeableLockFlag;
	}

	#endregion
}