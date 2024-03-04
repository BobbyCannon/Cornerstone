#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cornerstone.Presentation;
using Cornerstone.Testing;
using Cornerstone.Text;
using Cornerstone.Threading;

#endregion

namespace Cornerstone.UnitTests.Threading;

public abstract class BaseLockableTests : CornerstoneUnitTest
{
	#region Methods

	protected void RunTestOnLock(Action<TextBuilder, IReaderWriterLock> test)
	{
		foreach (var lockable in GetLocks())
		{
			var builder = new TextBuilder();
			var name = lockable.GetType().Name.Replace("ReaderWriterLock", "");
			builder.Append($"{name}: ");

			var watch = Stopwatch.StartNew();
			test.Invoke(builder, lockable);
			watch.Stop();
			
			builder.Append(watch.Elapsed.TotalMilliseconds.ToString("F4"));
			builder.Dump();

			if (lockable is IDisposable d)
			{
				d.Dispose();
			}
		}
	}

	protected IEnumerable<IReaderWriterLock> GetLocks()
	{
		var response = new IReaderWriterLock[]
		{
			new ReaderWriterLockTiny(),
			new ReaderWriterLockSlimProxy(),
			new Lockable(new ReaderWriterLockTiny()),
			new Lockable(new ReaderWriterLockSlimProxy()),
			new ReaderWriterLockBindable(new ReaderWriterLockTiny()),
			new ReaderWriterLockBindable(new ReaderWriterLockSlimProxy())
		};

		return response;
	}

	#endregion
}