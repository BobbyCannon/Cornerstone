#region References

using System;
using System.Threading;
using Cornerstone.Data;

#endregion

namespace Cornerstone.Threading;

public class ReaderWriterLockTiny : IReaderWriterLock
{
	#region Constants

	public const int UpgradeableLockFlag = 0x0200_0000;

	/// <summary>
	/// Represents an upgraded read lock without a write lock
	/// </summary>
	private const int UpgradeableReadLockFlag = 0x0200_0001;

	/// <summary>
	/// Represents an upgraded write lock.
	/// </summary>
	private const int UpgradedWriteLockFlag = 0x0600_0001;

	/// <summary>
	/// Represents a standard write lock.
	/// </summary>
	private const int WriteLockFlag = 0x0400_0000;

	#endregion

	#region Fields

	private int _awaitingWriteLock;
	private int _lock;
	private int _ownerId;

	#endregion

	#region Properties

	public bool IsAwaitingWriteLock => (_awaitingWriteLock > 0) && (_lock < WriteLockFlag);

	public bool IsReadLockHeld => _lock is > 0 and < WriteLockFlag;

	public bool IsWriteLockHeld => _lock >= WriteLockFlag;

	#endregion

	#region Methods

	public void EnterReadLock()
	{
		SpinUntil(() =>
		{
			var tempLock = _lock;
			return (tempLock < WriteLockFlag)
				&& (_awaitingWriteLock == 0)
				&& (tempLock == Interlocked.CompareExchange(ref _lock, tempLock + 1, tempLock));
		});
	}

	public void EnterUpgradeableReadLock()
	{
		// Get ownership of the lock
		SpinUntil(() => 0 == Interlocked.CompareExchange(ref _ownerId, Environment.CurrentManagedThreadId, 0));

		SpinUntil(() =>
		{
			var tempLock = _lock;
			return (tempLock < WriteLockFlag)
				&& (_awaitingWriteLock == 0)
				&& (tempLock == Interlocked.CompareExchange(ref _lock, (tempLock + 1) | UpgradeableLockFlag, tempLock));
		});
	}

	public void EnterWriteLock()
	{
		// Are we in an upgradeable read lock?
		if (_ownerId == Environment.CurrentManagedThreadId)
		{
			try
			{
				// Try to get an awaiting write lock
				SpinUntil(() => 1 == Interlocked.CompareExchange(ref _awaitingWriteLock, 1, 0));

				// Wait for us to be the last reader before getting the writer lock
				SpinUntil(() => UpgradeableReadLockFlag == Interlocked.CompareExchange(ref _lock, UpgradedWriteLockFlag, UpgradeableReadLockFlag));
			}
			finally
			{
				Interlocked.Exchange(ref _awaitingWriteLock, 0);
			}
			return;
		}

		// Get ownership of the lock
		SpinUntil(() => 0 == Interlocked.CompareExchange(ref _ownerId, Environment.CurrentManagedThreadId, 0));

		try
		{
			// Try to get an awaiting write lock
			SpinUntil(() => 1 == Interlocked.CompareExchange(ref _awaitingWriteLock, 1, 0));

			try
			{
				// Now try and grab the lock, we have to wait for readers to complete
				SpinUntil(() => 0 == Interlocked.CompareExchange(ref _lock, WriteLockFlag, 0));
			}
			finally
			{
				Interlocked.Exchange(ref _awaitingWriteLock, 0);
			}
		}
		finally
		{
			// Release ownership only if we didn't succeed
			if ((_lock != WriteLockFlag) && (_lock != UpgradedWriteLockFlag))
			{
				Interlocked.Exchange(ref _ownerId, 0);
			}
		}
	}

	public void ExitReadLock()
	{
		SpinUntil(() =>
		{
			var tempLock = _lock;

			if (tempLock >= WriteLockFlag)
			{
				throw new InvalidOperationException("Incorrect read lock exit while in a write lock.");
			}

			if (GetReaderLockCount(tempLock) <= GetMinimumNumberOfReaders(tempLock))
			{
				throw new InvalidOperationException("Incorrect read lock exit...");
			}

			return tempLock == Interlocked.CompareExchange(ref _lock, tempLock - 1, tempLock);
		});
	}

