#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Converters;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ScrollViewerTests : ScopedTestBase
{
	#region Fields

	private readonly MouseTestHelper _mouse = new();

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void BringIntoViewOnFocusChangeFalseDoesNotScrollChildControlIntoViewWhenFocused()
	{
		var content = new StackPanel
		{
			Children =
			{
				new Button
				{
					Width = 100,
					Height = 900
				},
				new Button
				{
					Width = 100,
					Height = 900
				}
			}
		};

		var target = new ScrollViewer
		{
			Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
			Content = content
		};
		var root = new TestRoot(target);

		root.LayoutManager.ExecuteInitialLayoutPass();

		var button = (Button) content.Children[1];
		button.Focus();

		CornerstoneTest.AreEqual(new Vector(0, 0), target.Offset);
	}

	[PresentationTestMethod]
	public void BringIntoViewOnFocusChangeScrollsChildControlIntoViewWhenFocused()
	{
		using var app = UnitTestApplication.Start(TestServices.RealFocus);
		var content = new StackPanel
		{
			Children =
			{
				new Button
				{
					Width = 100,
					Height = 900
				},
				new Button
				{
					Width = 100,
					Height = 900
				}
			}
		};

		var target = new ScrollViewer
		{
			Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
			Content = content
		};
		var root = new TestRoot(target);

		root.LayoutManager.ExecuteInitialLayoutPass();

		var button = (Button) content.Children[1];
		button.Focus();

		CornerstoneTest.AreEqual(new Vector(0, 800), target.Offset);
	}

	[PresentationTestMethod]
	public void ChangingExtentShouldRaiseScrollChanged()
	{
		var target = new ScrollViewer();
		var root = new TestRoot(target);
		var raised = 0;

		target.Extent = new Size(100, 100);
		target.Viewport = new Size(50, 50);
		target.Offset = new Vector(10, 10);

		root.LayoutManager.ExecuteInitialLayoutPass();

		target.ScrollChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(new Vector(11, 12), e.ExtentDelta);
			CornerstoneTest.AreEqual(default, e.OffsetDelta);
			CornerstoneTest.AreEqual(default, e.ViewportDelta);
			++raised;
		};

		target.Extent = new Size(111, 112);

		CornerstoneTest.AreEqual(0, raised);

		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void ChangingOffsetShouldRaiseScrollChanged()
	{
		var target = new ScrollViewer();
		var root = new TestRoot(target);
		var raised = 0;

		target.Extent = new Size(100, 100);
		target.Viewport = new Size(50, 50);
		target.Offset = new Vector(10, 10);

		root.LayoutManager.ExecuteInitialLayoutPass();

		target.ScrollChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(default, e.ExtentDelta);
			CornerstoneTest.AreEqual(new Vector(12, 14), e.OffsetDelta);
			CornerstoneTest.AreEqual(default, e.ViewportDelta);
			++raised;
		};

		target.Offset = new Vector(22, 24);

		CornerstoneTest.AreEqual(0, raised);

		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void ChangingViewportShouldRaiseScrollChanged()
	{
		var target = new ScrollViewer();
		var root = new TestRoot(target);
		var raised = 0;

		target.Extent = new Size(100, 100);
		target.Viewport = new Size(50, 50);
		target.Offset = new Vector(10, 10);

		root.LayoutManager.ExecuteInitialLayoutPass();

		target.ScrollChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(default, e.ExtentDelta);
			CornerstoneTest.AreEqual(default, e.OffsetDelta);
			CornerstoneTest.AreEqual(new Vector(6, 8), e.ViewportDelta);
			++raised;
		};

		target.Viewport = new Size(56, 58);

		CornerstoneTest.AreEqual(0, raised);

		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void ContentIsCreated()
	{
		var target = new ScrollViewer
		{
			Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
			Content = "Foo"
		};

		InitializeScrollViewer(target);

		CornerstoneTest.IsType<TextBlock>(target.Presenter!.Child);
	}

	[PresentationTestMethod]
	public void DeferredScrollingDefersScrollingUntilPointerUp()
	{
		var content = new TestContent();
		var target = new ScrollViewer
		{
			Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
			IsDeferredScrollingEnabled = true,
			Content = content
		};
		var root = new TestRoot(target);

		root.LayoutManager.ExecuteInitialLayoutPass();

		// We're working in absolute coordinates (i.e. relative to the root) and clicking on
		// the center of the vertical thumb.
		var thumb = GetVerticalThumb(target);
		var p = GetRootPoint(thumb, thumb.Bounds.Center);

		CornerstoneTest.AreEqual(Vector.Zero, target.Offset);
		CornerstoneTest.AreEqual(0, thumb.Bounds.Top);

		// Press the mouse button in the center of the thumb.
		_mouse.Down(thumb, position: p);
		root.LayoutManager.ExecuteLayoutPass();

		// Drag the thumb down 100 pixels.
		_mouse.Move(thumb, p += new Vector(0, 100));
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(Vector.Zero, target.Offset); // no change to scroll...
		CornerstoneTest.AreEqual(100, thumb.Bounds.Top); // ...but the Thumb has moved

		// Release the mouse
		_mouse.Up(thumb, position: p);

		CornerstoneTest.AreEqual(new Vector(0, 200), target.Offset);
		CornerstoneTest.AreEqual(100, thumb.Bounds.Top);
	}

	[PresentationTestMethod]
	public void FocusKeyInputShouldScroll()
	{
		var panel = new Panel
		{
			Width = 100_000,
			Height = 100_000
		};
		var target = new ScrollViewer
		{
			Content = panel,
			Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate)
		};
		var root = new TestRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();

		// Page down and page up
		KeyDown(target, Key.PageDown);
		CornerstoneTest.AreEqual(new(0, target.Viewport.Height), target.Offset);
		KeyDown(target, Key.PageUp);
		CornerstoneTest.AreEqual(new(0, 0), target.Offset);

		// Per-line scrolling in all directions with arrow keys
		KeyDown(target, Key.Down);
		CornerstoneTest.AreEqual(new(0, target.SmallChange.Height), target.Offset);
		KeyDown(target, Key.Right);
		CornerstoneTest.AreEqual(new(target.SmallChange.Width, target.SmallChange.Height), target.Offset);
		KeyDown(target, Key.Up);
		CornerstoneTest.AreEqual(new(ScrollViewer.DefaultSmallChange, 0), target.Offset);
		KeyDown(target, Key.Left);
		CornerstoneTest.AreEqual(new(0, 0), target.Offset);

		// Scrolling horizontally with a right-to-left flow direction
		target.FlowDirection = FlowDirection.RightToLeft;
		KeyDown(target, Key.Left);
		CornerstoneTest.AreEqual(new(target.SmallChange.Width, 0), target.Offset);
		KeyDown(target, Key.Right);
		CornerstoneTest.AreEqual(new(0, 0), target.Offset);
	}

	[PresentationTestMethod]
	public void LargeChangeShouldBeViewport()
	{
		var target = new ScrollViewer
		{
			Viewport = new Size(104, 143)
		};
		CornerstoneTest.AreEqual(new Size(104, 143), target.LargeChange);
	}

	[PresentationTestMethod]
	public void LargeChangeShouldComeFromILogicalScrollableIfPresent()
	{
		var child = new StubLogicalScrollable();
		child.IsLogicalScrollEnabled = true;
		child.PageScrollSize = new Size(45, 67);

		var target = new ScrollViewer
		{
			Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
			Content = child
		};

		InitializeScrollViewer(target);

		CornerstoneTest.AreEqual(new Size(45, 67), target.LargeChange);
	}

	[PresentationTestMethod]
	public void MenuScrollBarShouldBeVisibleWhenSpecifiedVisible()
	{
		var converter = MenuScrollingVisibilityConverter.Instance;
		var args = new List<object> { ScrollBarVisibility.Visible, 400d, 1800d, 500d };
		var result = converter.Convert(args, typeof(ScrollBarVisibility), "0", CultureInfo.CurrentCulture);
		CornerstoneTest.AreEqual(true, result);
	}

	[PresentationTestMethod]
	public void OffsetShouldBeCoercedToViewport()
	{
		var target = new ScrollViewer
		{
			Extent = new Size(20, 20),
			Viewport = new Size(10, 10),
			Offset = new Vector(12, 12)
		};

		CornerstoneTest.AreEqual(new Vector(10, 10), target.Offset);
	}

	[PresentationTestMethod]
	public void PaddingShouldBeIncludedInExtent()
	{
		const int itemCount = 19;
		const double itemHeight = 32;
		const double padding = 50;
		const double viewportHeight = 200;

		var content = new StackPanel();

		for (var i = 0; i < itemCount; ++i)
		{
			content.Children.Add(new Border { Height = itemHeight });
		}

		var target = new ScrollViewer
		{
			Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
			Padding = new Thickness(padding),
			Height = viewportHeight,
			Content = content
		};

		var root = new TestRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();

		CornerstoneTest.AreEqual(viewportHeight, target.Viewport.Height);
		CornerstoneTest.AreEqual((itemCount * itemHeight) + (2 * padding), target.Extent.Height);

		// The content is arranged below the top padding and isn't squashed by it.
		CornerstoneTest.AreEqual(padding, content.Bounds.Top);
		CornerstoneTest.AreEqual(itemCount * itemHeight, content.Bounds.Height);

		target.ScrollToEnd();
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(((itemCount * itemHeight) + (2 * padding)) - viewportHeight, target.Offset.Y);

		// Once scrolled to the end, the last item is fully visible and the bottom padding is still displayed below it.
		CornerstoneTest.AreEqual(viewportHeight - padding, content.Bounds.Bottom);
	}

	[PresentationTestMethod]
	public void PaddingShouldNotMakeContentScrollableWhenScrollViewerIsSizedToContent()
	{
		var target = new ScrollViewer
		{
			Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
			Padding = new Thickness(50),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Content = new Border { Width = 100, Height = 100 }
		};

		var root = new TestRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();

		// The padding is included in the desired size, so the content still fits exactly.
		CornerstoneTest.AreEqual(new Size(200, 200), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Size(200, 200), target.Viewport);
		CornerstoneTest.AreEqual(new Size(200, 200), target.Extent);
	}

	[PresentationTestMethod]
	public void ReducingExtentShouldConstrainOffset()
	{
		var target = new ScrollViewer
		{
			Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate)
		};
		var root = new TestRoot(target);
		var raised = 0;

		target.Extent = new(100, 100);
		target.Viewport = new(50, 50);
		target.Offset = new Vector(50, 50);

		root.LayoutManager.ExecuteInitialLayoutPass();

		target.ScrollChanged += (s, e) =>
		{
			CornerstoneTest.AreEqual(new Vector(-30, -30), e.ExtentDelta);
			CornerstoneTest.AreEqual(new Vector(-30, -30), e.OffsetDelta);
			CornerstoneTest.AreEqual(default, e.ViewportDelta);
			++raised;
		};

		target.Extent = new(70, 70);

		CornerstoneTest.AreEqual(0, raised);

		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(1, raised);
		CornerstoneTest.AreEqual(new Vector(20, 20), target.Offset);
	}

	[PresentationTestMethod]
	public void ScrollBarVisibilityShouldInvalidateMeasureAndArrange()
	{
		var panel = new TestPanel
		{
			DesiredWidth = 100_000
		};
		var target = new ScrollViewer
		{
			Content = panel,
			Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
		};
		var root = new TestRoot(target);
		root.LayoutManager.ExecuteInitialLayoutPass();
		panel.Reset();

		target.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
		root.LayoutManager.ExecuteLayoutPass();
		CornerstoneTest.AreEqual(1, panel.MeasureOverrideCalls);
		CornerstoneTest.AreEqual(1, panel.ArrangeOverrideCalls);
	}

	[PresentationTestMethod]
	public void ScrollDoesNotJumpWhenViewportBecomesSmallerWhileDraggingScrollBarThumb()
	{
		var content = new TestContent
		{
			MeasureSize = new Size(1000, 10000)
		};

		var target = new ScrollViewer
		{
			Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
			Content = content
		};
		var root = new TestRoot(target);

		root.LayoutManager.ExecuteInitialLayoutPass();

		CornerstoneTest.AreEqual(new Size(1000, 10000), target.Extent);
		CornerstoneTest.AreEqual(new Size(1000, 1000), target.Viewport);

		// We're working in absolute coordinates (i.e. relative to the root) and clicking on
		// the center of the vertical thumb.
		var thumb = GetVerticalThumb(target);
		var p = GetRootPoint(thumb, thumb.Bounds.Center);

		// Press the mouse button in the center of the thumb.
		_mouse.Down(thumb, position: p);
		root.LayoutManager.ExecuteLayoutPass();

		// Drag the thumb down 300 pixels.
		_mouse.Move(thumb, p += new Vector(0, 300));
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(new Vector(0, 3000), target.Offset);
		CornerstoneTest.AreEqual(300, thumb.Bounds.Top);

		// Now the extent changes from 10,000 to 5000.
		content.MeasureSize /= 2;
		content.InvalidateMeasure();
		root.LayoutManager.ExecuteLayoutPass();

		// Due to the extent change, the thumb moves down but the value remains the same.
		CornerstoneTest.AreEqual(600, thumb.Bounds.Top);
		CornerstoneTest.AreEqual(new Vector(0, 3000), target.Offset);

		// Drag the thumb down another 100 pixels.
		_mouse.Move(thumb, p += new Vector(0, 100));
		root.LayoutManager.ExecuteLayoutPass();

		// The drag should not cause the offset/thumb to jump *up* to the current absolute
		// mouse position, i.e. it should move down in the direction of the drag even if the
		// absolute mouse position is now above the thumb.
		CornerstoneTest.AreEqual(700, thumb.Bounds.Top);
		CornerstoneTest.AreEqual(new Vector(0, 3500), target.Offset);
	}

	[PresentationTestMethod]
	public void SettingOffsetToNaNDoesNotCauseInfiniteCoerceRecursion()
	{
		var target = new ScrollViewer
		{
			Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
			Content = "Foo",
			Extent = new Size(100, 100),
			Viewport = new Size(10, 10)
		};

		InitializeScrollViewer(target);

		target.Offset = new Vector(0, double.NaN);

		CornerstoneTest.IsFalse(double.IsNaN(target.Offset.X));
		CornerstoneTest.IsFalse(double.IsNaN(target.Offset.Y));
	}

	[PresentationTestMethod]
	public void SmallChangeShouldBe16()
	{
		var target = new ScrollViewer();

		CornerstoneTest.AreEqual(new Size(16, 16), target.SmallChange);
	}

	[PresentationTestMethod]
	public void SmallChangeShouldComeFromILogicalScrollableIfPresent()
	{
		var child = new StubLogicalScrollable();
		child.IsLogicalScrollEnabled = true;
		child.ScrollSize = new Size(12, 43);

		var target = new ScrollViewer
		{
			Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
			Content = child
		};

		InitializeScrollViewer(target);

		CornerstoneTest.AreEqual(new Size(12, 43), target.SmallChange);
	}

	[PresentationTestMethod]
	public void TestScrollToEnd()
	{
		var target = new ScrollViewer
		{
			Extent = new Size(50, 50),
			Viewport = new Size(10, 10),
			Offset = new Vector(25, 25)
		};
		target.ScrollToEnd();

		CornerstoneTest.AreEqual(new Vector(0, 40), target.Offset);
	}

	[PresentationTestMethod]
	public void TestScrollToHome()
	{
		var target = new ScrollViewer
		{
			Extent = new Size(50, 50),
			Viewport = new Size(10, 10),
			Offset = new Vector(25, 25)
		};
		target.ScrollToHome();

		CornerstoneTest.AreEqual(new Vector(0, 0), target.Offset);
	}

	[PresentationTestMethod]
	public void ThumbDoesNotBecomeDetachedFromMousePositionWhenScrollingPastTheStart()
	{
		var content = new TestContent();
		var target = new ScrollViewer
		{
			Template = new FuncControlTemplate<ScrollViewer>(CreateTemplate),
			Content = content
		};
		var root = new TestRoot(target);

		root.LayoutManager.ExecuteInitialLayoutPass();

		CornerstoneTest.AreEqual(new Size(1000, 2000), target.Extent);
		CornerstoneTest.AreEqual(new Size(1000, 1000), target.Viewport);

		// We're working in absolute coordinates (i.e. relative to the root) and clicking on
		// the center of the vertical thumb.
		var thumb = GetVerticalThumb(target);
		var p = GetRootPoint(thumb, thumb.Bounds.Center);

		// Press the mouse button in the center of the thumb.
		_mouse.Down(thumb, position: p);
		root.LayoutManager.ExecuteLayoutPass();

		// Drag the thumb down 100 pixels.
		_mouse.Move(thumb, p += new Vector(0, 100));
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(new Vector(0, 200), target.Offset);
		CornerstoneTest.AreEqual(100, thumb.Bounds.Top);

		// Drag the thumb up 200 pixels - 100 pixels past the top of the scrollbar.
		_mouse.Move(thumb, p -= new Vector(0, 200));
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(new Vector(0, 0), target.Offset);
		CornerstoneTest.AreEqual(0, thumb.Bounds.Top);

		// Drag the thumb back down 200 pixels.
		_mouse.Move(thumb, p += new Vector(0, 200));
		root.LayoutManager.ExecuteLayoutPass();

		// We should now be back in the state after we first scrolled down 100 pixels.
		CornerstoneTest.AreEqual(new Vector(0, 200), target.Offset);
		CornerstoneTest.AreEqual(100, thumb.Bounds.Top);
	}

	internal static Control CreateTemplate(ScrollViewer control, INameScope scope)
	{
		return new Grid
		{
			ColumnDefinitions = new ColumnDefinitions
			{
				new ColumnDefinition(1, GridUnitType.Star),
				new ColumnDefinition(GridLength.Auto)
			},
			RowDefinitions = new RowDefinitions
			{
				new RowDefinition(1, GridUnitType.Star),
				new RowDefinition(GridLength.Auto)
			},
			Children =
			{
				new ScrollContentPresenter
				{
					Name = "PART_ContentPresenter",
					[~ScrollContentPresenter.PaddingProperty] = control[~ScrollViewer.PaddingProperty]
				}.RegisterInNameScope(scope),
				new ScrollBar
				{
					Name = "PART_HorizontalScrollBar",
					Orientation = Orientation.Horizontal,
					Template = new FuncControlTemplate<ScrollBar>(CreateScrollBarTemplate),
					[~ScrollBar.VisibilityProperty] = control[~ScrollViewer.HorizontalScrollBarVisibilityProperty],
					[Grid.RowProperty] = 1
				}.RegisterInNameScope(scope),
				new ScrollBar
				{
					Name = "PART_VerticalScrollBar",
					Orientation = Orientation.Vertical,
					Template = new FuncControlTemplate<ScrollBar>(CreateScrollBarTemplate),
					[~ScrollBar.VisibilityProperty] = control[~ScrollViewer.VerticalScrollBarVisibilityProperty],
					[Grid.ColumnProperty] = 1
				}.RegisterInNameScope(scope)
			}
		};
	}

	private static Control CreateScrollBarTemplate(ScrollBar scrollBar, INameScope scope)
	{
		return new Border
		{
			Child = new Track
			{
				Name = "track",
				IsDirectionReversed = true,
				[!Track.MinimumProperty] = scrollBar[!RangeBase.MinimumProperty],
				[!Track.MaximumProperty] = scrollBar[!RangeBase.MaximumProperty],
				[!!Track.ValueProperty] = scrollBar[!!RangeBase.ValueProperty],
				[!Track.ViewportSizeProperty] = scrollBar[!ScrollBar.ViewportSizeProperty],
				[!Track.OrientationProperty] = scrollBar[!ScrollBar.OrientationProperty],
				[!Track.DeferThumbDragProperty] = scrollBar.TemplatedParent![!ScrollViewer.IsDeferredScrollingEnabledProperty],
				Thumb = new Thumb
				{
					Template = new FuncControlTemplate<Thumb>(CreateThumbTemplate)
				}
			}.RegisterInNameScope(scope)
		};
	}

	private static Control CreateThumbTemplate(Thumb control, INameScope scope)
	{
		return new Border
		{
			Background = Brushes.Gray
		};
	}

	private Point GetRootPoint(Visual control, Point p)
	{
		if (control.GetVisualRoot() is Visual root &&
			control.TransformToVisual(root) is Matrix m)
		{
			return p.Transform(m);
		}

		throw new InvalidOperationException("Could not get the point in root coordinates.");
	}

	private Thumb GetVerticalThumb(ScrollViewer target)
	{
		var scrollbar = CornerstoneTest.IsType<ScrollBar>(target.GetTemplateDescendants().FirstOrDefault(x => x.Name == "PART_VerticalScrollBar"));
		var track = CornerstoneTest.IsType<Track>(scrollbar.GetTemplateDescendants().FirstOrDefault(x => x.Name == "track"));
		return CornerstoneTest.IsType<Thumb>(track.Thumb);
	}

	private static void InitializeScrollViewer(ScrollViewer target)
	{
		target.ApplyTemplate();

		var presenter = (ScrollContentPresenter) target.Presenter!;
		presenter.AttachToScrollViewer();
		presenter.UpdateChild();
	}

	private static void KeyDown(IInputElement target, Key key)
	{
		target.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = key
		});
	}

	#endregion

	#region Classes

	public class TestPanel : Panel
	{
		#region Properties

		public int ArrangeOverrideCalls { get; private set; }
		public int DesiredWidth { get; set; }
		public int MeasureOverrideCalls { get; private set; }

		#endregion

		#region Methods

		public void Reset()
		{
			MeasureOverrideCalls = 0;
			ArrangeOverrideCalls = 0;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			ArrangeOverrideCalls++;
			return base.ArrangeOverride(finalSize);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			MeasureOverrideCalls++;
			return new Size(DesiredWidth, 1);
		}

		#endregion
	}

	private class TestContent : Control
	{
		#region Properties

		public Size MeasureSize { get; set; } = new(1000, 2000);

		#endregion

		#region Methods

		protected override Size MeasureOverride(Size availableSize)
		{
			return MeasureSize;
		}

		#endregion
	}

	#endregion
}