#region References

using System;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Animation;

[TestClass]
public class TransitionsTests
{
	#region Methods

	[PresentationTestMethod]
	public void CheckTransitionsInterpolationNegativeBoundsClamp()
	{
		var clock = new TestClock();

		var border = new Border
		{
			Transitions = new Transitions
			{
				new DoubleTransition
				{
					Duration = TimeSpan.FromSeconds(1), Property = Visual.OpacityProperty
				}
			}
		};

		border.Opacity = 0;

		clock.Pulse(TimeSpan.FromSeconds(0));
		clock.Pulse(TimeSpan.FromSeconds(-0.5));

		CornerstoneTest.AreEqual(0, border.Opacity);
	}

	[PresentationTestMethod]
	public void CheckTransitionsInterpolationPositiveBoundsClamp()
	{
		var clock = new TestClock();

		var border = new Border
		{
			Transitions = new Transitions
			{
				new DoubleTransition
				{
					Duration = TimeSpan.FromSeconds(1), Property = Visual.OpacityProperty
				}
			}
		};

		border.Opacity = 0;

		clock.Pulse(TimeSpan.FromSeconds(0));
		clock.Pulse(TimeSpan.FromMilliseconds(1001));

		CornerstoneTest.AreEqual(0, border.Opacity);
	}

	[PresentationTestMethod]
	public void TransitionInstanceProperlyCalculatesDelayAndDurationValues()
	{
		var clock = new TestClock();

		var i = -1;
		var completed = false;

		new TransitionInstance(clock, TimeSpan.FromMilliseconds(30), TimeSpan.FromMilliseconds(70)).Subscribe(
			nextValue =>
			{
				switch (i++)
				{
					case 0:
						CornerstoneTest.AreEqual(0, nextValue);
						break;
					case 1:
						CornerstoneTest.AreEqual(0, nextValue);
						break;
					case 2:
						CornerstoneTest.AreEqual(0, nextValue);
						break;
					case 3:
						CornerstoneTest.AreEqual(0, nextValue);
						break;
					case 4:
						CornerstoneTest.AreEqual(Math.Round(10d / 70d, 4), Math.Round(nextValue, 4));
						break;
					case 5:
						CornerstoneTest.AreEqual(Math.Round(20d / 70d, 4), Math.Round(nextValue, 4));
						break;
					case 6:
						CornerstoneTest.AreEqual(Math.Round(30d / 70d, 4), Math.Round(nextValue, 4));
						break;
					case 7:
						CornerstoneTest.AreEqual(Math.Round(40d / 70d, 4), Math.Round(nextValue, 4));
						break;
					case 8:
						CornerstoneTest.AreEqual(Math.Round(50d / 70d, 4), Math.Round(nextValue, 4));
						break;
					case 9:
						CornerstoneTest.AreEqual(Math.Round(60d / 70d, 4), Math.Round(nextValue, 4));
						break;
					case 10:
						CornerstoneTest.AreEqual(1d, nextValue);
						break;
				}
			}, () => completed = true);

		for (var z = 0; z <= 10; z++)
		{
			clock.Pulse(TimeSpan.FromMilliseconds(10));
		}

		CornerstoneTest.IsTrue(completed);
	}

	[PresentationTestMethod]
	public void TransitionInstanceWithDelayButZeroDurationIsCompletedAfterDelay()
	{
		var clock = new TestClock();

		var i = -1;
		var completed = false;

		new TransitionInstance(clock, TimeSpan.FromMilliseconds(30), TimeSpan.Zero).Subscribe(
			nextValue =>
			{
				switch (i++)
				{
					case 0:
						CornerstoneTest.AreEqual(0, nextValue);
						break;
					case 1:
						CornerstoneTest.AreEqual(0, nextValue);
						break;
					case 2:
						CornerstoneTest.AreEqual(0, nextValue);
						break;
					case 3: // one iteration sooner than the test above, because the start of the transition is also the end
						CornerstoneTest.AreEqual(1, nextValue);
						break;
				}
			}, () => completed = true);

		for (var z = 0; z <= 4; z++)
		{
			clock.Pulse(TimeSpan.FromMilliseconds(10));
		}

		CornerstoneTest.IsTrue(completed);
	}

	[PresentationTestMethod]
	public void TransitionInstanceWithZeroDurationIsCompletedOnFirstTick()
	{
		var clock = new TestClock();

		var i = 0;

		new TransitionInstance(clock, TimeSpan.Zero, TimeSpan.Zero).Subscribe(nextValue =>
		{
			switch (i++)
			{
				case 0:
					CornerstoneTest.AreEqual(0, nextValue);
					break;
				case 1:
					CornerstoneTest.AreEqual(1d, nextValue);
					break;
			}
		});

		clock.Pulse(TimeSpan.FromMilliseconds(10));
	}

	#endregion
}