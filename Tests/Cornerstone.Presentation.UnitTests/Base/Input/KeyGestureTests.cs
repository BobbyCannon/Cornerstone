#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Input;

[TestClass]
public class KeyGestureTests
{
	#region Fields

	public static readonly IEnumerable<object[]> ParseData = new[]
	{
		new object[] { "Ctrl+A", new KeyGesture(Key.A, KeyModifiers.Control) },
		new object[] { "  \tShift\t+Alt +B", new KeyGesture(Key.B, KeyModifiers.Shift | KeyModifiers.Alt) },
		new object[] { "Control++", new KeyGesture(Key.OemPlus, KeyModifiers.Control) },
		new object[] { "Shift+⌘+A", new KeyGesture(Key.A, KeyModifiers.Meta | KeyModifiers.Shift) },
		new object[] { "Shift+Cmd+A", new KeyGesture(Key.A, KeyModifiers.Meta | KeyModifiers.Shift) },
		new object[] { "None", new KeyGesture(Key.None) },
		new object[] { "Alt+Shift", new KeyGesture(Key.None, KeyModifiers.Alt | KeyModifiers.Shift) }
	};

	public static readonly IEnumerable<object[]> ToStringData = new[]
	{
		new object[] { new KeyGesture(Key.A), "A" },
		new object[] { new KeyGesture(Key.A, KeyModifiers.Control), "Ctrl+A" },
		new object[] { new KeyGesture(Key.A, KeyModifiers.Control | KeyModifiers.Shift), "Ctrl+Shift+A" },
		new object[] { new KeyGesture(Key.A, KeyModifiers.Alt | KeyModifiers.Shift), "Shift+Alt+A" },
		new object[] { new KeyGesture(Key.A, KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Shift), "Ctrl+Shift+Alt+A" },
		new object[] { new KeyGesture(Key.A, KeyModifiers.Meta | KeyModifiers.Shift), "Shift+Cmd+A" },
		new object[] { new KeyGesture(Key.None), "None" },
		new object[] { new KeyGesture(Key.None, KeyModifiers.Alt | KeyModifiers.Shift), "Shift+Alt" }
	};

	#endregion

	#region Methods

	[PresentationTestMethod]
	[TestData(nameof(ParseData))]
	public void KeyGestureIsAbleToParseSampleData(string text, KeyGesture gesture)
	{
		CornerstoneTest.AreEqual(gesture, KeyGesture.Parse(text));
	}

	[PresentationTestMethod]
	[DataRow(Key.OemMinus, Key.Subtract)]
	[DataRow(Key.OemPlus, Key.Add)]
	[DataRow(Key.OemPeriod, Key.Decimal)]
	public void KeyGestureMatchesNumPadToRegularDigit(Key gestureKey, Key pressedKey)
	{
		var keyGesture = new KeyGesture(gestureKey);

		CornerstoneTest.IsTrue(keyGesture.Matches(new KeyEventArgs
		{
			Key = pressedKey
		}));
	}

	[PresentationTestMethod]
	[TestData(nameof(ToStringData))]
	public void ToStringProducesCorrectResults(KeyGesture gesture, string expected)
	{
		CornerstoneTest.AreEqual(expected, gesture.ToString());
	}

	#endregion
}