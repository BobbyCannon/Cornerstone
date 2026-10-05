#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.PullToRefresh;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.PullToRefresh;

[TestClass]
public class RefreshVisualizerTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void InteractionRatioIsOneWhileRefreshingAndZeroAfterCompletion()
	{
		var provider = new RefreshInfoProvider(
			PullDirection.TopToBottom,
			new Size(100, 100),
			null);

		var visualizer = new RefreshVisualizer();
		visualizer.RefreshInfoProvider = provider;

		var sawDuringRefresh = false;

		visualizer.RefreshRequested += (s, e) =>
		{
			CornerstoneTest.AreEqual(1d, provider.InteractionRatio);
			sawDuringRefresh = true;
		};

		visualizer.RequestRefresh();

		CornerstoneTest.IsTrue(sawDuringRefresh, "RefreshRequested event should have been raised");

		CornerstoneTest.AreEqual(0d, provider.InteractionRatio);
	}

	#endregion
}