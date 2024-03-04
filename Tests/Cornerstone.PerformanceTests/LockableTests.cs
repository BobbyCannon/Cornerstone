#region References

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Testing;
using Cornerstone.Text;
using Cornerstone.Threading;
using Cornerstone.UnitTests.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.PerformanceTests;

[TestClass]
public class LockableTests : BaseLockableTests
{
	#region Methods

	[TestMethod]
	public void CompareLocks()
	{
		var locks = GetLocks();
		var loops = 100_000;

		foreach (var lockable in locks)
		{
			// Warmup
			RunLock(lockable);

			var watch = Stopwatch.StartNew();

			for (var i = 0; i < loops; i++)
			{
				RunLock(lockable);
			}

			watch.Stop();
			var name = lockable.GetType().Name.Replace("ReaderWriterLock", "");
			watch.Elapsed.TotalMilliseconds.Dump($"{name}: ");

			if (lockable is IDisposable d)
			{
				d.Dispose();
			}
		}
	}

	[TestMethod]
	public void ReadAndWriteRequestShouldBeOrdered()
	{
		RunTestOnLock((builder, lockable) =>
		{
			var task1 = StartRead(lockable, 50, builder);
			Thread.Sleep(1);
			var task2 = StartWrite(lockable, 30, builder);
			Thread.Sleep(1);
			var task3 = StartRead(lockable, 25, builder);
			Thread.Sleep(1);
			Task.WaitAll(task1, task2, task3);
			IsTrue(builder.ToString().EndsWith("R:50,W:30,R:25,"), builder.ToString());
		});
	}

	private void RunLock(IReaderWriterLock locker)
	{
		IsFalse(locker.IsReadLockHeld);
		IsFalse(locker.IsWriteLockHeld);
		locker.EnterReadLock();
		IsTrue(locker.IsReadLockHeld);
		IsFalse(locker.IsWriteLockHeld);
		locker.ExitReadLock();
		locker.EnterWriteLock();
		IsFalse(locker.IsReadLockHeld);
		IsTrue(locker.IsWriteLockHeld);
		locker.ExitWriteLock();
		IsFalse(locker.IsReadLockHeld);
		IsFalse(locker.IsWriteLockHeld);
	}

	private Task StartRead(IReaderWriterLock readerWriterLock, int delay, TextBuilder builder)
	{
		return Task.Run(() =>
		{
			try
			{
				readerWriterLock.EnterReadLock();
				builder.Append($"R:{delay},");
				Thread.Sleep(delay);
			}
			finally
			{
				readerWriterLock.ExitReadLock();
			}
		});
	}

	private Task StartWrite(IReaderWriterLock readerWriterLock, int delay, TextBuilder builder)
	{
		return Task.Run(() =>
		{
			try
			{
				readerWriterLock.EnterWriteLock();
				builder.Append($"W:{delay},");
				Thread.Sleep(delay);
			}
			finally
			{
				readerWriterLock.ExitWriteLock();
			}
		});
	}

	#endregion
}