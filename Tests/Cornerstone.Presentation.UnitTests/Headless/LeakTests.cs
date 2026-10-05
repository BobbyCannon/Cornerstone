#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Headless;

[TestClass]
public class LeakTests
{
	#region Fields

	private static WeakReference spreviousFontManager;

	#endregion

	#region Properties

	// Three isolated runs prove collection. More blocking GC.Collect passes after a full suite stall on heap size.
	public static IEnumerable<object[]> TestData { get; } =
		Enumerable.Range(0, 3).Select(i => new object[] { i.ToString() });

	#endregion

	#region Methods

	[TestData(nameof(TestData))]
	[HeadlessTestMethod]
	public void PreviousFontManagerShouldBeCollected(string data)
	{
		// Arrange
		var fontManager = new WeakReference(FontManager.Current);
		var button = new Button { Content = data };
		var window = new Window
		{
			Content = button
		};

		// Act, just some interaction, to make sure the FontManager is actually used
		window.Show();
		button.Focus();
		window.MouseDown(new Point(1, 1), MouseButton.Left);
		window.Close();

		// Assert
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		// Either previous font manager is collected (IsAlive == false), or it is the same as current (shared isolation mode).
		if (spreviousFontManager is not null && (spreviousFontManager.Target != fontManager.Target))
		{
			AssertHelper.False(spreviousFontManager.IsAlive);
		}

		spreviousFontManager = fontManager;
	}

	#endregion
}