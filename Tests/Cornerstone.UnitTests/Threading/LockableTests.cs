#region References

using System;
using System.Threading.Tasks;
using Cornerstone.Extensions;
using Cornerstone.Testing;
using Cornerstone.Text;
using Cornerstone.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

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
	public void UpgradeableReadLockCanShareWithOtherReaders()
	{
		var orderedSteps = new TestCoordinator();
		var lockable = new ReaderWriterLockTiny();
		IsTrue(lockable is { IsReadLockHeld: false, IsWriteLockHeld: false });
		var builder = new TextBuilder();

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

		orderedSteps.WaitForStepToComplete(4);

		AreEqual("+1-1+2-2+3-3+4-4", builder);
	}

	[TestMethod]
	public void UpgradeableReadLockWillWaitForCompletedReaders()
	{
		var orderedSteps = new TestCoordinator();
		var lockable = new ReaderWriterLockTiny();
		IsTrue(lockable is { IsReadLockHeld: false, IsWriteLockHeld: false });
		var builder = new TextBuilder();

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
				var result = lockable.WaitUntil(x => x.IsAwaitingWriteLock, 1000, 10);
				IsTrue(result, "The thread was never awaiting lock...");
				builder.Append("+3");
				lockable.ExitReadLock();
				builder.Append("-3");
			});
		});

		orderedSteps.WaitForStepToComplete(2);

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

		builder.Append("-W");

		IsFalse(task.IsFaulted, task.Exception.ToDetailedString());
		AreEqual("+1-1+2-2+W+3-3+4-4-W", builder);
	}

	[TestMethod]
	public void UpgradeableReadLockWithExistingReaders()
	{
		var orderedSteps = new TestCoordinator();
		var lockable = new ReaderWriterLockTiny();
		//var lockable = new ReaderWriterLockSlimProxy();
		IsTrue(lockable is { IsReadLockHeld: false, IsWriteLockHeld: false });
		var builder = new TextBuilder();

		Environment.CurrentManagedThreadId.Dump("Test Thread: ");

		orderedSteps.ProcessStep(1, () =>
		{
			builder.Append("+R1");
			lockable.EnterReadLock();
			IsTrue(lockable is { IsReadLockHeld: true, IsWriteLockHeld: false });
		});

		var task = Task.Run(() =>
		{
			Environment.CurrentManagedThreadId.Dump("Task Thread: ");

			orderedSteps.ProcessStep(2, () =>
			{
				builder.Append("+UR2");
				lockable.EnterUpgradeableReadLock();
				IsTrue(lockable is { IsReadLockHeld: true, IsWriteLockHeld: false });
			});

			orderedSteps.ProcessStep(3, () =>
			{
				// Wait until the main test thread is awaiting the write lock
				// once it's waiting we can release the read lock
				var result = lockable.WaitUntil(x => x.IsAwaitingWriteLock, WaitTimeout, 10);
				IsTrue(result, "The thread was never awaiting lock...");
				builder.Append("-UR2");
				lockable.ExitUpgradeableReadLock();
				builder.Append("-R1");
				lockable.ExitReadLock();
			});
		});

		orderedSteps.WaitForStepToComplete(2);

		Assert.Inconclusive("Enter Write lock never triggers IsAwaitingWriteLock needed above");

		// Should block and wait for step 3 to complete
		builder.Append("+W1");
		lockable.EnterWriteLock();
		IsTrue(lockable is { IsWriteLockHeld: true });

		orderedSteps.ProcessStep(4, () =>
		{
			builder.Append("-W1");
			lockable.ExitWriteLock();
			IsTrue(lockable is { IsWriteLockHeld: false });
		});

		IsFalse(task.IsFaulted, task.Exception.ToDetailedString());
		AreEqual("+R1+UR2+W1-UR2-R1-W1", builder);
	}

	#endregion
}