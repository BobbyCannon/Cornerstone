#region References

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Extensions;
using Cornerstone.Testing;
using Cornerstone.Text;
using Cornerstone.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

#pragma warning disable MSTEST0039

namespace Cornerstone.UnitTests.Threading;

[TestClass]
public class LockableTests : BaseLockableTests
{
	#region Methods

	[TestMethod]
	public void BasicLocksAndUnlocks()
	{
		RunTestOnLock((_, lockable) =>
		{
			IsFalse(lockable.IsReadLockHeld);
			IsFalse(lockable.IsWriteLockHeld);
			lockable.EnterReadLock();
			IsTrue(lockable.IsReadLockHeld);
			IsFalse(lockable.IsWriteLockHeld);
			lockable.ExitReadLock();
			lockable.EnterWriteLock();
			IsFalse(lockable.IsReadLockHeld);
			IsTrue(lockable.IsWriteLockHeld);
			lockable.ExitWriteLock();
			IsFalse(lockable.IsReadLockHeld);
			IsFalse(lockable.IsWriteLockHeld);
		}, true);
	}

	[TestMethod]
	public void BasicUpgradeableLocksAndUnlocks()
	{
		RunTestOnLock((_, lockable) =>
		{
			IsFalse(lockable.IsReadLockHeld);
			IsFalse(lockable.IsWriteLockHeld);
			lockable.EnterReadLock();
			IsTrue(lockable.IsReadLockHeld);
			IsFalse(lockable.IsWriteLockHeld);
			lockable.EnterUpgradeableReadLock();
			IsTrue(lockable.IsReadLockHeld);
			IsFalse(lockable.IsWriteLockHeld);
			lockable.ExitReadLock();
			IsTrue(lockable.IsReadLockHeld);
			IsFalse(lockable.IsWriteLockHeld);
			lockable.ExitUpgradeableReadLock();
			lockable.EnterWriteLock();
			IsFalse(lockable.IsReadLockHeld);
			IsTrue(lockable.IsWriteLockHeld);
			lockable.ExitWriteLock();
			IsFalse(lockable.IsReadLockHeld);
			IsFalse(lockable.IsWriteLockHeld);
		}, false);
	}

	[TestMethod]
	public void ReadLockPreventsConcurrentWriteLock()
	{
		RunTestOnLock((_, lockable) =>
		{
			var sb = new StringBuilder();
			var writerReady = new ManualResetEventSlim(false);

			sb.Append("+R");
			lockable.EnterReadLock();

			var writeAcquired = false;
			var writeTask = Task.Run(() =>
			{
				writerReady.Set();
				sb.Append("+W?");
				lockable.EnterWriteLock();
				writeAcquired = true;

				sb.Append("+W");
				lockable.ExitWriteLock();
				sb.Append("-W");
			});

			// Wait until writer is blocked at EnterWriteLock (very fast, usually < 1 ms)
			var signaled = writerReady.Wait(TimeSpan.FromSeconds(2));
			IsTrue(signaled, () => "Writer thread didn't reach the lock call in time");

			// Now that writer is queued/blocked, release the read lock
			sb.Append("-R");
			lockable.ExitReadLock();

			writeTask.Wait(TimeSpan.FromSeconds(2));
			IsTrue(writeAcquired);

			var log = sb.ToString();
			IsTrue(log.Contains("+R+W?-R+W-W"), () => $"Unexpected sequence {log}");
		}, true);
	}

	[TestMethod]
	public void UpgradeableReadLockCanBeUpgradedToWriteLock()
	{
		RunTestOnLock((_, lockable) =>
		{
			using var rented = StringBuilderPool.Rent();
			var builder = rented.Value;

			// Test the correct upgrade path
			builder.Append("+UR");
			lockable.EnterUpgradeableReadLock();
			IsTrue(lockable.IsReadLockHeld);
			IsFalse(lockable.IsWriteLockHeld);

			builder.Append("+W");
			lockable.EnterWriteLock();
			IsTrue(lockable.IsWriteLockHeld);

			builder.Append("-W");
			lockable.ExitWriteLock();
			IsTrue(lockable.IsReadLockHeld);
			IsFalse(lockable.IsWriteLockHeld);

			builder.Append("-UR");
			lockable.ExitUpgradeableReadLock();
			IsFalse(lockable.IsReadLockHeld);
			IsFalse(lockable.IsWriteLockHeld);

			AreEqual("+UR+W-W-UR", builder.ToString());
		}, false);
	}

