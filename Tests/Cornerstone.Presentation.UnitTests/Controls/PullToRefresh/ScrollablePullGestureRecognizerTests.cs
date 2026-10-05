#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.PullToRefresh;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.PullToRefresh;

[TestClass]
public class ScrollablePullGestureRecognizerTests : ScopedTestBase
{
	#region Methods

	// Repro for the "PointerCaptureLost doesn't clean up state" bug.
	//
	// PointerReleased clears _pullInProgress, _tracking, _initialPosition.
	// PointerCaptureLost only raises EndPull (so the PullGestureEnded event fires)
	// but leaves _pullInProgress == true and _tracking pointing at the lost pointer.
	// The next gesture re-enters PointerMoved with (_pullInProgress=true, pulling=true)
	// and goes straight to HandlePull, skipping BeginPull. This reuses the OLD _gestureId
	// for the next PullGestureEvent - the same id that was just used in the
	// PullGestureEndedEvent for the previous gesture.
	[PresentationTestMethod]
	public void GestureAfterPointerCaptureLostUsesANewId()
	{
		var scrollable = new TestScrollable();
		var recognizer = new ScrollablePullGestureRecognizer(PullDirection.TopToBottom, true);
		scrollable.GestureRecognizers.Add(recognizer);

		var root = new TestRoot(scrollable);

		var pullIds = new List<int>();
		var endedIds = new List<int>();
		root.AddHandler(InputElement.PullGestureEvent, (_, e) => pullIds.Add(e.Id));
		root.AddHandler(InputElement.PullGestureEndedEvent, (_, e) => endedIds.Add(e.Id));

		var touch = new TouchTestHelper();

		// First gesture: start pulling downwards so the recognizer begins a pull and captures the pointer.
		touch.Down(scrollable, new Point(10, 20));
		touch.Move(scrollable, new Point(10, 70));

		CornerstoneTest.Single(pullIds);
		var firstGestureId = pullIds[0];

		// Capture is lost (e.g. another control steals it, or the visual is detached mid-gesture)
		touch.Cancel();

		CornerstoneTest.AreEqual([firstGestureId], endedIds);

		// Second gesture must begin cleanly (BeginPull, not HandlePull) and therefore get a fresh id.
		touch.Down(scrollable, new Point(10, 20));
		touch.Move(scrollable, new Point(10, 70));

		CornerstoneTest.AreEqual(2, pullIds.Count);
		CornerstoneTest.AreNotEqual(firstGestureId, pullIds[1]);
	}

	#endregion

	#region Classes

	private sealed class TestScrollable : Decorator, IScrollable
	{
		#region Properties

		public bool CanHorizontallyScroll => false;
		public bool CanVerticallyScroll => true;
		public Size Extent => new(100, 1000);
		public Vector Offset { get; set; }
		public Size Viewport => new(100, 100);

		#endregion
	}

	#endregion
}