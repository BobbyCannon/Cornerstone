#region References

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Presenters;

[TestClass]
public class ScrollContentPresenterTests : NameScopeTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 10, 10, 80, 80)]
	[DataRow(HorizontalAlignment.Left, VerticalAlignment.Stretch, 10, 10, 16, 80)]
	[DataRow(HorizontalAlignment.Right, VerticalAlignment.Stretch, 74, 10, 16, 80)]
	[DataRow(HorizontalAlignment.Center, VerticalAlignment.Stretch, 42, 10, 16, 80)]
	[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Top, 10, 10, 80, 16)]
	[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Bottom, 10, 74, 80, 16)]
	[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Center, 10, 42, 80, 16)]
	public void AlignmentAndPaddingAreAppliedToChildBounds(
		HorizontalAlignment h,
		VerticalAlignment v,
		double expectedX,
		double expectedY,
		double expectedWidth,
		double expectedHeight)
	{
		Border content;
		var target = new ScrollContentPresenter
		{
			Padding = new Thickness(10),
			Content = content = new Border
			{
				MinWidth = 16,
				MinHeight = 16,
				HorizontalAlignment = h,
				VerticalAlignment = v
			}
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Rect(expectedX, expectedY, expectedWidth, expectedHeight), content.Bounds);
	}

	[PresentationTestMethod]
	public void ArrangeShouldConstrainChildWidthWhenCanHorizontallyScrollFalse()
	{
		var child = new WrapPanel
		{
			Children =
			{
				new Border { Width = 40, Height = 50 },
				new Border { Width = 40, Height = 50 },
				new Border { Width = 40, Height = 50 }
			}
		};

		var target = new ScrollContentPresenter
		{
			Content = child,
			CanHorizontallyScroll = false
		};

		target.UpdateChild();
		target.Measure(Size.Infinity);
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(100, child.Bounds.Width);
	}

	[PresentationTestMethod]
	public void ArrangeShouldSetViewportAndExtentInThatOrder()
	{
		var target = new ScrollContentPresenter
		{
			Content = new Border { Width = 40, Height = 50 }
		};

		var set = new List<string>();

		target.UpdateChild();
		target.Measure(new Size(100, 100));

		target.GetObservable(ScrollViewer.ViewportProperty).Skip(1).Subscribe(_ => set.Add("Viewport"));
		target.GetObservable(ScrollViewer.ExtentProperty).Skip(1).Subscribe(_ => set.Add("Extent"));

		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new[] { "Viewport", "Extent" }, set);
	}

	[PresentationTestMethod]
	public void BottomPaddingShouldBeVisibleWhenScrolledToEnd()
	{
		StackPanel content;
		var target = new ScrollContentPresenter
		{
			CanVerticallyScroll = true,
			Padding = new Thickness(10),
			Content = content = new StackPanel
			{
				Children =
				{
					new Border { Height = 50 },
					new Border { Height = 50 }
				}
			}
		};

		target.UpdateChild();
		target.Measure(new Size(50, 50));
		target.Arrange(new Rect(0, 0, 50, 50));

		// Scroll to the end: extent (120) - viewport (50).
		target.Offset = new Vector(0, 70);
		target.Arrange(new Rect(0, 0, 50, 50));

		CornerstoneTest.AreEqual(new Vector(0, 70), target.Offset);

		// The whole content is scrolled into view and the bottom padding is still visible.
		CornerstoneTest.AreEqual(-60, content.Bounds.Top);
		CornerstoneTest.AreEqual(40, content.Bounds.Bottom);
	}

	[PresentationTestMethod]
	public void BringDescendantIntoViewShouldBeIdempotentBeforeNextLayoutPass()
	{
		var panel = new StackPanel();

		for (var i = 0; i < 100; ++i)
		{
			panel.Children.Add(new Border { Height = 20 });
		}

		var target = new ScrollContentPresenter
		{
			Width = 50,
			Height = 100,
			CanVerticallyScroll = true,
			Content = panel
		};

		target.UpdateChild();
		target.Measure(Size.Infinity);
		target.Arrange(new Rect(0, 0, 50, 100));

		// The 50th child spans 1000..1020, so with a 100px viewport it is brought into view by scrolling to 920.
		var child = panel.Children[50];

		target.BringDescendantIntoView(child, new Rect(child.Bounds.Size));
		CornerstoneTest.AreEqual(920, target.Offset.Y);
		CornerstoneTest.IsFalse(target.IsArrangeValid);

		target.BringDescendantIntoView(child, new Rect(child.Bounds.Size));
		CornerstoneTest.AreEqual(920, target.Offset.Y);
	}

	[PresentationTestMethod]
	public void BringDescendantIntoViewShouldHandleChildMargin()
	{
		Border border;
		var target = new ScrollContentPresenter
		{
			CanHorizontallyScroll = true,
			CanVerticallyScroll = true,
			Width = 100,
			Height = 100,
			Content = new Decorator
			{
				Margin = new Thickness(50),
				Child = border = new Border
				{
					Width = 200,
					Height = 200
				}
			}
		};

		target.UpdateChild();
		target.Measure(Size.Infinity);
		target.Arrange(new Rect(0, 0, 100, 100));
		target.BringDescendantIntoView(border, new Rect(200, 200, 0, 0));

		CornerstoneTest.AreEqual(new Vector(150, 150), target.Offset);
	}

	[PresentationTestMethod]
	public void BringDescendantIntoViewShouldMoveChildAtLeastPartiallyAboveViewport()
	{
		var border = new Border
		{
			Width = 100,
			Height = 20
		};
		var content = new StackPanel
		{
			Orientation = Orientation.Vertical,
			Width = 100
		};

		for (var i = 0; i < 100; i++)
		{
			// border position will be (0,60)
			var child = i == 3
				? border
				: new Border
				{
					Width = 100,
					Height = 20
				};
			content.Children.Add(child);
		}
		var target = new ScrollContentPresenter
		{
			CanHorizontallyScroll = true,
			CanVerticallyScroll = true,
			Width = 200,
			Height = 100,
			Content = new Decorator
			{
				Child = content
			}
		};

		target.UpdateChild();
		target.Measure(Size.Infinity);
		target.Arrange(new Rect(0, 0, 100, 100));

		// move border to above the view port
		target.Offset = new Vector(0, 90);
		target.Arrange(new Rect(0, 0, 100, 100));
		target.BringDescendantIntoView(border, new Rect(border.Bounds.Size));

		CornerstoneTest.AreEqual(new Vector(0, 60), target.Offset);

		// move border to partially above the view port
		target.Offset = new Vector(0, 70);
		target.Arrange(new Rect(0, 0, 100, 100));
		target.BringDescendantIntoView(border, new Rect(border.Bounds.Size));

		CornerstoneTest.AreEqual(new Vector(0, 60), target.Offset);
	}

	[PresentationTestMethod]
	public void BringDescendantIntoViewShouldMoveChildAtLeastPartiallyBelowViewport()
	{
		var border = new Border
		{
			Width = 100,
			Height = 20
		};
		var content = new StackPanel
		{
			Orientation = Orientation.Vertical,
			Width = 100
		};

		for (var i = 0; i < 100; i++)
		{
			// border position will be (0,180)
			var child = i == 9
				? border
				: new Border
				{
					Width = 100,
					Height = 20
				};
			content.Children.Add(child);
		}
		var target = new ScrollContentPresenter
		{
			CanHorizontallyScroll = true,
			CanVerticallyScroll = true,
			Width = 200,
			Height = 100,
			Content = new Decorator
			{
				Child = content
			}
		};

		target.UpdateChild();
		target.Measure(Size.Infinity);
		target.Arrange(new Rect(0, 0, 100, 100));

		// border is at (0, 180) and below the viewport
		target.BringDescendantIntoView(border, new Rect(border.Bounds.Size));

		CornerstoneTest.AreEqual(new Vector(0, 100), target.Offset);

		// move border to partially below the view port
		target.Offset = new Vector(0, 90);
		target.BringDescendantIntoView(border, new Rect(border.Bounds.Size));
	}

	[PresentationTestMethod]
	public void BringDescendantIntoViewShouldMoveChildEvenWithMarginInParent()
	{
		var namescope = new NameScope();
		var content = new StackPanel
		{
			Orientation = Orientation.Vertical,
			Width = 100,
			Margin = new Thickness(0, 200)
		};

		for (var i = 0; i < 100; i++)
		{
			var child = new Border
			{
				Width = 100,
				Height = 20,
				Name = $"Border{i}"
			}.RegisterInNameScope(namescope);
			content.Children.Add(child);
		}
		var target = new ScrollContentPresenter
		{
			CanHorizontallyScroll = true,
			CanVerticallyScroll = true,
			Width = 200,
			Height = 100,
			Content = new Decorator
			{
				Child = content
			}
		};

		NameScope.SetNameScope(target, namescope);

		target.UpdateChild();
		target.Measure(Size.Infinity);
		target.Arrange(new Rect(0, 0, 100, 100));

		// Border20 is at position 0,600 with bottom at Y=620
		var border20 = target.GetControl<Border>("Border20");
		target.BringDescendantIntoView(border20, new Rect(border20.Bounds.Size));

		// With viewport Height of 100, border becomes fully visible when alligned from the bottom at Offset Y=520, i.e. 620-100
		CornerstoneTest.AreEqual(new Vector(0, 520), target.Offset);

		// Reset stack panel's margin
		content.Margin = default;
		target.Measure(Size.Infinity);
		target.Arrange(new Rect(0, 0, 100, 100));

		// Border20 is at position 0,800 with bottom at Y=820
		var border40 = target.GetControl<Border>("Border40");
		target.BringDescendantIntoView(border40, new Rect(border40.Bounds.Size));

		// With viewport Height of 100, border becomes fully visible when alligned from the bottom at Offset Y=720, i.e. 820-100
		CornerstoneTest.AreEqual(new Vector(0, 720), target.Offset);
	}

	[PresentationTestMethod]
	public void BringDescendantIntoViewShouldNotMoveChildIfCompletelyCoversViewport()
	{
		var border = new Border
		{
			Width = 100,
			Height = 200
		};
		var content = new StackPanel
		{
			Orientation = Orientation.Vertical,
			Width = 100
		};

		for (var i = 0; i < 100; i++)
		{
			// border position will be (0,60)
			var child = i == 3
				? border
				: new Border
				{
					Width = 100,
					Height = 20
				};
			content.Children.Add(child);
		}
		var target = new ScrollContentPresenter
		{
			CanHorizontallyScroll = true,
			CanVerticallyScroll = true,
			Width = 200,
			Height = 100,
			Content = new Decorator
			{
				Child = content
			}
		};

		target.UpdateChild();
		target.Measure(Size.Infinity);
		target.Arrange(new Rect(0, 0, 100, 100));

		// move border such that it's partially above viewport and partially below viewport
		target.Offset = new Vector(0, 90);
		target.Arrange(new Rect(0, 0, 100, 100));
		target.BringDescendantIntoView(border, new Rect(border.Bounds.Size));

		CornerstoneTest.AreEqual(new Vector(0, 90), target.Offset);
	}

	[PresentationTestMethod]
	public void BringDescendantIntoViewShouldNotMoveChildIfCompletelyInView()
	{
		var namescope = new NameScope();
		var content = new StackPanel
		{
			Orientation = Orientation.Vertical,
			Width = 100
		};

		for (var i = 0; i < 100; i++)
		{
			var child = new Border
			{
				Width = 100,
				Height = 20,
				Name = $"Border{i}"
			}.RegisterInNameScope(namescope);
			content.Children.Add(child);
		}
		var target = new ScrollContentPresenter
		{
			CanHorizontallyScroll = true,
			CanVerticallyScroll = true,
			Width = 200,
			Height = 100,
			Content = new Decorator
			{
				Child = content
			}
		};

		NameScope.SetNameScope(target, namescope);

		target.UpdateChild();
		target.Measure(Size.Infinity);
		target.Arrange(new Rect(0, 0, 100, 100));
		var border3 = target.GetControl<Border>("Border3");
		target.BringDescendantIntoView(border3, new Rect(border3.Bounds.Size));

		// Border3 is still in view, offset hasn't changed
		CornerstoneTest.AreEqual(new Vector(0, 0), target.Offset);
	}

	[PresentationTestMethod]
	public void BringDescendantIntoViewShouldUpdateOffset()
	{
		var target = new ScrollContentPresenter
		{
			Width = 100,
			Height = 100,
			CanVerticallyScroll = true,
			CanHorizontallyScroll = true,
			Content = new Border
			{
				Width = 200,
				Height = 200
			}
		};

		target.UpdateChild();
		target.Measure(Size.Infinity);
		target.Arrange(new Rect(0, 0, 100, 100));
		target.BringDescendantIntoView(target.Child!, new Rect(200, 200, 0, 0));

		CornerstoneTest.AreEqual(new Vector(100, 100), target.Offset);
	}

	[PresentationTestMethod]
	public void ContentCanBeLargerThanViewport()
	{
		TestControl content;
		var target = new ScrollContentPresenter
		{
			CanHorizontallyScroll = true,
			CanVerticallyScroll = true,
			Content = content = new TestControl()
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Rect(0, 0, 150, 150), content.Bounds);
	}

	[PresentationTestMethod]
	public void ContentCanBeOffset()
	{
		Border content;
		var target = new ScrollContentPresenter
		{
			CanHorizontallyScroll = true,
			CanVerticallyScroll = true,
			Content = content = new Border
			{
				Width = 150,
				Height = 150
			}
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		target.Offset = new Vector(25, 25);

		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Rect(-25, -25, 150, 150), content.Bounds);
	}

	[PresentationTestMethod]
	public void ContentLargerThanViewportShouldNotBeSquashedByPadding()
	{
		StackPanel content;
		var target = new ScrollContentPresenter
		{
			CanVerticallyScroll = true,
			Padding = new Thickness(10),
			Content = content = new StackPanel
			{
				Children =
				{
					new Border { Height = 50 },
					new Border { Height = 50 }
				}
			}
		};

		target.UpdateChild();
		target.Measure(new Size(50, 50));
		target.Arrange(new Rect(0, 0, 50, 50));

		CornerstoneTest.AreEqual(new Rect(10, 10, 30, 100), content.Bounds);
		CornerstoneTest.AreEqual(new Size(50, 120), target.Extent);
	}

	[PresentationTestMethod]
	public void DesiredSizeIsAvailableSizeWhenContentLargerThanAvailableSize()
	{
		var target = new ScrollContentPresenter
		{
			Padding = new Thickness(10),
			Content = new Border
			{
				MinWidth = 160,
				MinHeight = 160
			}
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Size(100, 100), target.DesiredSize);
	}

	[PresentationTestMethod]
	public void DesiredSizeIsContentSizePlusPaddingWhenSmallerThanAvailableSize()
	{
		var target = new ScrollContentPresenter
		{
			Padding = new Thickness(10),
			Content = new Border
			{
				MinWidth = 16,
				MinHeight = 16
			}
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Size(36, 36), target.DesiredSize);
	}

	[PresentationTestMethod]
	public void ExtentShouldBeRoundedToViewportWhenClose()
	{
		var root = new TestRoot
		{
			LayoutScaling = 1.75,
			UseLayoutRounding = true
		};

		var target = new ScrollContentPresenter
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Content = new Border
			{
				Width = 164.57142857142858,
				Height = 164.57142857142858,
				Margin = new Thickness(6)
			}
		};

		root.Child = target;
		target.UpdateChild();
		target.Measure(new Size(1000, 1000));
		target.Arrange(new Rect(0, 0, 1000, 1000));

		var nonRoundedVieViewport = target.Child!.Bounds.Size.Inflate(
			LayoutHelper.RoundLayoutThickness(target.Child.Margin, root.LayoutScaling));

		CornerstoneTest.AreEqual(new Size(176.00000000000003, 176.00000000000003), nonRoundedVieViewport);
		CornerstoneTest.AreEqual(new Size(176, 176), target.Viewport);
		CornerstoneTest.AreEqual(new Size(176, 176), target.Extent);
	}

	[PresentationTestMethod]
	public void ExtentShouldIncludeContentMargin()
	{
		var target = new ScrollContentPresenter
		{
			Content = new Border
			{
				Width = 100,
				Height = 100,
				Margin = new Thickness(5)
			}
		};

		target.UpdateChild();
		target.Measure(new Size(50, 50));
		target.Arrange(new Rect(0, 0, 50, 50));

		CornerstoneTest.AreEqual(new Size(110, 110), target.Extent);
	}

	[PresentationTestMethod]
	public void ExtentShouldIncludeContentMarginScaledWithLayoutRounding()
	{
		var root = new TestRoot
		{
			LayoutScaling = 1.25,
			UseLayoutRounding = true
		};

		var target = new ScrollContentPresenter
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Content = new Border
			{
				Width = 200,
				Height = 200,
				Margin = new Thickness(2)
			}
		};

		root.Child = target;
		target.UpdateChild();
		target.Measure(new Size(1000, 1000));
		target.Arrange(new Rect(0, 0, 1000, 1000));

		CornerstoneTest.AreEqual(new Size(203.2, 203.2), target.Viewport);
		CornerstoneTest.AreEqual(new Size(203.2, 203.2), target.Extent);
	}

	[PresentationTestMethod]
	public void ExtentShouldIncludePadding()
	{
		var target = new ScrollContentPresenter
		{
			CanHorizontallyScroll = true,
			CanVerticallyScroll = true,
			Padding = new Thickness(10),
			Content = new Border
			{
				Width = 100,
				Height = 100
			}
		};

		target.UpdateChild();
		target.Measure(new Size(50, 50));
		target.Arrange(new Rect(0, 0, 50, 50));

		CornerstoneTest.AreEqual(new Size(50, 50), target.Viewport);
		CornerstoneTest.AreEqual(new Size(120, 120), target.Extent);
	}

	[PresentationTestMethod]
	public void ExtentWidthShouldBeArrangeWidthWhenCanScrollHorizontallyFalse()
	{
		var child = new WrapPanel
		{
			Children =
			{
				new Border { Width = 40, Height = 50 },
				new Border { Width = 40, Height = 50 },
				new Border { Width = 40, Height = 50 }
			}
		};

		var target = new ScrollContentPresenter
		{
			Content = child,
			CanHorizontallyScroll = false
		};

		target.UpdateChild();
		target.Measure(Size.Infinity);
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Size(100, 100), target.Extent);
	}

	[PresentationTestMethod]
	public void MeasureShouldDeflateAvailableSizeByPadding()
	{
		var child = new TestControl();
		var target = new ScrollContentPresenter
		{
			Padding = new Thickness(10),
			Content = child
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));

		CornerstoneTest.AreEqual(new Size(80, 80), child.AvailableSize);
	}

	[PresentationTestMethod]
	public void MeasureShouldPassBoundedXIfCannotScrollHorizontally()
	{
		var child = new TestControl();
		var target = new ScrollContentPresenter
		{
			CanVerticallyScroll = true,
			Content = child
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));

		CornerstoneTest.AreEqual(new Size(100, double.PositiveInfinity), child.AvailableSize);
	}

	[PresentationTestMethod]
	public void MeasureShouldPassUnboundedXIfCanScrollHorizontally()
	{
		var child = new TestControl();
		var target = new ScrollContentPresenter
		{
			CanHorizontallyScroll = true,
			CanVerticallyScroll = true,
			Content = child
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));

		CornerstoneTest.AreEqual(Size.Infinity, child.AvailableSize);
	}

	[PresentationTestMethod]
	public void NestedPresentersShouldScrollOuterWhenContentExceedsViewport()
	{
		ScrollContentPresenter innerPresenter;
		Border border;

		var outerPresenter = new ScrollContentPresenter
		{
			CanHorizontallyScroll = true,
			CanVerticallyScroll = true,
			Width = 100,
			Height = 100,
			Content = innerPresenter = new ScrollContentPresenter
			{
				CanHorizontallyScroll = true,
				CanVerticallyScroll = true,
				Width = 100,
				Height = 200,
				Content = border = new Border
				{
					Width = 200, // larger than viewport
					Height = 25,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					Margin = new Thickness(0, 120, 0, 0)
				}
			}
		};

		innerPresenter.UpdateChild();
		outerPresenter.UpdateChild();
		outerPresenter.Measure(new Size(100, 100));
		outerPresenter.Arrange(new Rect(0, 0, 100, 100));

		border.BringIntoView();

		CornerstoneTest.AreEqual(new Vector(0, 45), outerPresenter.Offset);
		CornerstoneTest.AreEqual(new Vector(0, 0), innerPresenter.Offset);
	}

	[PresentationTestMethod]
	public void NestedPresentersShouldScrollOuterWhenViewportsAreClose()
	{
		ScrollContentPresenter innerPresenter;
		Border border;

		var outerPresenter = new ScrollContentPresenter
		{
			CanHorizontallyScroll = true,
			CanVerticallyScroll = true,
			Width = 100,
			Height = 170.0568181818182,
			UseLayoutRounding = false,
			Content = innerPresenter = new ScrollContentPresenter
			{
				CanHorizontallyScroll = true,
				CanVerticallyScroll = true,
				Width = 100,
				Height = 493.2613636363636,
				UseLayoutRounding = false,
				Content = new StackPanel
				{
					Children =
					{
						new Border
						{
							Height = 455.31818181818187,
							UseLayoutRounding = false
						},
						(border = new Border
						{
							Width = 100,
							Height = 37.94318181818182,
							UseLayoutRounding = false
						})
					}
				}
			}
		};

		innerPresenter.UpdateChild();
		outerPresenter.UpdateChild();
		outerPresenter.Measure(new Size(100, 170.0568181818182));
		outerPresenter.Arrange(new Rect(0, 0, 100, 170.0568181818182));

		border.BringIntoView();

		CornerstoneTest.AreEqual(new Vector(0, 323.20454545454544), outerPresenter.Offset);
		CornerstoneTest.AreEqual(new Vector(0, 0), innerPresenter.Offset);
	}

	[PresentationTestMethod]
	public void SettingOffsetShouldInvalidateArrange()
	{
		var target = new ScrollContentPresenter
		{
			Content = new Border { Width = 140, Height = 150 }
		};

		target.UpdateChild();
		target.Measure(new Size(100, 100));
		target.Arrange(new Rect(0, 0, 100, 100));
		target.Offset = new Vector(10, 100);

		CornerstoneTest.IsTrue(target.IsMeasureValid);
		CornerstoneTest.IsFalse(target.IsArrangeValid);
	}

	[PresentationTestMethod]
	public void ShouldCorrectlyArrangeChildLargerThanViewport()
	{
		var child = new Canvas { MinWidth = 150, MinHeight = 150 };
		var target = new ScrollContentPresenter { Content = child };

		target.UpdateChild();
		target.Measure(Size.Infinity);
		target.Arrange(new Rect(0, 0, 100, 100));

		CornerstoneTest.AreEqual(new Size(150, 150), child.Bounds.Size);
	}

	#endregion

	#region Classes

	private class TestControl : Control
	{
		#region Properties

		public Size AvailableSize { get; private set; }

		#endregion

		#region Methods

		protected override Size MeasureOverride(Size availableSize)
		{
			AvailableSize = availableSize;
			return new Size(150, 150);
		}

		#endregion
	}

	#endregion
}