	[TestMethod]
	public void UpgradeableReadLockCanShareWithOtherReaders()
	{
		RunTestOnLock((_, lockable) =>
		{
			using var orderedSteps = new TestCoordinator(4);
			IsTrue(lockable is { IsReadLockHeld: false, IsWriteLockHeld: false });
			using var rented = StringBuilderPool.Rent();
			var builder = rented.Value;

			Environment.CurrentManagedThreadId.Dump("Test Thread: ");

			orderedSteps.ProcessStep(1, () =>
			{
				builder.Append("+1");
				lockable.EnterUpgradeableReadLock();
				IsTrue(lockable is { IsReadLockHeld: true, IsWriteLockHeld: false });
				builder.Append("-1");
			});

			Task.Run(() =>
			{
				Environment.CurrentManagedThreadId.Dump("Task Thread: ");

				orderedSteps.ProcessStep(2, () =>
				{
					builder.Append("+2");
					lockable.EnterReadLock();
					IsTrue(lockable is { IsReadLockHeld: true, IsWriteLockHeld: false });
					builder.Append("-2");
				});

				orderedSteps.ProcessStep(4, () =>
				{
					builder.Append("+4");
					lockable.ExitReadLock();
					IsTrue(lockable is { IsReadLockHeld: false, IsWriteLockHeld: false });
					builder.Append("-4");
				});
			});

			orderedSteps.ProcessStep(3, () =>
			{
				builder.Append("+3");
				lockable.ExitUpgradeableReadLock();
				IsTrue(lockable is { IsReadLockHeld: true, IsWriteLockHeld: false });
				builder.Append("-3");
			});

			orderedSteps.WaitForStep(4);

			AreEqual("+1-1+2-2+3-3+4-4", builder.ToString());
		}, false);
	}

	[TestMethod]
	public void UpgradeableReadLockWillWaitForCompletedReaders()
	{
		RunTestOnLock((_, lockable) =>
		{
			using var orderedSteps = new TestCoordinator(4);
			IsTrue(lockable is { IsReadLockHeld: false, IsWriteLockHeld: false });
			var builder = new StringBuilder();

			Environment.CurrentManagedThreadId.Dump("Test Thread: ");

			orderedSteps.ProcessStep(1, () =>
			{
				builder.Append("+1");
				lockable.EnterUpgradeableReadLock();
				IsTrue(lockable is { IsReadLockHeld: true, IsWriteLockHeld: false });
				builder.Append("-1");
			});

			var task = Task.Run(() =>
			{
				Environment.CurrentManagedThreadId.Dump("Task Thread: ");

				orderedSteps.ProcessStep(2, () =>
				{
					builder.Append("+2");
					lockable.EnterReadLock();
					IsTrue(lockable is { IsReadLockHeld: true, IsWriteLockHeld: false });
					builder.Append("-2");
				});

				orderedSteps.ProcessStep(3, () =>
				{
					// Wait until the main test thread is awaiting the write lock
					// once it's waiting we can release the read lock
					var result = lockable.WaitUntil(x => x.IsAwaitingWriteLock, 1000);
					IsTrue(result, () => "The thread was never awaiting lock...");
					builder.Append("+3");
					lockable.ExitReadLock();
					builder.Append("-3");
				});
			});

			orderedSteps.WaitForStep(2);

			// Should block and wait for step 3 to complete
			builder.Append("+W");
			lockable.EnterWriteLock();
			IsTrue(lockable is { IsWriteLockHeld: true });

			orderedSteps.ProcessStep(4, () =>
			{
				builder.Append("+4");
				lockable.ExitWriteLock();
				IsTrue(lockable is { IsWriteLockHeld: false });
				builder.Append("-4");
			});

			orderedSteps.WaitForStep(4);
			builder.Append("-W");

			IsFalse(task.IsFaulted, () => task.Exception?.ToDetailedString());
			AreEqual("+1-1+2-2+W+3-3+4-4-W", builder.ToString());
		}, false);
	}

