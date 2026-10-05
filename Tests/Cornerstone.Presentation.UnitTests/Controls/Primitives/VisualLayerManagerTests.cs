#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Primitives;

[TestClass]
public class VisualLayerManagerTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void GetAdornerLayerReturnsDedicatedAdornerLayerForControlsInsideOverlayLayer()
	{
		var button = new Button();
		var vlm = new VisualLayerManager { EnableOverlayLayer = true, Child = button };
		var root = new TestRoot { Child = vlm };

		root.Measure(new Size(100, 100));
		root.Arrange(new Rect(0, 0, 100, 100));

		var overlayLayer = vlm.OverlayLayer;
		CornerstoneTest.IsNotNull(overlayLayer);

		var overlayChild = new Border();
		overlayLayer.Children.Add(overlayChild);

		// The adorner layer for a control inside the OverlayLayer
		// should be the dedicated one, not the main VLM adorner layer.
		var overlayAdornerLayer = AdornerLayer.GetAdornerLayer(overlayChild);
		CornerstoneTest.IsNotNull(overlayAdornerLayer);
		CornerstoneTest.Same(overlayLayer.AdornerLayer, overlayAdornerLayer);

		// The main VLM adorner layer should be different.
		var mainAdornerLayer = AdornerLayer.GetAdornerLayer(button);
		CornerstoneTest.IsNotNull(mainAdornerLayer);
		CornerstoneTest.NotSame(overlayAdornerLayer, mainAdornerLayer);
	}

	[PresentationTestMethod]
	public void GetAdornerLayerReturnsSameAdornerLayerForChild()
	{
		var button = new Button();
		var vlm = new VisualLayerManager { Child = button };
		var root = new TestRoot { Child = vlm };

		root.Measure(new Size(100, 100));
		root.Arrange(new Rect(0, 0, 100, 100));

		var adornerLayer = vlm.AdornerLayer;
		CornerstoneTest.IsNotNull(adornerLayer);

		// The adorner layer for a control inside the OverlayLayer
		// should be the dedicated one, not the main VLM adorner layer.
		var target = AdornerLayer.GetAdornerLayer(button);
		CornerstoneTest.IsNotNull(target);
		CornerstoneTest.Same(adornerLayer, target);
	}

	[PresentationTestMethod]
	public void GetAdornerLayerReturnsSameAdornerLayerForVisualLayerManager()
	{
		var vlm = new VisualLayerManager();
		var root = new TestRoot { Child = vlm };

		root.Measure(new Size(100, 100));
		root.Arrange(new Rect(0, 0, 100, 100));

		var adornerLayer = vlm.AdornerLayer;
		CornerstoneTest.IsNotNull(adornerLayer);

		// The adorner layer for a control inside the OverlayLayer
		// should be the dedicated one, not the main VLM adorner layer.
		var target = AdornerLayer.GetAdornerLayer(vlm);
		CornerstoneTest.IsNotNull(target);
		CornerstoneTest.Same(adornerLayer, target);
	}

	[PresentationTestMethod]
	public void GetOverlayLayerReturnsSameOverlayLayerForChild()
	{
		var button = new Button();
		var vlm = new VisualLayerManager { EnableOverlayLayer = true, Child = button };
		var root = new TestRoot { Child = vlm };

		root.Measure(new Size(100, 100));
		root.Arrange(new Rect(0, 0, 100, 100));

		var overlayLayer = vlm.OverlayLayer;
		CornerstoneTest.IsNotNull(overlayLayer);

		// The adorner layer for a control inside the OverlayLayer
		// should be the dedicated one, not the main VLM adorner layer.
		var target = OverlayLayer.GetOverlayLayer(button);
		CornerstoneTest.IsNotNull(target);
		CornerstoneTest.Same(overlayLayer, target);
	}

	[PresentationTestMethod]
	public void GetOverlayLayerReturnsSameOverlayLayerForVisualLayerManager()
	{
		var vlm = new VisualLayerManager { EnableOverlayLayer = true };
		var root = new TestRoot { Child = vlm };

		root.Measure(new Size(100, 100));
		root.Arrange(new Rect(0, 0, 100, 100));

		var overlayLayer = vlm.OverlayLayer;
		CornerstoneTest.IsNotNull(overlayLayer);

		// The adorner layer for a control inside the OverlayLayer
		// should be the dedicated one, not the main VLM adorner layer.
		var target = OverlayLayer.GetOverlayLayer(vlm);
		CornerstoneTest.IsNotNull(target);
		CornerstoneTest.Same(overlayLayer, target);
	}

	#endregion
}