#region References

using System;
using System.Linq;
using Cornerstone.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Input;

[TestClass]
public class ExpectedKeyMonitorTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ShouldMatch()
	{
		var scenarios = GetScenarios();

		foreach (var scenario in scenarios)
		{
			scenario.binding.Reset();

			var actual = false;

			for (var index = 1; index <= scenario.states.Length; index++)
			{
				var state = scenario.states[index - 1];
				actual |= scenario.binding.Process(state);
			}

			IsTrue(actual);
		}
	}

	[TestMethod]
	public void ShouldNotMatch()
	{
		// Require missing modifiers
		TestMatch((_, y) => y.IsAltRequired = true, false);
		TestMatch((_, y) => y.IsLeftAltRequired = true, false);
		TestMatch((_, y) => y.IsRightAltRequired = true, false);

		TestMatch((_, y) => y.IsControlRequired = true, false);
		TestMatch((_, y) => y.IsLeftControlRequired = true, false);
		TestMatch((_, y) => y.IsRightControlRequired = true, false);

		TestMatch((_, y) => y.IsShiftRequired = true, false);
		TestMatch((_, y) => y.IsLeftShiftRequired = true, false);
		TestMatch((_, y) => y.IsRightShiftRequired = true, false);

		TestMatch((_, y) => y.Key = KeyboardKey.B, false);
	}

	[TestMethod]
	public void ShouldNotMatchInReverseOrder()
	{
		var scenarios = GetScenarios();

		// Reverse order
		foreach (var scenario in scenarios.Where(x => x.states.Length > 1))
		{
			scenario.binding.Reset();

			for (var index = scenario.states.Length - 1; index >= 0; index--)
			{
				var state = scenario.states[index];
				var actual = scenario.binding.Process(state);

				IsFalse(actual);
			}
		}
	}

	private static (ExpectedKeyMonitor binding, KeyboardState[] states)[] GetScenarios()
	{
		return
		[
			(
				new ExpectedKeyMonitor(
					new ExpectedKeyState { Key = KeyboardKey.K, IsControlRequired = true }
				),
				[
					new KeyboardState { Character = 'k', IsControlPressed = true, IsLeftControlPressed = true, IsPressed = true, Key = KeyboardKey.K }
				]
			),
			(
				new ExpectedKeyMonitor(
					new ExpectedKeyState { Key = KeyboardKey.K, IsControlRequired = true },
					new ExpectedKeyState { Key = KeyboardKey.M, IsControlRequired = true }
				),
				[
					new KeyboardState { IsControlPressed = true, IsLeftControlPressed = true, IsPressed = true, Key = KeyboardKey.LeftControl },
					new KeyboardState { Character = 'k', IsControlPressed = true, IsLeftControlPressed = true, IsPressed = true, Key = KeyboardKey.K },
					new KeyboardState { Character = 'k', IsControlPressed = true, IsLeftControlPressed = true, Key = KeyboardKey.K },
					new KeyboardState { Character = 'm', IsControlPressed = true, IsLeftControlPressed = true, IsPressed = true, Key = KeyboardKey.M },
					new KeyboardState { Character = 'm', IsControlPressed = true, IsLeftControlPressed = true, Key = KeyboardKey.M },
					new KeyboardState { Key = KeyboardKey.LeftControl }
				]
			)
		];
	}

	private void TestMatch(Action<KeyboardState, ExpectedKeyState> updateState, bool expected)
	{
		var state = new KeyboardState { Key = KeyboardKey.A };
		var binding = new ExpectedKeyMonitor(new ExpectedKeyState { Key = KeyboardKey.A });
		updateState(state, binding.Keys[0]);
		AreEqual(expected, binding.Process(state));
	}

	#endregion
}