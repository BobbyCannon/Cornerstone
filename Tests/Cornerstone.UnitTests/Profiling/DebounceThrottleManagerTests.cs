#region References

using System;
using System.Collections.Generic;
using System.Threading;
using Cornerstone.Extensions;
using Cornerstone.Profiling;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Profiling;

[TestClass]
public class DebounceThrottleManagerTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void DebounceShouldOnlyFireOnLastTriggeredData()
	{
		var actual = new List<int>();
		var delay = TimeSpan.FromMilliseconds(50);

		using var manager = DebounceThrottleManager.Start(this, 1);
		var debounce = manager.CreateDebounce(delay, Work);
		actual.Clear();

		debounce.Trigger(1);
		debounce.Trigger(2);
		debounce.Trigger(3);

		IncrementTime(delay);
		var r = this.WaitUntil(_ => !debounce.IsActive, (int) WaitTimeout.TotalMilliseconds, 0);
		IsTrue(r, () => "Should not have timed out... debounce never completed...");
		IsFalse(debounce.IsTriggered);

		AreEqual(1, actual.Count);
		AreEqual(3, actual[0]);
		return;

		void Work(CancellationToken token, object data, bool forced)
		{
			Console.WriteLine(data);
			actual.Add((int) data);
		}
	}

	[TestMethod]
	public void DebounceCanCancel()
	{
		var triggered = false;
		using var manager = DebounceThrottleManager.Start(this, 1);
		var service = manager.CreateDebounce(TimeSpan.FromSeconds(5), (_, _, _) => triggered = true);
		service.Trigger(null);
		IncrementTime(seconds: 4);
		service.Cancel();
		IncrementTime(seconds: 1);
		IsFalse(triggered);
	}

	#endregion
}