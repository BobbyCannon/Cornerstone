#region References

using System.Collections.Specialized;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class DecoratorTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ChangingContentShouldFireLogicalChildrenCollectionChanged()
	{
		var decorator = new Decorator();
		var child1 = new Control();
		var child2 = new Control();
		var called = false;

		decorator.Child = child1;

		((ILogical) decorator).LogicalChildren.CollectionChanged += (s, e) => called = true;

		decorator.Child = child2;

		CornerstoneTest.IsTrue(called);
	}

	[PresentationTestMethod]
	public void ClearingContentShouldClearChildControlsParent()
	{
		var decorator = new Decorator();
		var child = new Control();

		decorator.Child = child;
		decorator.Child = null;

		CornerstoneTest.IsNull(child.Parent);
		CornerstoneTest.IsNull(((ILogical) child).LogicalParent);
	}

	[PresentationTestMethod]
	public void ClearingContentShouldFireLogicalChildrenCollectionChanged()
	{
		var decorator = new Decorator();
		var child = new Control();
		var called = false;

		decorator.Child = child;

		((ILogical) decorator).LogicalChildren.CollectionChanged += (s, e) =>
			called = e.Action == NotifyCollectionChangedAction.Remove;

		decorator.Child = null;

		CornerstoneTest.IsTrue(called);
	}

	[PresentationTestMethod]
	public void ClearingContentShouldRemoveFromLogicalChildren()
	{
		var decorator = new Decorator();
		var child = new Control();

		decorator.Child = child;
		decorator.Child = null;

		CornerstoneTest.AreEqual(new ILogical[0], ((ILogical) decorator).LogicalChildren.ToList());
	}

	[PresentationTestMethod]
	public void ContentControlShouldAppearInLogicalChildren()
	{
		var decorator = new Decorator();
		var child = new Control();

		decorator.Child = child;

		CornerstoneTest.AreEqual(new[] { child }, ((ILogical) decorator).LogicalChildren.ToList());
	}

	[PresentationTestMethod]
	public void MeasureShouldReturnPaddingWhenNoChildPresent()
	{
		var target = new Decorator
		{
			Padding = new Thickness(8)
		};

		target.Measure(new Size(100, 100));

		CornerstoneTest.AreEqual(new Size(16, 16), target.DesiredSize);
	}

	[PresentationTestMethod]
	public void SettingContentShouldFireLogicalChildrenCollectionChanged()
	{
		var decorator = new Decorator();
		var child = new Control();
		var called = false;

		((ILogical) decorator).LogicalChildren.CollectionChanged += (s, e) =>
			called = e.Action == NotifyCollectionChangedAction.Add;

		decorator.Child = child;

		CornerstoneTest.IsTrue(called);
	}

	[PresentationTestMethod]
	public void SettingContentShouldSetChildControlsParent()
	{
		var decorator = new Decorator();
		var child = new Control();

		decorator.Child = child;

		CornerstoneTest.AreEqual(child.Parent, decorator);
		CornerstoneTest.AreEqual(((ILogical) child).LogicalParent, decorator);
	}

	#endregion

	#region Classes

	[TestClass]
	public class UseLayoutRounding : ScopedTestBase
	{
		#region Methods

		[PresentationTestMethod]
		public void MeasureRoundsPadding()
		{
			var target = new Decorator
			{
				Padding = new Thickness(1),
				Child = new Canvas
				{
					Width = 101,
					Height = 101
				}
			};

			var root = CreatedRoot(1.5, target);

			root.LayoutManager.ExecuteInitialLayoutPass();

			// - 1 pixel padding is rounded up to 1.3333; for both sides it is 2.6666
			// - Size of 101 gets rounded up to 101.3333
			// - Desired size = 101.3333 + 2.6666 = 104
			CornerstoneTest.AreEqual(new Size(104, 104), target.DesiredSize);
		}

		private static TestRoot CreatedRoot(
			double scaling,
			Control child,
			Size? constraint = null)
		{
			return new TestRoot
			{
				LayoutScaling = scaling,
				UseLayoutRounding = true,
				Child = child,
				ClientSize = constraint ?? new Size(1000, 1000)
			};
		}

		#endregion
	}

	#endregion
}