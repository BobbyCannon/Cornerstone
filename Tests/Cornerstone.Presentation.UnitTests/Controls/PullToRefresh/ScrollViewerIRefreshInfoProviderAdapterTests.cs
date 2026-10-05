#region References

using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.PullToRefresh;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.PullToRefresh;

[TestClass]
public class ScrollViewerIRefreshInfoProviderAdapterTests : ScopedTestBase
{
	#region Methods

	// Repro for the "gesture recognizer leaked across Adapt() calls" bug.
	//
	// Adapt() cleans up the previous ScrollViewer's pointer handlers and the previous
	// RefreshInfoProvider's pull-event handlers, but never removes the previously-created
	// ScrollablePullGestureRecognizer from _interactionSource.GestureRecognizers.
	// Each subsequent Adapt() instantiates and adds a new recognizer, so the input element
	// ends up holding N recognizers after N calls. They all listen for the same pointer
	// events and raise duplicate PullGesture/PullGestureEnded pairs, which (combined with
	// the _entered desync fix in RefreshInfoProvider) corrupts the visualizer state.
	[PresentationTestMethod]
	public void AdaptCalledTwiceDoesNotLeakPullGestureRecognizer()
	{
		var sv = new ScrollViewer
		{
			Template = new FuncControlTemplate<ScrollViewer>(ScrollViewerTests.CreateTemplate),
			Content = new Border()
		};

		// Wrap in a TestRoot and execute the layout pass so Loaded fires and the
		// visual tree under the ScrollContentPresenter is fully wired up
		// (otherwise the adapter never reaches MakeInteractionSource).
		var root = new TestRoot(sv);
		root.LayoutManager.ExecuteInitialLayoutPass();

		var adapter = new ScrollViewerIRefreshInfoProviderAdapter(PullDirection.TopToBottom, false);

		adapter.Adapt(sv, new Size(100, 100));
		var interactionSource = adapter.InteractionSource;
		CornerstoneTest.IsNotNull(interactionSource);
		var afterFirst = interactionSource.GestureRecognizers.OfType<ScrollablePullGestureRecognizer>().Count();
		CornerstoneTest.AreEqual(1, afterFirst);

		adapter.Adapt(sv, new Size(100, 100));
		var interactionSourceAfter = adapter.InteractionSource;
		CornerstoneTest.IsNotNull(interactionSourceAfter);
		var afterSecond = interactionSourceAfter.GestureRecognizers.OfType<ScrollablePullGestureRecognizer>().Count();
		CornerstoneTest.AreEqual(1, afterSecond);
	}

	#endregion
}