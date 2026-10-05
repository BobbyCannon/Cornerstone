#region References

using Cornerstone.Presentation.Controls.PullToRefresh;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.PullToRefresh;

[TestClass]
public class RefreshInfoProviderTests : ScopedTestBase
{
	#region Methods

	// Repro for the typo where horizontal pulls checked Height==0 instead of Width==0.
	// With Width==0, value.X / Width produces +Infinity / NaN, which then breaks every
	// downstream consumer of InteractionRatio (Math.Min(1, NaN) returns NaN).
	[PresentationTestMethod]
	public void HorizontalPullWithZeroWidthProducesSafeInteractionRatio()
	{
		var provider = new RefreshInfoProvider(
			PullDirection.LeftToRight,
			new Size(0, 100),
			null);

		provider.ValuesChanged(new Vector(50, 0));

		CornerstoneTest.IsFalse(double.IsNaN(provider.InteractionRatio));
		CornerstoneTest.IsFalse(double.IsInfinity(provider.InteractionRatio));
	}

	// Repro for the "_entered desync" bug.
	//
	// Real-world flow that triggers it:
	//   1. User starts pulling.   ScrollablePullGestureRecognizer raises PullGestureEvent
	//                             -> InteractingStateEntered sets IsInteractingForRefresh = true
	//                                and _entered = true.
	//   2. The pull motion produces a small ScrollChanged that pushes the scroll offset
	//      past the threshold. ScrollViewerIRefreshInfoProviderAdapter.ScrollViewer_ScrollChanged
	//      writes IsInteractingForRefresh = false directly, bypassing PullGestureEnded.
	//      (ScrollViewer_PointerReleased does the same.)
	//   3. _entered stays true.
	//   4. The user is still pulling, more PullGestureEvents arrive.
	//      InteractingStateEntered short-circuits because _entered is already true,
	//      so IsInteractingForRefresh is NOT reasserted.
	//   5. RefreshVisualizer never re-enters the Interacting state -> spinner does not appear.
	[PresentationTestMethod]
	public void IsInteractingForRefreshIsReassertedAfterBeingClearedExternally()
	{
		var provider = new RefreshInfoProvider(
			PullDirection.TopToBottom,
			new Size(100, 100),
			null);

		var pullArgs = new PullGestureEventArgs(0, new Vector(0, 50), PullDirection.TopToBottom);

		// 1. First PullGestureEvent of a gesture
		provider.InteractingStateEntered(this, pullArgs);
		CornerstoneTest.IsTrue(provider.IsInteractingForRefresh, "IsInteractingForRefresh should be true after the first PullGestureEvent");

		// 2. Adapter clears the flag directly (simulating ScrollViewer_ScrollChanged
		//    or ScrollViewer_PointerReleased)
		provider.IsInteractingForRefresh = false;
		CornerstoneTest.IsFalse(provider.IsInteractingForRefresh);

		// 3. Pull is still in progress, the next PullGestureEvent arrives
		provider.InteractingStateEntered(this, pullArgs);

		// BUG: stays false because _entered short-circuits the assignment.
		// After the fix this assertion must pass.
		CornerstoneTest.IsTrue(provider.IsInteractingForRefresh, "PullGestureEvent must re-assert IsInteractingForRefresh after it was cleared by something other than PullGestureEnded");
	}

	// Sanity check for the existing happy-path: a complete gesture lifecycle
	// (Entered -> Exited -> Entered) must toggle IsInteractingForRefresh correctly.
	[PresentationTestMethod]
	public void NormalGestureLifecycleTogglesIsInteractingForRefreshCorrectly()
	{
		var provider = new RefreshInfoProvider(
			PullDirection.TopToBottom,
			new Size(100, 100),
			null);

		var pullArgs = new PullGestureEventArgs(0, new Vector(0, 50), PullDirection.TopToBottom);
		var endArgs = new PullGestureEndedEventArgs(0, PullDirection.TopToBottom);

		provider.InteractingStateEntered(this, pullArgs);
		CornerstoneTest.IsTrue(provider.IsInteractingForRefresh);

		provider.InteractingStateExited(this, endArgs);
		CornerstoneTest.IsFalse(provider.IsInteractingForRefresh);

		// Next gesture should work
		provider.InteractingStateEntered(this, pullArgs);
		CornerstoneTest.IsTrue(provider.IsInteractingForRefresh);
	}

	#endregion
}