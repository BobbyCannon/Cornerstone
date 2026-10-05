#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Layout;

[TestClass]
public class MeasureTests
{
	#region Methods

	[PresentationTestMethod]
	public void InvalidatingChildShouldNotInvalidateParent()
	{
		var panel = new StackPanel();
		var child = new Border();
		panel.Children.Add(child);

		panel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

		CornerstoneTest.AreEqual(new Size(0, 0), panel.DesiredSize);

		child.Width = 100;
		child.Height = 100;

		CornerstoneTest.IsTrue(panel.IsMeasureValid);
		CornerstoneTest.IsFalse(child.IsMeasureValid);

		panel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		CornerstoneTest.AreEqual(new Size(0, 0), panel.DesiredSize);
	}

	[PresentationTestMethod]
	public void MarginShouldAffectAvailableSize()
	{
		MeasureTest target;

		var outer = new Decorator
		{
			Width = 100,
			Height = 100,
			Child = target = new MeasureTest
			{
				Margin = new Thickness(10)
			}
		};

		outer.Measure(Size.Infinity);

		CornerstoneTest.AreEqual(new Size(80, 80), target.AvailableSize);
	}

	[PresentationTestMethod]
	public void MarginShouldBeAppliedBeforeWidthHeight()
	{
		MeasureTest target;

		var outer = new Decorator
		{
			Width = 100,
			Height = 100,
			Child = target = new MeasureTest
			{
				Width = 80,
				Height = 80,
				Margin = new Thickness(10)
			}
		};

		outer.Measure(Size.Infinity);

		CornerstoneTest.AreEqual(new Size(80, 80), target.AvailableSize);
	}

	[PresentationTestMethod]
	public void MarginShouldBeIncludedInDesiredSize()
	{
		var decorator = new Decorator
		{
			Width = 100,
			Height = 100,
			Margin = new Thickness(8)
		};

		decorator.Measure(Size.Infinity);

		CornerstoneTest.AreEqual(new Size(116, 116), decorator.DesiredSize);
	}

	[PresentationTestMethod]
	public void NegativeMarginLargerThanConstraintShouldRequestHeight0()
	{
		Control target;

		var outer = new Decorator
		{
			Width = 100,
			Height = 100,
			Child = target = new Control
			{
				Margin = new Thickness(0, -100, 0, 0)
			}
		};

		outer.Measure(Size.Infinity);

		CornerstoneTest.AreEqual(0, target.DesiredSize.Height);
	}

	[PresentationTestMethod]
	public void NegativeMarginLargerThanConstraintShouldRequestWidth0()
	{
		Control target;

		var outer = new Decorator
		{
			Width = 100,
			Height = 100,
			Child = target = new Control
			{
				Margin = new Thickness(-100, 0, 0, 0)
			}
		};

		outer.Measure(Size.Infinity);

		CornerstoneTest.AreEqual(0, target.DesiredSize.Width);
	}

	[PresentationTestMethod]
	public void RemovingFromParentShouldInvalidateMeasureOfControlAndDescendants()
	{
		var panel = new StackPanel();
		var child2 = new Border();
		var child1 = new Border { Child = child2 };
		panel.Children.Add(child1);

		panel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
		CornerstoneTest.IsTrue(child1.IsMeasureValid);
		CornerstoneTest.IsTrue(child2.IsMeasureValid);

		panel.Children.Remove(child1);
		CornerstoneTest.IsFalse(child1.IsMeasureValid);
		CornerstoneTest.IsFalse(child2.IsMeasureValid);
	}

	[PresentationTestMethod]
	public void StyleHidingControlShouldBeAppliedBeforeMeasuring()
	{
		var child = new Border
		{
			Width = 100,
			Height = 100,
			Classes = { "hidden" }
		};
		var target = new Decorator
		{
			Child = child
		};
		var root = new TestRoot(target);

		root.Styles.Add(new Style(x => x.OfType<Border>().Class("hidden"))
		{
			Setters = { new Setter(Visual.IsVisibleProperty, false) }
		});

		target.Measure(Size.Infinity);

		CornerstoneTest.IsFalse(child.IsVisible);
		CornerstoneTest.AreEqual(new Size(0, 0), child.DesiredSize);
		CornerstoneTest.AreEqual(new Size(0, 0), target.DesiredSize);
	}

	#endregion

	#region Classes

	private class MeasureTest : Control
	{
		#region Properties

		public Size? AvailableSize { get; private set; }

		#endregion

		#region Methods

		protected override Size MeasureOverride(Size availableSize)
		{
			AvailableSize = availableSize;
			return availableSize;
		}

		#endregion
	}

	#endregion
}