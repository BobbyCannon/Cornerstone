#region References

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.NativeHosts;

#endregion

namespace Cornerstone.UnitTests.Presentation;

[TestClass]
public class NativeAirspaceHoleTests : CornerstoneCornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void AncestorChromeOutsideHostHitsCornerstone()
	{
		var previous = NativeAirspace.BehindComposition;
		NativeAirspace.BehindComposition = true;
		try
		{
			var result = RunOnUi(() =>
			{
				var chrome = new Border
				{
					Width = 400,
					Height = 40,
					Background = Brushes.Blue
				};
				var host = new NativeControlHost { Width = 200, Height = 200 };
				Canvas.SetTop(host, 40);
				var canvas = new Canvas();
				canvas.Children.Add(host);
				canvas.Children.Add(chrome);
				var window = new Window
				{
					Width = 400,
					Height = 300,
					Background = Brushes.White,
					Content = canvas
				};
				window.Show();
				window.UpdateLayout();
				RunUiJobs();
				host.TryUpdateNativeControlPosition();
				using (window.CaptureRenderedFrame())
				{
				}
				var content = (IInputElement) window.Content;
				var chromeHit = content.InputHitTest(new Point(20, 10), false);
				var hostHit = content.InputHitTest(new Point(20, 80), false);
				window.Close();
				return (chromeHit is Border, hostHit is NativeControlHost);
			});

			Assert.IsTrue(result.Item1);
			Assert.IsTrue(result.Item2);
		}
		finally
		{
			NativeAirspace.BehindComposition = previous;
		}
	}

	[TestMethod]
	public void AncestorClipGeometryShrinksHostAbsoluteBounds()
	{
		var clipped = RunOnUi(() =>
		{
			var window = new Window { Width = 400, Height = 300 };
			var clip = new Border
			{
				Width = 100,
				Height = 80,
				Clip = new RectangleGeometry(new Rect(0, 0, 50, 80)),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top
			};
			var host = new NativeControlHost { Width = 100, Height = 80 };
			clip.Child = host;
			window.Content = clip;
			window.Show();
			clip.Measure(new Size(400, 300));
			clip.Arrange(new Rect(0, 0, 100, 80));
			host.Measure(new Size(100, 80));
			host.Arrange(new Rect(0, 0, 100, 80));
			window.UpdateLayout();
			RunUiJobs();
			host.TryUpdateNativeControlPosition();
			var bounds = host.GetAbsoluteBounds();
			window.Close();
			return bounds;
		});

		Assert.IsTrue(clipped.HasValue);
		Assert.AreEqual(50, clipped.Value.Width, 0.5);
		Assert.AreEqual(80, clipped.Value.Height, 0.5);
	}

	[TestMethod]
	public void ClipToBoundsShrinksHostAbsoluteBounds()
	{
		var clipped = RunOnUi(() =>
		{
			var window = new Window { Width = 400, Height = 300 };
			var clip = new Border
			{
				Width = 100,
				Height = 80,
				ClipToBounds = true,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top
			};
			var host = new NativeControlHost { Width = 100, Height = 200 };
			clip.Child = host;
			window.Content = clip;
			window.Show();
			clip.Measure(new Size(400, 300));
			clip.Arrange(new Rect(0, 0, 100, 80));
			host.Measure(new Size(100, 200));
			host.Arrange(new Rect(0, 0, 100, 200));
			window.UpdateLayout();
			RunUiJobs();
			host.TryUpdateNativeControlPosition();
			var bounds = host.GetAbsoluteBounds();
			var holes = window.Renderer.CompositionTarget.NativeAirspaceHoles.Count;
			window.Close();
			return (bounds, holes);
		});

		Assert.IsTrue(clipped.bounds.HasValue);
		Assert.AreEqual(80, clipped.bounds.Value.Height, 0.5);
		Assert.AreEqual(100, clipped.bounds.Value.Width, 0.5);
		Assert.AreEqual(1, clipped.holes);
	}

	[TestMethod]
	public void CornerRadiusForHostFillsParentKeepsRadius()
	{
		var rect = new Rect(0, 0, 100, 100);
		var radii = NativeAirspaceClip.CornerRadiusForHost(rect, rect, new CornerRadius(8));
		Assert.AreEqual(8, radii.TopLeft);
	}

	[TestMethod]
	public void CornerRadiusForHostShrinksByInset()
	{
		var ancestor = new Rect(0, 0, 100, 100);
		var host = new Rect(10, 10, 80, 80);
		var radii = NativeAirspaceClip.CornerRadiusForHost(host, ancestor, new CornerRadius(20));
		Assert.AreEqual(10, radii.TopLeft);
		Assert.AreEqual(10, radii.TopRight);
		Assert.AreEqual(10, radii.BottomRight);
		Assert.AreEqual(10, radii.BottomLeft);
	}

	[TestMethod]
	public void DetachedHostRemovesHole()
	{
		var previous = NativeAirspace.BehindComposition;
		NativeAirspace.BehindComposition = true;
		try
		{
			var result = RunOnUi(() =>
			{
				var window = ShowHostWindow(out _, null);
				var owner = window.PlatformImpl;
				var attachedWhileShown = (owner != null) && NativeAirspace.HasAttachedHost(owner);
				var holesWhileShown = window.Renderer.CompositionTarget.NativeAirspaceHoles.Count;
				window.Content = null;
				window.UpdateLayout();
				RunUiJobs();
				var holesAfter = window.Renderer.CompositionTarget.NativeAirspaceHoles.Count;
				var attachedAfter = (owner != null) && NativeAirspace.HasAttachedHost(owner);
				window.Close();
				return (attachedWhileShown, holesWhileShown, holesAfter, attachedAfter);
			});

			Assert.IsTrue(result.attachedWhileShown);
			Assert.AreEqual(1, result.holesWhileShown);
			Assert.AreEqual(0, result.holesAfter);
			Assert.IsFalse(result.attachedAfter);
		}
		finally
		{
			NativeAirspace.BehindComposition = previous;
		}
	}

	[TestMethod]
	public void IntersectKeepsOverlap()
	{
		var host = new Rect(0, 40, 100, 80);
		var viewport = new Rect(0, 0, 100, 80);
		var clipped = NativeAirspaceClip.Intersect(host, viewport);
		Assert.AreEqual(new Rect(0, 40, 100, 40), clipped);
	}

	[TestMethod]
	public void IntersectReturnsEmptyWhenRectsDoNotOverlap()
	{
		var host = new Rect(0, 200, 100, 80);
		var viewport = new Rect(0, 0, 100, 80);
		var clipped = NativeAirspaceClip.Intersect(host, viewport);
		Assert.IsTrue(clipped.IsEmpty());
	}

	[TestMethod]
	public void InvisibleHostIsEmptyHole()
	{
		var result = RunOnUi(() =>
		{
			var window = ShowHostWindow(out var host, null);
			host.IsVisible = false;
			window.UpdateLayout();
			RunUiJobs();
			host.TryUpdateNativeControlPosition();
			var holes = window.Renderer.CompositionTarget.NativeAirspaceHoles.Count;
			window.Close();
			return holes;
		});

		Assert.AreEqual(0, result);
	}

	[TestMethod]
	public void NativeBehindCompositionDefaultsOn()
	{
		Assert.IsTrue(new Win32PlatformOptions().NativeBehindComposition);
		Assert.IsTrue(new MacOSPlatformOptions().NativeBehindComposition);
		Assert.IsTrue(new WaylandPlatformOptions().NativeBehindComposition);
	}

	[TestMethod]
	public void NestedRoundedClipToBoundsUsesMaxInsetRadii()
	{
		var radii = RunOnUi(() =>
		{
			var window = new Window { Width = 400, Height = 300 };
			var outer = new Border
			{
				Width = 100,
				Height = 100,
				CornerRadius = new CornerRadius(20),
				ClipToBounds = true,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top
			};
			var inner = new Border
			{
				Width = 80,
				Height = 80,
				Margin = new Thickness(10),
				CornerRadius = new CornerRadius(8),
				ClipToBounds = true
			};
			var host = new NativeControlHost { Width = 80, Height = 80 };
			inner.Child = host;
			outer.Child = inner;
			window.Content = outer;
			window.Show();
			outer.Measure(new Size(400, 300));
			outer.Arrange(new Rect(0, 0, 100, 100));
			inner.Measure(new Size(80, 80));
			inner.Arrange(new Rect(10, 10, 80, 80));
			host.Measure(new Size(80, 80));
			host.Arrange(new Rect(0, 0, 80, 80));
			window.UpdateLayout();
			RunUiJobs();
			var result = NativeAirspaceClip.GetHoleCornerRadius(host, window);
			window.Close();
			return result;
		});

		Assert.AreEqual(10, radii.TopLeft);
		Assert.AreEqual(10, radii.TopRight);
		Assert.AreEqual(10, radii.BottomRight);
		Assert.AreEqual(10, radii.BottomLeft);
	}

	[TestMethod]
	public void OverlayChromeOverHostHitsChromeNotHost()
	{
		var previous = NativeAirspace.BehindComposition;
		NativeAirspace.BehindComposition = true;
		try
		{
			var result = RunOnUi(() =>
			{
				var overlay = new Border
				{
					Width = 80,
					Height = 80,
					Background = Brushes.Red
				};
				Canvas.SetLeft(overlay, 60);
				Canvas.SetTop(overlay, 60);
				var window = ShowHostWindow(out var host, overlay);
				var content = (IInputElement) window.Content;
				var overlayHit = content.InputHitTest(new Point(100, 100), false);
				var holeHit = content.InputHitTest(new Point(10, 10), false);
				window.Close();
				return (overlayHit, holeHit, overlay, host);
			});

			Assert.AreSame(result.overlay, result.overlayHit);
			Assert.IsFalse(NativeAirspace.IsHoleHit(result.overlayHit));
			Assert.AreSame(result.host, result.holeHit);
		}
		finally
		{
			NativeAirspace.BehindComposition = previous;
		}
	}

	[TestMethod]
	public void PortalClipDoesNotHideLaterSiblings()
	{
		var result = RunOnUi(() =>
		{
			var window = new Window
			{
				Width = 400,
				Height = 300,
				Background = Brushes.White
			};
			var canvas = new Canvas();
			var host = new NativeControlHost { Width = 200, Height = 200 };
			Canvas.SetLeft(host, 0);
			Canvas.SetTop(host, 0);
			var overlay = new Border
			{
				Width = 80,
				Height = 80,
				Background = Brushes.Red
			};
			Canvas.SetLeft(overlay, 60);
			Canvas.SetTop(overlay, 60);
			canvas.Children.Add(host);
			canvas.Children.Add(overlay);
			window.Content = canvas;
			window.Show();
			window.UpdateLayout();
			RunUiJobs();
			host.TryUpdateNativeControlPosition();
			using var frame = window.CaptureRenderedFrame();
			Assert.IsNotNull(frame);
			Assert.AreEqual(1, window.Renderer.CompositionTarget.NativeAirspaceHoles.Count);
			var overlayPixel = ReadRgbaPixel(frame, 100, 100);
			var holePixel = ReadRgbaPixel(frame, 10, 10);
			window.Close();
			return (overlayPixel, holePixel);
		});

		Assert.IsTrue((result.overlayPixel.A > 200) && (result.overlayPixel.R > 200)
			&& (result.overlayPixel.G < 40) && (result.overlayPixel.B < 40),
			$"Later sibling must still paint in the hole, got {result.overlayPixel}.");
		Assert.IsTrue(result.holePixel.A < 40,
			$"Uncovered hole must stay clear, got {result.holePixel}.");
	}

	[TestMethod]
	[DataRow(1.0)]
	[DataRow(1.5)]
	public void EarlierSiblingDoesNotFillNativeHole(double scale)
	{
		var previous = NativeAirspace.BehindComposition;
		NativeAirspace.BehindComposition = true;
		try
		{
			var result = RunOnUi(() =>
			{
				var earlier = new Border
				{
					Width = 150,
					Height = 50,
					Opacity = 0.4,
					Background = Brushes.Blue
				};
				Canvas.SetLeft(earlier, 10);
				Canvas.SetTop(earlier, 70);
				var host = new NativeControlHost { Width = 180, Height = 140 };
				Canvas.SetLeft(host, 50);
				Canvas.SetTop(host, 40);
				var later = new Border
				{
					Width = 40,
					Height = 30,
					Background = Brushes.Red
				};
				Canvas.SetLeft(later, 100);
				Canvas.SetTop(later, 80);
				var canvas = new Canvas();
				canvas.Children.Add(earlier);
				canvas.Children.Add(host);
				canvas.Children.Add(later);
				var window = new Window
				{
					Width = 400,
					Height = 300,
					Background = Brushes.White,
					Content = canvas
				};
				window.Show();
				window.UpdateLayout();
				RunUiJobs();
				if (scale != 1.0)
					window.SetRenderScaling(scale);
				window.UpdateLayout();
				RunUiJobs();
				host.TryUpdateNativeControlPosition();
				using var frame = window.CaptureRenderedFrame();
				Assert.IsNotNull(frame);
				var scaling = window.RenderScaling;
				var overlap = ReadRgbaPixel(frame, DipToPixel(60, scaling), DipToPixel(90, scaling));
				var outside = ReadRgbaPixel(frame, DipToPixel(20, scaling), DipToPixel(90, scaling));
				var overlay = ReadRgbaPixel(frame, DipToPixel(110, scaling), DipToPixel(90, scaling));
				var content = (IInputElement) window.Content;
				var overlapHit = content.InputHitTest(new Point(60, 90), false);
				window.Close();
				return (overlap, outside, overlay, overlapHit);
			});

			Assert.IsTrue(result.overlap.A < 40,
				$"Earlier sibling must stay out of the hole at scale {scale}, got {result.overlap}.");
			Assert.IsTrue(result.outside.A > 200 && result.outside.B > result.outside.R && result.outside.B > 200,
				$"Earlier sibling must still paint outside the hole at scale {scale}, got {result.outside}.");
			Assert.IsTrue(result.overlay.A > 200 && result.overlay.R > 200
				&& result.overlay.G < 40 && result.overlay.B < 40,
				$"Later sibling must still paint in the hole at scale {scale}, got {result.overlay}.");
			Assert.IsInstanceOfType(result.overlapHit, typeof(NativeControlHost),
				$"Point in the hole under the earlier sibling must hit the host at scale {scale}, got {result.overlapHit}.");
		}
		finally
		{
			NativeAirspace.BehindComposition = previous;
		}
	}

	[TestMethod]
	public void PortalWalkClipsVisualsBelowHost()
	{
		Assert.IsTrue(NativeAirspaceClip.ClipVisualOutOfHole(0, 5));
		Assert.IsFalse(NativeAirspaceClip.ClipVisualOutOfHole(5, 5));
		Assert.IsFalse(NativeAirspaceClip.ClipVisualOutOfHole(6, 5));
	}

	[TestMethod]
	public void ScrolledOutHostIsEmptyHole()
	{
		var result = RunOnUi(() =>
		{
			var window = new Window { Width = 400, Height = 300 };
			var clip = new Border
			{
				Width = 100,
				Height = 80,
				ClipToBounds = true,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top
			};
			var host = new NativeControlHost { Width = 100, Height = 80, Margin = new Thickness(0, 200, 0, 0) };
			clip.Child = host;
			window.Content = clip;
			window.Show();
			clip.Measure(new Size(400, 300));
			clip.Arrange(new Rect(0, 0, 100, 80));
			host.Measure(new Size(100, 280));
			host.Arrange(new Rect(0, 200, 100, 80));
			window.UpdateLayout();
			RunUiJobs();
			host.TryUpdateNativeControlPosition();
			var bounds = host.GetAbsoluteBounds();
			var holes = window.Renderer.CompositionTarget.NativeAirspaceHoles.Count;
			window.Close();
			return (bounds, holes);
		});

		Assert.IsTrue(result.bounds.HasValue);
		Assert.IsTrue(result.bounds.Value.IsEmpty());
		Assert.AreEqual(0, result.holes);
	}

	[TestMethod]
	public void SubtractHolesLeavesRemainderAroundIsland()
	{
		var window = new LtrbRect(0, 0, 100, 100);
		var remainders = NativeAirspaceClip.SubtractHoles(window, new[] { new LtrbRect(25, 25, 75, 75) });
		Assert.AreEqual(4, remainders.Length);
		var area = 0d;
		for (var i = 0; i < remainders.Length; i++)
		{
			area += remainders[i].Width * remainders[i].Height;
		}
		Assert.AreEqual(7500, area);
	}

	[TestMethod]
	public void SubtractHolesWithTwoOverlappingIslands()
	{
		var window = new LtrbRect(0, 0, 100, 100);
		var remainders = NativeAirspaceClip.SubtractHoles(window, new[]
		{
			new LtrbRect(0, 0, 60, 60),
			new LtrbRect(40, 40, 100, 100)
		});
		var area = 0d;
		for (var i = 0; i < remainders.Length; i++)
		{
			area += remainders[i].Width * remainders[i].Height;
		}
		Assert.AreEqual(3200, area);
	}

	[TestMethod]
	public void TwoHostsPublishTwoHoles()
	{
		var result = RunOnUi(() =>
		{
			var window = new Window { Width = 400, Height = 300, Background = Brushes.White };
			var canvas = new Canvas();
			var first = new NativeControlHost { Width = 80, Height = 80 };
			var second = new NativeControlHost { Width = 80, Height = 80 };
			Canvas.SetLeft(second, 40);
			Canvas.SetTop(second, 40);
			canvas.Children.Add(first);
			canvas.Children.Add(second);
			window.Content = canvas;
			window.Show();
			window.UpdateLayout();
			RunUiJobs();
			first.TryUpdateNativeControlPosition();
			second.TryUpdateNativeControlPosition();
			var holes = window.Renderer.CompositionTarget.NativeAirspaceHoles.Count;
			window.Close();
			return holes;
		});

		Assert.AreEqual(2, result);
	}

	[TestMethod]
	public void UncoveredHoleHitTestReturnsNativeHost()
	{
		var previous = NativeAirspace.BehindComposition;
		NativeAirspace.BehindComposition = true;
		try
		{
			var result = RunOnUi(() =>
			{
				var window = ShowHostWindow(out var host, null);
				var content = (IInputElement) window.Content;
				var hit = content.InputHitTest(new Point(10, 10), false);
				var holeHit = NativeAirspace.IsHoleHit(hit);
				window.Close();
				return (hit is NativeControlHost, holeHit, ReferenceEquals(hit, host));
			});

			Assert.IsTrue(result.Item1);
			Assert.IsTrue(result.holeHit);
			Assert.IsTrue(result.Item3);
		}
		finally
		{
			NativeAirspace.BehindComposition = previous;
		}
	}

	[TestMethod]
	public void TransparentOverlayOverHostHitsOverlayAndStaysClear()
	{
		var previous = NativeAirspace.BehindComposition;
		NativeAirspace.BehindComposition = true;
		try
		{
			var result = RunOnUi(() =>
			{
				var overlay = new Border
				{
					Width = 80,
					Height = 80,
					Background = Brushes.Transparent
				};
				Canvas.SetLeft(overlay, 60);
				Canvas.SetTop(overlay, 60);
				var window = ShowHostWindow(out var host, overlay);
				using var frame = window.CaptureRenderedFrame();
				var content = (IInputElement) window.Content;
				var overlayHit = content.InputHitTest(new Point(100, 100));
				var holeHit = content.InputHitTest(new Point(10, 10));
				var pixel = ReadRgbaPixel(frame, 100, 100);
				var interactive = NativeAirspace.IsInteractiveOverlay(overlayHit as Visual, window);
				window.Close();
				return (overlayHit, holeHit, pixel, interactive, overlay, host);
			});

			Assert.AreSame(result.overlay, result.overlayHit);
			Assert.IsTrue(result.interactive);
			Assert.IsFalse(NativeAirspace.IsHoleHit(result.overlayHit));
			Assert.AreSame(result.host, result.holeHit);
			Assert.IsTrue(result.pixel.A < 40, $"Transparent overlay must stay clear, got {result.pixel}.");
		}
		finally
		{
			NativeAirspace.BehindComposition = previous;
		}
	}

	[TestMethod]
	public void TransparentOverlayCoverageIsInteractiveOnlyOverTheOverlay()
	{
		var previous = NativeAirspace.BehindComposition;
		NativeAirspace.BehindComposition = true;
		try
		{
			var result = RunOnUi(() =>
			{
				var overlay = new Border
				{
					Width = 80,
					Height = 80,
					Background = Brushes.Transparent
				};
				Canvas.SetLeft(overlay, 60);
				Canvas.SetTop(overlay, 60);
				var window = ShowHostWindow(out var host, overlay);
				var holes = window.Renderer.CompositionTarget.NativeAirspaceHoles;
				if (holes.Count != 1)
				{
					window.Close();
					return (holeCount: holes.Count, overlayCovered: false, holeCovered: false, cellCount: 0);
				}

				var hole = holes[0];
				var splitters = new List<LtrbRect>();
				NativeAirspaceHitAlpha.CollectSplitters(window, hole.Bounds, hole.Scaling, splitters);
				var cells = new List<NativeAirspaceHitAlpha.Cell>();
				NativeAirspaceHitAlpha.BuildInteractiveCells(hole.PhysicalBounds, splitters, cells, (px, py) =>
				{
					var hit = window.InputHitTest(new Point(px / hole.Scaling, py / hole.Scaling));
					return NativeAirspace.IsInteractiveOverlay(hit as Visual, window);
				});
				var overOverlay = overlay.TranslatePoint(new Point(20, 20), window).Value;
				var overHole = host.TranslatePoint(new Point(10, 10), window).Value;
				var overlayCovered = CellContains(cells, overOverlay.X * hole.Scaling, overOverlay.Y * hole.Scaling);
				var holeCovered = CellContains(cells, overHole.X * hole.Scaling, overHole.Y * hole.Scaling);
				window.Close();
				return (holeCount: holes.Count, overlayCovered, holeCovered, cellCount: cells.Count);
			});

			Assert.AreEqual(1, result.holeCount);
			Assert.IsTrue(result.overlayCovered, "Transparent overlay must keep input over the native hole.");
			Assert.IsFalse(result.holeCovered, "Uncovered hole pixels must stay with native.");
			Assert.IsTrue(result.cellCount > 0);
		}
		finally
		{
			NativeAirspace.BehindComposition = previous;
		}
	}

	[TestMethod]
	public void OpacityZeroOverlayOverHostHitsOverlay()
	{
		var previous = NativeAirspace.BehindComposition;
		NativeAirspace.BehindComposition = true;
		try
		{
			var result = RunOnUi(() =>
			{
				var overlay = new Border
				{
					Width = 80,
					Height = 80,
					Background = Brushes.Red,
					Opacity = 0
				};
				Canvas.SetLeft(overlay, 60);
				Canvas.SetTop(overlay, 60);
				var window = ShowHostWindow(out _, overlay);
				var content = (IInputElement) window.Content;
				var overlayHit = content.InputHitTest(new Point(100, 100));
				var interactive = NativeAirspace.IsInteractiveOverlay(overlayHit as Visual, window);
				window.Close();
				return (overlayHit, overlay, interactive);
			});

			Assert.AreSame(result.overlay, result.overlayHit);
			Assert.IsTrue(result.interactive);
		}
		finally
		{
			NativeAirspace.BehindComposition = previous;
		}
	}

	[TestMethod]
	public void StampRaisesClearInteractivePixels()
	{
		var pixels = new byte[8 * 32];
		pixels[(1 * 32) + (2 * 4) + 3] = 200;
		var cells = new List<NativeAirspaceHitAlpha.Cell>();
		NativeAirspaceHitAlpha.BuildInteractiveCells(
			new LtrbRect(0, 0, 8, 8),
			new List<LtrbRect>(),
			cells,
			(_, _) => true);
		NativeAirspaceHitAlpha.Apply(pixels, 8, 8, 32, null, cells);

		Assert.AreEqual(1, cells.Count);
		Assert.AreEqual(NativeAirspaceHitAlpha.MinimumInteractiveAlpha, pixels[3]);
		Assert.AreEqual(200, pixels[(1 * 32) + (2 * 4) + 3]);
	}

	[TestMethod]
	public void StampFollowsPartialCoverageInsideHole()
	{
		var pixels = new byte[32 * 128];
		var cells = new List<NativeAirspaceHitAlpha.Cell>();
		var splitters = new List<LtrbRect> { new LtrbRect(10, 12, 14, 16) };
		NativeAirspaceHitAlpha.BuildInteractiveCells(
			new LtrbRect(0, 0, 32, 32),
			splitters,
			cells,
			(x, y) => x >= 10 && x < 14 && y >= 12 && y < 16);
		NativeAirspaceHitAlpha.Apply(pixels, 32, 32, 128, null, cells);

		Assert.AreEqual(NativeAirspaceHitAlpha.MinimumInteractiveAlpha, pixels[(13 * 128) + (11 * 4) + 3]);
		Assert.AreEqual(0, pixels[(13 * 128) + (9 * 4) + 3]);
		Assert.AreEqual(0, pixels[(20 * 128) + (20 * 4) + 3]);
	}

	[TestMethod]
	public void StampRevertsPreviousCoverage()
	{
		var pixels = new byte[4 * 16];
		var covered = new List<NativeAirspaceHitAlpha.Cell>
		{
			new NativeAirspaceHitAlpha.Cell(0, 0, 4, 4)
		};
		NativeAirspaceHitAlpha.Apply(pixels, 4, 4, 16, null, covered);
		pixels[0] = 1;
		pixels[1] = 2;
		pixels[2] = 3;
		pixels[3] = 1;
		NativeAirspaceHitAlpha.Apply(pixels, 4, 4, 16, covered, new List<NativeAirspaceHitAlpha.Cell>());

		Assert.AreEqual(1, pixels[0]);
		Assert.AreEqual(2, pixels[1]);
		Assert.AreEqual(3, pixels[2]);
		Assert.AreEqual(1, pixels[3]);
		Assert.AreEqual(0, pixels[(1 * 16) + 3]);
	}

	private static bool CellContains(List<NativeAirspaceHitAlpha.Cell> cells, double x, double y)
	{
		var px = (int) x;
		var py = (int) y;
		for (var i = 0; i < cells.Count; i++)
		{
			var cell = cells[i];
			if (px >= cell.Left && px < cell.Right && py >= cell.Top && py < cell.Bottom)
				return true;
		}

		return false;
	}

	private static int DipToPixel(int dip, double scaling) => (int) ((dip * scaling) + 0.5);

	private static Color ReadRgbaPixel(WriteableBitmap bitmap, int x, int y)
	{
		using var framebuffer = bitmap.Lock();
		var offset = (y * framebuffer.RowBytes) + (x * 4);
		var b0 = Marshal.ReadByte(framebuffer.Address, offset);
		var b1 = Marshal.ReadByte(framebuffer.Address, offset + 1);
		var b2 = Marshal.ReadByte(framebuffer.Address, offset + 2);
		var b3 = Marshal.ReadByte(framebuffer.Address, offset + 3);
		if (framebuffer.Format == PixelFormat.Bgra8888)
		{
			return Color.FromArgb(b3, b2, b1, b0);
		}

		return Color.FromArgb(b3, b0, b1, b2);
	}

	private static Window ShowHostWindow(out NativeControlHost host, Control overlay)
	{
		var window = new Window
		{
			Width = 400,
			Height = 300,
			Background = Brushes.White
		};
		var canvas = new Canvas();
		host = new NativeControlHost { Width = 200, Height = 200 };
		Canvas.SetLeft(host, 0);
		Canvas.SetTop(host, 0);
		canvas.Children.Add(host);
		if (overlay != null)
		{
			canvas.Children.Add(overlay);
		}
		window.Content = canvas;
		window.Show();
		window.UpdateLayout();
		RunUiJobs();
		host.TryUpdateNativeControlPosition();
		using (window.CaptureRenderedFrame())
		{
		}
		return window;
	}

	#endregion
}