	public void ExitUpgradeableReadLock()
	{
		if (_ownerId != Environment.CurrentManagedThreadId)
		{
			throw new InvalidOperationException("Incorrect thread trying to downgrade.");
		}

		SpinUntil(() =>
		{
			var tempLock = _lock;

			if (GetReaderLockCount(tempLock) < 1)
			{
				return false;
			}

			var newValue = (tempLock - 1) & ~UpgradeableLockFlag;

			if (tempLock == Interlocked.CompareExchange(ref _lock, newValue, tempLock))
			{
				Interlocked.Exchange(ref _ownerId, 0);
				return true;
			}

			return false;
		});
	}

	public void ExitWriteLock()
	{
		if (_ownerId != Environment.CurrentManagedThreadId)
		{
			throw new InvalidOperationException("Incorrect thread trying to release lock.");
		}

		// See if the lock is an upgrade one
		if (_lock == UpgradedWriteLockFlag)
		{
			// if so just downgrade the lock back to a single reader and keep ownership
			Interlocked.Exchange(ref _lock, UpgradeableReadLockFlag);
		}
		else if (_lock == WriteLockFlag)
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

	public IDisposable ReadLock()
	{
		EnterReadLock();
		return Disposable.Create(ExitReadLock);
	}

	public override string ToString()
	{
		return $"lock: {_lock}, owner: {_ownerId}";
	}

	public bool TryEnterWriteLock()
	{
		// Fast path: check if we are already the owner (e.g., upgrading from upgradeable read)
		if (_ownerId == Environment.CurrentManagedThreadId)
		{
			// We own the lock, so we can try to upgrade directly.
			// First, ensure no other thread is awaiting a write lock
			if (Interlocked.CompareExchange(ref _awaitingWriteLock, 1, 0) != 0)
			{
				return false; // someone else is already awaiting
			}

			try
			{
				// Current state must be exactly one upgradeable reader (no other readers)
				if (Interlocked.CompareExchange(ref _lock, UpgradedWriteLockFlag, UpgradeableReadLockFlag) == UpgradeableReadLockFlag)
				{
					return true;
				}
			}
			finally
			{
				// Always clear the awaiting flag if we set it
				Interlocked.Exchange(ref _awaitingWriteLock, 0);
			}

			return false;
		}

		// Normal path: try to become the owner first
		if (Interlocked.CompareExchange(ref _ownerId, Environment.CurrentManagedThreadId, 0) != 0)
		{
			return false; // someone else already owns the lock structure
		}

		try
		{
			// Try to mark ourselves as awaiting a write lock
			if (Interlocked.CompareExchange(ref _awaitingWriteLock, 1, 0) != 0)
			{
				return false; // another writer is already awaiting
			}

			try
			{
				// Now try to grab the full write lock if there are no readers
				if (Interlocked.CompareExchange(ref _lock, WriteLockFlag, 0) == 0)
				{
					return true;
				}
			}
			finally
			{
				Interlocked.Exchange(ref _awaitingWriteLock, 0);
			}
		}
		finally
		{
			// If we failed to get the write lock, release ownership
			// (we succeeded in getting ownership earlier, but didn't get the write lock)
			if ((_lock != WriteLockFlag) && (_lock != UpgradedWriteLockFlag))
			{
				Interlocked.Exchange(ref _ownerId, 0);
			}

			// Note: if we did succeed, ownership remains held — correct behavior
		}

		return false;
	}

	public IDisposable WriteLock()
	{
		EnterWriteLock();
		return Disposable.Create(ExitWriteLock);
	}

	private static int GetMinimumNumberOfReaders(int tempLock)
	{
		return (tempLock & UpgradeableLockFlag) == UpgradeableLockFlag ? 1 : 0;
	}

	private static int GetReaderLockCount(int value)
	{
		return value & ~UpgradeableLockFlag;
	}

	private static void SpinUntil(Func<bool> condition)
	{
		const int MaxSpinAttempts = 40;

		var w = new SpinWait();
		var spinCount = 0;

		while (!condition())
		{
			if (spinCount < MaxSpinAttempts)
			{
				w.SpinOnce();
				spinCount++;
			}
			else
			{
				Thread.Yield();

				// Optional: spinCount = 0;  // to give another burst after yield
				// Currently we keep yielding until condition is met (safer default)
			}
		}
	}

	#endregion
}