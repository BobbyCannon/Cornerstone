#region References

using System.Linq;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cornerstone.Presentation.Controls;

#endregion

namespace Cornerstone.UnitTests.Presentation.Text;

[TestClass]
public class TextRendererScrollTests : CornerstoneCornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ArrangeAppliesViewMetricsOffsetWhenViewportIsReal()
	{
		RunOnUi(() =>
		{
			var renderer = CreateRenderer(new Vector(0, 400));
			renderer.ViewModel.IsRestoringViewport = true;
			renderer.Measure(new Size(200, 150));
			renderer.ViewModel.ViewMetrics.Offset = new Vector(0, 400);
			renderer.Arrange(new Rect(0, 0, 200, 150));

			AreEqual(new Vector(0, 400), renderer.Offset);
			AreEqual(new Vector(0, 400), renderer.ViewModel.ViewMetrics.Offset);
			IsFalse(renderer.ViewModel.IsRestoringViewport);
		});
	}

	[TestMethod]
	public void AutoScrollDoesNotPersistCoercedZeroOffset()
	{
		RunOnUi(() =>
		{
			var renderer = CreateRenderer(new Vector(0, 400));
			renderer.ViewModel.AutoScroll = true;
			renderer.ViewModel.IsRestoringViewport = false;
			renderer.ViewModel.ViewMetrics.Viewport = new Size(200, 150);
			renderer.ViewModel.ViewMetrics.DocumentSize = new Size(200, 1000);

			renderer.Offset = default;

			AreEqual(new Vector(0, 400), renderer.ViewModel.ViewMetrics.Offset);
		});
	}

	[TestMethod]
	public void CollapsedArrangeDoesNotWipeViewMetricsOffset()
	{
		RunOnUi(() =>
		{
			var renderer = CreateRenderer(new Vector(0, 400));
			renderer.Measure(new Size(200, 0));
			renderer.Arrange(new Rect(0, 0, 200, 0));

			AreEqual(new Vector(0, 400), renderer.ViewModel.ViewMetrics.Offset);
			IsTrue(renderer.ViewModel.IsRestoringViewport);
		});
	}

	[TestMethod]
	public void CollapsedThenRealArrangeRestoresViewMetricsOffset()
	{
		RunOnUi(() =>
		{
			var renderer = CreateRenderer(new Vector(0, 400));
			renderer.Measure(new Size(200, 150));
			renderer.ViewModel.ViewMetrics.Offset = new Vector(0, 400);
			renderer.Arrange(new Rect(0, 0, 200, 150));
			AreEqual(new Vector(0, 400), renderer.Offset);

			renderer.Measure(new Size(200, 0));
			renderer.Arrange(new Rect(0, 0, 200, 0));
			AreEqual(new Vector(0, 400), renderer.ViewModel.ViewMetrics.Offset);
			IsTrue(renderer.ViewModel.IsRestoringViewport);

			renderer.Offset = default;
			AreEqual(new Vector(0, 400), renderer.ViewModel.ViewMetrics.Offset);

			renderer.Measure(new Size(200, 150));
			renderer.Arrange(new Rect(0, 0, 200, 150));
			AreEqual(new Vector(0, 400), renderer.Offset);
			AreEqual(new Vector(0, 400), renderer.ViewModel.ViewMetrics.Offset);
			IsFalse(renderer.ViewModel.IsRestoringViewport);
		});
	}

	[TestMethod]
	public void OffsetWriteDoesNotPersistWhenNotAttached()
	{
		RunOnUi(() =>
		{
			var renderer = CreateRenderer(new Vector(0, 400));
			renderer.ViewModel.ViewMetrics.Viewport = new Size(200, 150);
			renderer.ViewModel.ViewMetrics.DocumentSize = new Size(200, 1000);

			renderer.Offset = default;

			AreEqual(new Vector(0, 400), renderer.ViewModel.ViewMetrics.Offset);
		});
	}

	[TestMethod]
	public void OffsetWriteDoesNotPersistWhileRestoringViewport()
	{
		RunOnUi(() =>
		{
			var renderer = CreateRenderer(new Vector(0, 400));
			renderer.ViewModel.ViewMetrics.Viewport = new Size(200, 150);
			renderer.ViewModel.ViewMetrics.DocumentSize = new Size(200, 1000);
			renderer.ViewModel.IsRestoringViewport = true;

			renderer.Offset = default;

			AreEqual(new Vector(0, 400), renderer.ViewModel.ViewMetrics.Offset);
		});
	}

	private static TextRenderer CreateRenderer(Vector offset)
	{
		var renderer = new TextRenderer { FontSize = 16 };
		renderer.ViewModel.Load(string.Join("\r\n", Enumerable.Repeat("line of terminal output", 80)));
		renderer.ViewModel.ViewMetrics.Offset = offset;
		return renderer;
	}

	#endregion
}