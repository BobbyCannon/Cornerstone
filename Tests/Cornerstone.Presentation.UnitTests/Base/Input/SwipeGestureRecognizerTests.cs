#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.GestureRecognizers;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Input;

[TestClass]
public class SwipeGestureRecognizerTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void DefaultsDisableBothAxes()
	{
		var recognizer = new SwipeGestureRecognizer();

		CornerstoneTest.IsFalse(recognizer.CanHorizontallySwipe);
		CornerstoneTest.IsFalse(recognizer.CanVerticallySwipe);
	}

	[PresentationTestMethod]
	public void DoesNotRaiseSwipeWhenBothAxesAreDisabled()
	{
		var (border, root) = CreateTarget(new SwipeGestureRecognizer { Threshold = 1 });
		var touch = new TouchTestHelper();
		var swipeRaised = false;
		var endedRaised = false;

		root.AddHandler(InputElement.SwipeGestureEvent, (_, _) => swipeRaised = true);
		root.AddHandler(InputElement.SwipeGestureEndedEvent, (_, _) => endedRaised = true);

		touch.Down(border, new Point(50, 50));
		touch.Move(border, new Point(20, 20));
		touch.Up(border, new Point(20, 20));

		CornerstoneTest.IsFalse(swipeRaised);
		CornerstoneTest.IsFalse(endedRaised);
	}

	[PresentationTestMethod]
	public void EndedEventUsesSameIdAndLastVelocity()
	{
		var (border, root) = CreateTarget(new SwipeGestureRecognizer
		{
			CanHorizontallySwipe = true,
			Threshold = 1
		});
		var touch = new TouchTestHelper();
		var updateIds = new List<int>();
		var velocities = new List<Vector>();
		var endedId = 0;
		var endedVelocity = Vector.Zero;

		root.AddHandler(InputElement.SwipeGestureEvent, (_, e) =>
		{
			updateIds.Add(e.Id);
			velocities.Add(e.Velocity);
		});
		root.AddHandler(InputElement.SwipeGestureEndedEvent, (_, e) =>
		{
			endedId = e.Id;
			endedVelocity = e.Velocity;
		});

		touch.Down(border, new Point(50, 50));
		touch.Move(border, new Point(40, 50));
		touch.Move(border, new Point(30, 50));
		touch.Up(border, new Point(30, 50));

		CornerstoneTest.IsTrue(updateIds.Count >= 2);
		CornerstoneTest.All(updateIds, id => CornerstoneTest.AreEqual(updateIds[0], id));
		CornerstoneTest.AreEqual(updateIds[0], endedId);
		CornerstoneTest.AreEqual(velocities[^1], endedVelocity);
	}

	[PresentationTestMethod]
	public void MouseSwipeIsRaisedWhenEnabled()
	{
		var mouse = new MouseTestHelper();
		var (border, root) = CreateTarget(new SwipeGestureRecognizer
		{
			CanHorizontallySwipe = true,
			Threshold = 1,
			IsMouseEnabled = true
		});
		var swipeRaised = false;

		root.AddHandler(InputElement.SwipeGestureEvent, (_, _) => swipeRaised = true);

		mouse.Down(border, position: new Point(50, 50));
		mouse.Move(border, new Point(30, 50));
		mouse.Up(border, position: new Point(30, 50));

		CornerstoneTest.IsTrue(swipeRaised);
	}

	[PresentationTestMethod]
	public void MouseSwipeRequiresIsMouseEnabled()
	{
		var mouse = new MouseTestHelper();
		var (border, root) = CreateTarget(new SwipeGestureRecognizer
		{
			CanHorizontallySwipe = true,
			Threshold = 1
		});
		var swipeRaised = false;

		root.AddHandler(InputElement.SwipeGestureEvent, (_, _) => swipeRaised = true);

		mouse.Down(border, position: new Point(50, 50));
		mouse.Move(border, new Point(30, 50));
		mouse.Up(border, position: new Point(30, 50));

		CornerstoneTest.IsFalse(swipeRaised);
	}

	[PresentationTestMethod]
	public void StartsOnlyAfterThresholdIsExceeded()
	{
		var (border, root) = CreateTarget(new SwipeGestureRecognizer
		{
			CanHorizontallySwipe = true,
			Threshold = 50
		});
		var touch = new TouchTestHelper();
		var deltas = new List<Vector>();

		root.AddHandler(InputElement.SwipeGestureEvent, (_, e) => deltas.Add(e.Delta));

		touch.Down(border, new Point(5, 5));
		touch.Move(border, new Point(40, 5));

		CornerstoneTest.Empty(deltas);

		touch.Move(border, new Point(80, 5));

		CornerstoneTest.Single(deltas);
		CornerstoneTest.AreNotEqual(Vector.Zero, deltas[0]);
	}

	private static (Border Border, TestRoot Root) CreateTarget(SwipeGestureRecognizer recognizer)
	{
		var border = new Border
		{
			Width = 100,
			Height = 100
		};
		border.GestureRecognizers.Add(recognizer);

		var root = new TestRoot
		{
			Child = border
		};

		return (border, root);
	}

	#endregion
}