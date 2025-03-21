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

	protected IEnumerable<IReaderWriterLock> GetLocks(bool includeSlim)
	{
		yield return new ReaderWriterLockTiny();
		yield return new Lockable(new ReaderWriterLockTiny());
		yield return new ReaderWriterLockBindable(new ReaderWriterLockTiny());

		if (includeSlim)
		{
			yield return new ReaderWriterLockSlimProxy();
			yield return new Lockable(new ReaderWriterLockSlimProxy());
			yield return new ReaderWriterLockBindable(new ReaderWriterLockSlimProxy());
		}
	}

	protected void RunTestOnLock(Action<TextBuilder, IReaderWriterLock> test, bool includeSlim)
	{
		foreach (var lockable in GetLocks(includeSlim))
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

	#endregion
}