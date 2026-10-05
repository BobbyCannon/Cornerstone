#region References

using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class StackPanelTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ArrangesHorizontalChildrenWithCorrectBounds()
	{
		var target = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Children =
			{
				new TestControl
				{
					VerticalAlignment = VerticalAlignment.Top,
					MeasureSize = new Size(10, 50)
				},
				new TestControl
				{
					VerticalAlignment = VerticalAlignment.Top,
					MeasureSize = new Size(10, 150)
				},
				new TestControl
				{
					VerticalAlignment = VerticalAlignment.Center,
					MeasureSize = new Size(10, 50)
				},
				new TestControl
				{
					VerticalAlignment = VerticalAlignment.Center,
					MeasureSize = new Size(10, 150)
				},
				new TestControl
				{
					VerticalAlignment = VerticalAlignment.Bottom,
					MeasureSize = new Size(10, 50)
				},
				new TestControl
				{
					VerticalAlignment = VerticalAlignment.Bottom,
					MeasureSize = new Size(10, 150)
				},
				new TestControl
				{
					VerticalAlignment = VerticalAlignment.Stretch,
					MeasureSize = new Size(10, 50)
				},
				new TestControl
				{
					VerticalAlignment = VerticalAlignment.Stretch,
					MeasureSize = new Size(10, 150)
				}
			}
		};

		target.Measure(new Size(150, 100));
		CornerstoneTest.AreEqual(new Size(80, 100), target.DesiredSize);

		target.Arrange(new Rect(target.DesiredSize));

		var bounds = target.Children.Select(x => x.Bounds).ToArray();

		CornerstoneTest.AreEqual(new[]
		{
			new Rect(0, 0, 10, 50),
			new Rect(10, 0, 10, 100),
			new Rect(20, 25, 10, 50),
			new Rect(30, 0, 10, 100),
			new Rect(40, 50, 10, 50),
			new Rect(50, 0, 10, 100),
			new Rect(60, 0, 10, 100),
			new Rect(70, 0, 10, 100)
		}, bounds);
	}

	[PresentationTestMethod]
	public void ArrangesVerticalChildrenWithCorrectBounds()
	{
		var target = new StackPanel
		{
			Orientation = Orientation.Vertical,
			Children =
			{
				new TestControl
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					MeasureSize = new Size(50, 10)
				},
				new TestControl
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					MeasureSize = new Size(150, 10)
				},
				new TestControl
				{
					HorizontalAlignment = HorizontalAlignment.Center,
					MeasureSize = new Size(50, 10)
				},
				new TestControl
				{
					HorizontalAlignment = HorizontalAlignment.Center,
					MeasureSize = new Size(150, 10)
				},
				new TestControl
				{
					HorizontalAlignment = HorizontalAlignment.Right,
					MeasureSize = new Size(50, 10)
				},
				new TestControl
				{
					HorizontalAlignment = HorizontalAlignment.Right,
					MeasureSize = new Size(150, 10)
				},
				new TestControl
				{
					HorizontalAlignment = HorizontalAlignment.Stretch,
					MeasureSize = new Size(50, 10)
				},
				new TestControl
				{
					HorizontalAlignment = HorizontalAlignment.Stretch,
					MeasureSize = new Size(150, 10)
				}
			}
		};

		target.Measure(new Size(100, 150));
		CornerstoneTest.AreEqual(new Size(100, 80), target.DesiredSize);

		target.Arrange(new Rect(target.DesiredSize));

		var bounds = target.Children.Select(x => x.Bounds).ToArray();

		CornerstoneTest.AreEqual(new[]
		{
			new Rect(0, 0, 50, 10),
			new Rect(0, 10, 100, 10),
			new Rect(25, 20, 50, 10),
			new Rect(0, 30, 100, 10),
			new Rect(50, 40, 50, 10),
			new Rect(0, 50, 100, 10),
			new Rect(0, 60, 100, 10),
			new Rect(0, 70, 100, 10)
		}, bounds);
	}

	[PresentationTestMethod]
	public void LaysOutChildrenHorizontally()
	{
		var target = new StackPanel
		{
			Orientation = Orientation.Horizontal,
			Children =
			{
				new Border { Width = 20, Height = 120 },
				new Border { Width = 30 },
				new Border { Width = 50 }
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(100, 120), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 20, 120), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(20, 0, 30, 120), target.Children[1].Bounds);
		CornerstoneTest.AreEqual(new Rect(50, 0, 50, 120), target.Children[2].Bounds);
	}

	[PresentationTestMethod]
	public void LaysOutChildrenHorizontallyEvenIfLargerThanPanel()
	{
		var target = new StackPanel
		{
			Width = 60,
			Orientation = Orientation.Horizontal,
			Children =
			{
				new Border { Width = 20, Height = 120 },
				new Border { Width = 30 },
				new Border { Width = 50 }
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(60, 120), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 20, 120), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(20, 0, 30, 120), target.Children[1].Bounds);
		CornerstoneTest.AreEqual(new Rect(50, 0, 50, 120), target.Children[2].Bounds);
	}

	[PresentationTestMethod]
	public void LaysOutChildrenHorizontallyWithSpacing()
	{
		var target = new StackPanel
		{
			Spacing = 10,
			Orientation = Orientation.Horizontal,
			Children =
			{
				new Border { Width = 20, Height = 120 },
				new Border { Width = 30 },
				new Border { Width = 50 }
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(120, 120), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 20, 120), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(30, 0, 30, 120), target.Children[1].Bounds);
		CornerstoneTest.AreEqual(new Rect(70, 0, 50, 120), target.Children[2].Bounds);
	}

	[PresentationTestMethod]
	public void LaysOutChildrenVertically()
	{
		var target = new StackPanel
		{
			Children =
			{
				new Border { Height = 20, Width = 120 },
				new Border { Height = 30 },
				new Border { Height = 50 }
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(120, 100), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 120, 20), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 20, 120, 30), target.Children[1].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 50, 120, 50), target.Children[2].Bounds);
	}

	[PresentationTestMethod]
	public void LaysOutChildrenVerticallyEvenIfLargerThanPanel()
	{
		var target = new StackPanel
		{
			Height = 60,
			Children =
			{
				new Border { Height = 20, Width = 120 },
				new Border { Height = 30 },
				new Border { Height = 50 }
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(120, 60), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 120, 20), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 20, 120, 30), target.Children[1].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 50, 120, 50), target.Children[2].Bounds);
	}

	[PresentationTestMethod]
	public void LaysOutChildrenVerticallyWithSpacing()
	{
		var target = new StackPanel
		{
			Spacing = 10,
			Children =
			{
				new Border { Height = 20, Width = 120 },
				new Border { Height = 30 },
				new Border { Height = 50 }
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));

		CornerstoneTest.AreEqual(new Size(120, 120), target.Bounds.Size);
		CornerstoneTest.AreEqual(new Rect(0, 0, 120, 20), target.Children[0].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 30, 120, 30), target.Children[1].Bounds);
		CornerstoneTest.AreEqual(new Rect(0, 70, 120, 50), target.Children[2].Bounds);
	}

	[PresentationTestMethod]
	[DataRow(Orientation.Horizontal)]
	[DataRow(Orientation.Vertical)]
	public void OnlyArrangeVisibleChildren(Orientation orientation)
	{
		var hiddenPanel = new Panel { Width = 10, Height = 10, IsVisible = false };
		var panel = new Panel { Width = 10, Height = 10 };

		var target = new StackPanel
		{
			Spacing = 40,
			Orientation = orientation,
			Children =
			{
				hiddenPanel,
				panel
			}
		};

		target.Measure(Size.Infinity);
		target.Arrange(new Rect(target.DesiredSize));
		CornerstoneTest.AreEqual(new Rect(0, 0, 10, 10), panel.Bounds);
	}

	[PresentationTestMethod]
	[DataRow(Orientation.Horizontal)]
	[DataRow(Orientation.Vertical)]
	public void SpacingNotAddedForChildrenHiddenByStyleAppliedDuringMeasure(Orientation orientation)
	{
		var target = new StackPanel
		{
			Spacing = 40,
			Orientation = orientation,
			Children =
			{
				new StackPanel { Width = 10, Height = 10, Classes = { "hidden" } },
				new StackPanel { Width = 10, Height = 10 },
				new StackPanel { Width = 10, Height = 10 }
			}
		};

		var root = new TestRoot(target);

		root.Styles.Add(new Style(x => x.OfType<StackPanel>().Class("hidden"))
		{
			Setters = { new Setter(Visual.IsVisibleProperty, false) }
		});

		target.Measure(Size.Infinity);

		var expected = orientation == Orientation.Horizontal ? new Size(60, 10) : new Size(10, 60);

		CornerstoneTest.AreEqual(expected, target.DesiredSize);
	}

	[PresentationTestMethod]
	[DataRow(Orientation.Horizontal)]
	[DataRow(Orientation.Vertical)]
	public void SpacingNotAddedForInvisibleChildren(Orientation orientation)
	{
		var targetThreeChildrenOneInvisble = new StackPanel
		{
			Spacing = 40,
			Orientation = orientation,
			Children =
			{
				new StackPanel { Width = 10, Height = 10, IsVisible = false },
				new StackPanel { Width = 10, Height = 10 },
				new StackPanel { Width = 10, Height = 10 }
			}
		};
		var targetTwoChildrenNoneInvisible = new StackPanel
		{
			Spacing = 40,
			Orientation = orientation,
			Children =
			{
				new StackPanel { Width = 10, Height = 10 },
				new StackPanel { Width = 10, Height = 10 }
			}
		};

		targetThreeChildrenOneInvisble.Measure(Size.Infinity);
		targetThreeChildrenOneInvisble.Arrange(new Rect(targetThreeChildrenOneInvisble.DesiredSize));

		targetTwoChildrenNoneInvisible.Measure(Size.Infinity);
		targetTwoChildrenNoneInvisible.Arrange(new Rect(targetTwoChildrenNoneInvisible.DesiredSize));

		var sizeWithTwoChildren = targetTwoChildrenNoneInvisible.Bounds.Size;
		var sizeWithThreeChildren = targetThreeChildrenOneInvisble.Bounds.Size;

		CornerstoneTest.AreEqual(sizeWithTwoChildren, sizeWithThreeChildren);
	}

	#endregion

	#region Classes

	private class TestControl : Control
	{
		#region Properties

		public Size MeasureConstraint { get; private set; }
		public Size MeasureSize { get; set; }

		#endregion

		#region Methods

		protected override Size MeasureOverride(Size availableSize)
		{
			MeasureConstraint = availableSize;
			return MeasureSize;
		}

		#endregion
	}

	#endregion
}