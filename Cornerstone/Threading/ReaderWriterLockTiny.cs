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
		var currentThreadId = Environment.CurrentManagedThreadId;

		if (_ownerId != currentThreadId)
		{
			SpinUntil(() => 0 == Interlocked.CompareExchange(ref _ownerId, currentThreadId, 0));
		}

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
		var currentThreadId = Environment.CurrentManagedThreadId;

		if (_ownerId == currentThreadId)
		{
			try
			{
				SpinUntil(() => 1 == Interlocked.CompareExchange(ref _awaitingWriteLock, 1, 0));

				SpinUntil(() => UpgradeableReadLockFlag == Interlocked.CompareExchange(ref _lock, UpgradedWriteLockFlag, UpgradeableReadLockFlag));
			}
			finally
			{
				Interlocked.Exchange(ref _awaitingWriteLock, 0);
			}

			return;
		}

		if (_ownerId != currentThreadId)
		{
			SpinUntil(() => 0 == Interlocked.CompareExchange(ref _ownerId, currentThreadId, 0));
		}

		try
		{
			SpinUntil(() => 1 == Interlocked.CompareExchange(ref _awaitingWriteLock, 1, 0));

			try
			{
				SpinUntil(() => 0 == Interlocked.CompareExchange(ref _lock, WriteLockFlag, 0));
			}
			finally
			{
				Interlocked.Exchange(ref _awaitingWriteLock, 0);
			}
		}
		finally
		{
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
			var currentLock = _lock;

			if (currentLock >= WriteLockFlag)
			{
				throw new InvalidOperationException("Incorrect read lock exit while in a write lock.");
			}

			if (GetReaderLockCount(currentLock) <= GetMinimumNumberOfReaders(currentLock))
			{
				throw new InvalidOperationException("Incorrect read lock exit...");
			}

			return currentLock == Interlocked.CompareExchange(ref _lock, currentLock - 1, currentLock);
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

		if (_lock == UpgradedWriteLockFlag)
		{
			Interlocked.Exchange(ref _lock, UpgradeableReadLockFlag);
		}
		else if (_lock == WriteLockFlag)
		{
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
		var currentThreadId = Environment.CurrentManagedThreadId;

		if (_ownerId == currentThreadId)
		{
			if (Interlocked.CompareExchange(ref _awaitingWriteLock, 1, 0) != 0)
			{
				return false;
			}

			try
			{
				if (Interlocked.CompareExchange(ref _lock, UpgradedWriteLockFlag, UpgradeableReadLockFlag) == UpgradeableReadLockFlag)
				{
					return true;
				}
			}
			finally
			{
				Interlocked.Exchange(ref _awaitingWriteLock, 0);
			}

			return false;
		}

		if (_ownerId != currentThreadId)
		{
			if (Interlocked.CompareExchange(ref _ownerId, currentThreadId, 0) != 0)
			{
				return false;
			}
		}

		try
		{
			if (Interlocked.CompareExchange(ref _awaitingWriteLock, 1, 0) != 0)
			{
				return false;
			}

			try
			{
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
			if ((_lock != WriteLockFlag) && (_lock != UpgradedWriteLockFlag))
			{
				Interlocked.Exchange(ref _ownerId, 0);
			}
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
		const int maxSpinAttempts = 40;

		var w = new SpinWait();
		var spinCount = 0;

		while (!condition())
		{
			if (spinCount < maxSpinAttempts)
			{
				w.SpinOnce();
				spinCount++;
			}
			else
			{
				Thread.Yield();
			}
		}
	}

	#endregion
}