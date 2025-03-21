#region References

using System;
using Avalonia.Headless.NUnit;
using Cornerstone.Avalonia.Controls;
using Cornerstone.Testing;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Avalonia.Controls;

[TestFixture]
public class TimeSinceUntilTests : CornerstoneUnitTest
{
	#region Methods

	[AvaloniaTest]
	public void Elapsed()
	{
		var start = DateTimeOffset.Parse("1998-02-06T00:00:00Z");
		var end = DateTimeOffset.Parse("2025-02-07T00:00:00Z");

		SetTime(end.UtcDateTime);

		var control = new TimeSinceUntil { Start = start };
		control.Refresh(this);

		var elapsed = end - start;
		elapsed.Dump();
		AreEqual(elapsed, control.Elapsed);
		AreEqual("27 Years and 1 Day", control.ElapsedText);
	}

	#endregion
}