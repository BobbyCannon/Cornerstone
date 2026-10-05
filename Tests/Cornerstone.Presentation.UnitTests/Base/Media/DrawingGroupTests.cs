#region References

using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Rendering.SceneGraph;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class DrawingGroupTests
{
	#region Methods

	/// <summary>
	/// Regression test: DrawingGroup.DrawCore was passing uninflated localBounds to PushOpacityMask
	/// when an Effect was also set, meaning the opacity mask didn't cover the effect output region.
	/// </summary>
	[PresentationTestMethod]
	public void DrawCoreWithEffectAndOpacityMaskShouldUseEffectBoundsForOpacityMask()
	{
		using (UnitTestApplication.Start(new TestServices(renderInterface: new HeadlessPlatformRenderInterface())))
		{
			var effect = new BlurEffect { Radius = 10 };
			var contentBounds = new Rect(10, 10, 100, 100);
			var expectedEffectBounds = contentBounds.Inflate(effect.GetEffectOutputPadding());

			var group = new DrawingGroup
			{
				Effect = effect,
				EffectBounds = contentBounds,
				OpacityMask = Brushes.Red
			};
			group.Children.Add(new GeometryDrawing
			{
				Brush = Brushes.Blue,
				Geometry = new RectangleGeometry { Rect = new Rect(20, 20, 50, 50) }
			});

			var mockContext = new MockDrawingContext();
			group.Draw(mockContext);

			CornerstoneTest.AreEqual(expectedEffectBounds, mockContext.OpacityMaskBounds);
		}
	}

	[PresentationTestMethod]
	public void DrawingWithEffectShouldUseStoredBounds()
	{
		using (UnitTestApplication.Start(new TestServices(renderInterface: new HeadlessPlatformRenderInterface())))
		{
			var effect = new BlurEffect { Radius = 10 };
			var bounds = new Rect(10, 10, 100, 100);
			var group = new DrawingGroup
			{
				Effect = effect,
				EffectBounds = bounds
			};
			group.Children.Add(new GeometryDrawing { Brush = Brushes.Red, Geometry = new RectangleGeometry { Rect = new Rect(20, 20, 50, 50) } });

			var mockContext = new MockDrawingContext();
			group.Draw(mockContext);

			CornerstoneTest.AreEqual(effect, mockContext.Effect);
			CornerstoneTest.AreEqual(bounds, mockContext.Bounds);
		}
	}

	[PresentationTestMethod]
	public void InvalidatedIsRaisedWhenChildChanges()
	{
		var child = new GeometryDrawing();
		var group = new DrawingGroup();
		group.Children.Add(child);

		var count = 0;
		group.Invalidated += (_, _) => count++;

		child.Brush = Brushes.Red;
		CornerstoneTest.IsTrue(count > 0);
	}

	[PresentationTestMethod]
	public void InvalidatedIsRaisedWhenChildIsAddedOrRemoved()
	{
		var group = new DrawingGroup();
		var child = new GeometryDrawing();
		var count = 0;
		group.Invalidated += (_, _) => count++;

		group.Children.Add(child);
		CornerstoneTest.AreEqual(1, count);

		group.Children.Remove(child);
		CornerstoneTest.AreEqual(2, count);
	}

	/// <summary>
	/// Regression test: PlatformDrawingContext.PushEffectCore was passing the raw content bounds to
	/// the platform effect API without inflating by the effect output padding, causing effects to be clipped.
	/// </summary>
	[PresentationTestMethod]
	public void PlatformDrawingContextPushEffectShouldInflateBoundsForPlatformApi()
	{
		var effectImpl = new StubDrawingContextImpl();
		effectImpl.Transform = Matrix.Identity;
		using var context = new PlatformDrawingContext(effectImpl, false);

		var effect = new BlurEffect { Radius = 10 };
		var contentBounds = new Rect(10, 10, 100, 100);
		var expectedInflatedBounds = contentBounds.Inflate(effect.GetEffectOutputPadding());

		using (context.PushEffect(effect, contentBounds))
		{
		}

		// Verify the platform API received the inflated (output) bounds, not the raw content bounds
		effectImpl.Calls.VerifyCalled("PushEffect", 1);
		effectImpl.Calls.VerifyLastPrefix("PushEffect", expectedInflatedBounds);
	}

	[PresentationTestMethod]
	public void PushEffectShouldStoreProvidedBounds()
	{
		using (UnitTestApplication.Start(new TestServices(renderInterface: new HeadlessPlatformRenderInterface())))
		{
			var group = new DrawingGroup();
			var effect = new BlurEffect { Radius = 10 };
			var bounds = new Rect(10, 10, 100, 100);

			using (var context = group.Open())
			{
				using (context.PushEffect(effect, bounds))
				{
					context.DrawRectangle(Brushes.Red, null, new Rect(20, 20, 50, 50));
				}
			}

			// The Open() call adds a child DrawingGroup to the root group when PushEffect is called.
			CornerstoneTest.Single(group.Children);
			var childGroup = CornerstoneTest.IsType<DrawingGroup>(group.Children[0]);
			CornerstoneTest.AreEqual(effect, childGroup.Effect);
			CornerstoneTest.AreEqual(bounds, childGroup.EffectBounds);
		}
	}

	[PresentationTestMethod]
	public void RemovedChildIsNoLongerTracked()
	{
		var child = new GeometryDrawing();
		var group = new DrawingGroup();
		group.Children.Add(child);
		group.Children.Remove(child);

		var count = 0;
		group.Invalidated += (_, _) => count++;

		child.Brush = Brushes.Red;
		CornerstoneTest.AreEqual(0, count);
	}

	#endregion

	#region Classes

	private class MockDrawingContext : DrawingContext
	{
		#region Properties

		public Rect Bounds { get; private set; }
		public IEffect Effect { get; private set; }
		public Rect OpacityMaskBounds { get; private set; }

		#endregion

		#region Methods

		public override void Custom(ICustomDrawOperation custom)
		{
		}

		public override void DrawGlyphRun(IBrush foreground, GlyphRun glyphRun)
		{
		}

		protected override void DisposeCore()
		{
		}

		// Implementing required abstract members
		protected override void DrawEllipseCore(IBrush brush, IPen pen, Rect rect)
		{
		}

		protected override void DrawGeometryCore(IBrush brush, IPen pen, IGeometryImpl geometry)
		{
		}

		protected override void DrawLineCore(IPen pen, Point p1, Point p2)
		{
		}

		protected override void DrawRectangleCore(IBrush brush, IPen pen, RoundedRect rrect, BoxShadows boxShadows = default)
		{
		}

		protected override void PopClipCore()
		{
		}

		protected override void PopEffectCore()
		{
		}

		protected override void PopGeometryClipCore()
		{
		}

		protected override void PopOpacityCore()
		{
		}

		protected override void PopOpacityMaskCore()
		{
		}

		protected override void PopRenderOptionsCore()
		{
		}

		protected override void PopTextOptionsCore()
		{
		}

		protected override void PopTransformCore()
		{
		}

		protected override void PushClipCore(Rect rect)
		{
		}

		protected override void PushClipCore(RoundedRect rect)
		{
		}

		protected override void PushEffectCore(IEffect effect, Rect bounds)
		{
			Effect = effect;
			Bounds = bounds;
		}

		protected override void PushGeometryClipCore(Geometry clip)
		{
		}

		protected override void PushOpacityCore(double opacity)
		{
		}

		protected override void PushOpacityMaskCore(IBrush mask, Rect bounds)
		{
			OpacityMaskBounds = bounds;
		}

		protected override void PushRenderOptionsCore(RenderOptions renderOptions)
		{
		}

		protected override void PushTextOptionsCore(TextOptions textOptions)
		{
		}

		protected override void PushTransformCore(Matrix matrix)
		{
		}

		internal override void DrawBitmap(IRef<IBitmapImpl> source, double opacity, Rect sourceRect, Rect destRect)
		{
		}

		#endregion
	}

	#endregion
}