	[TestMethod]
	public void VerifyAllExceptionPatterns()
	{
		var lockable = new ReaderWriterLockTiny();

		//Exiting a read lock when none is held
		var ex1 = Assert.ThrowsExactly<InvalidOperationException>(() => lockable.ExitReadLock());
		StringAssert.Contains(ex1.Message, "Incorrect read lock exit");

		//Exiting a read lock when write lock is held
		lockable.EnterWriteLock();
		var ex2 = Assert.ThrowsExactly<InvalidOperationException>(() => lockable.ExitReadLock());
		StringAssert.Contains(ex2.Message, "Incorrect read lock exit while in a write lock");
		lockable.ExitWriteLock();

		//Exiting an upgradeable read lock from a different thread
		lockable.EnterUpgradeableReadLock();
		Debug.WriteLine($"Before Task.Run : Thread {Environment.CurrentManagedThreadId}");
		Thread.Sleep(0);

		var task1 = Task.Factory.StartNew(() =>
		{
			Debug.WriteLine($"Inside Task.Run : Thread {Environment.CurrentManagedThreadId}");
			return Assert.ThrowsExactly<InvalidOperationException>(() => lockable.ExitUpgradeableReadLock()).Message;
		}, TaskCreationOptions.LongRunning);

		var result1 = task1.Result;
		StringAssert.Contains(result1, "Incorrect thread trying to downgrade");
		lockable.ExitUpgradeableReadLock();

		// Exiting an upgradeable read lock when not held
		var ex3 = Assert.ThrowsExactly<InvalidOperationException>(() => lockable.ExitUpgradeableReadLock());
		StringAssert.Contains(ex3.Message, "Incorrect thread trying to downgrade");

		// Exiting a write lock from a different thread
		lockable.EnterWriteLock();
		Debug.WriteLine($"Before Task.Run : Thread {Environment.CurrentManagedThreadId}");
		Thread.Sleep(0);

		var task2 = Task.Factory.StartNew(() =>
		{
			Debug.WriteLine($"Inside Task.Run : Thread {Environment.CurrentManagedThreadId}");
			return Assert.ThrowsExactly<InvalidOperationException>(() => lockable.ExitWriteLock()).Message;
		}, TaskCreationOptions.LongRunning);

		var result2 = task2.Result;
		StringAssert.Contains(result2, "Incorrect thread trying to release lock");
		lockable.ExitWriteLock();

		// Exiting a write lock when not held
		var ex4 = Assert.ThrowsExactly<InvalidOperationException>(() => lockable.ExitWriteLock());
		StringAssert.Contains(ex4.Message, "Incorrect thread trying to release lock");

		// Exiting more read locks than entered
		lockable.EnterReadLock();
		lockable.ExitReadLock();
		var ex5 = Assert.ThrowsExactly<InvalidOperationException>(() => lockable.ExitReadLock());
		StringAssert.Contains(ex5.Message, "Incorrect read lock exit");
		var ex6 = Assert.ThrowsExactly<InvalidOperationException>(() => lockable.ExitReadLock());
		StringAssert.Contains(ex6.Message, "Incorrect read lock exit");
	}

	[TestMethod]
	public void WriteLockGuaranteesExclusiveAccess()
	{
		RunTestOnLock((_, lockable) =>
		{
			var sharedValue = 0;
			var concurrentWritersDetected = false;
			var concurrentWriters = 0;
			var writerCount = 5;
			var operationsPerWriter = 10;

			// Create several writer tasks
			var tasks = new Task[writerCount];
			for (var i = 0; i < writerCount; i++)
			{
				tasks[i] = Task.Run(() =>
				{
					for (var j = 0; j < operationsPerWriter; j++)
					{
						lockable.EnterWriteLock();
						try
						{
							// Track concurrent writers - if we ever see more than one, flag it
							if (Interlocked.Increment(ref concurrentWriters) > 1)
							{
								concurrentWritersDetected = true;
							}

							var current = sharedValue;
							Thread.Sleep(0);
							sharedValue = current + 1;
						}
						finally
						{
							Interlocked.Decrement(ref concurrentWriters);
							lockable.ExitWriteLock();
						}
					}
				});
			}

			Task.WaitAll(tasks);

			IsFalse(concurrentWritersDetected, () => "There should never be more than one writer at a time");

			// Verify the shared value was incremented correctly
			AreEqual(writerCount * operationsPerWriter, sharedValue,
				() => "Final value should match the total number of operations"
			);
		}, true);
	}

	#endregion
}