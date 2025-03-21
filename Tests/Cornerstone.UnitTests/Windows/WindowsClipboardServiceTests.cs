#region References

using System;
using Cornerstone.Extensions;
using Cornerstone.Platforms.Windows;
using NUnit.Framework;

#endregion

namespace Cornerstone.UnitTests.Windows;

public class WindowsClipboardServiceTests : CornerstoneUnitTest
{
	#region Methods

	[Test]
	public void SetGetClearClipboard()
	{
		var expected = Guid.NewGuid().ToString();
		var service = new WindowsClipboardService(this);
		service.SetTextAsync(expected).AwaitResults();
		AreEqual(expected, service.GetTextAsync().Result);
		service.ClearAsync().AwaitResults();
		AreEqual(null, service.GetTextAsync().Result);
	}

	#endregion
}