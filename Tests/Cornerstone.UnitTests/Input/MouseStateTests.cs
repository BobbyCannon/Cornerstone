#region References

using System;
using Cornerstone.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Input;

[TestClass]
public class MouseStateTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void IsButtonUpDown()
	{
		var scenarios = new (MouseButton button, Action<MouseState> update)[]
		{
			(MouseButton.LeftButton, x => x.LeftButton = true),
			(MouseButton.MiddleButton, x => x.MiddleButton = true),
			(MouseButton.RightButton, x => x.RightButton = true),
			(MouseButton.XButton1, x => x.XButton1 = true),
			(MouseButton.XButton2, x => x.XButton2 = true)
		};

		foreach (var scenario in scenarios)
		{
			var state = new MouseState();
			IsFalse(state.IsButtonDown(scenario.button));

			scenario.update(state);
			IsTrue(state.IsButtonDown(scenario.button));
		}
	}

	#endregion
}