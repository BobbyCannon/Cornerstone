#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Base.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Styling;

[TestClass]
public class ContainerTests
{
	#region Methods

	[PresentationTestMethod]
	public void ContainerCannotBeAddedToStyleChildren()
	{
		var target = new ContainerQuery();
		var style = new Style();

		Assert.Throws<InvalidOperationException>(() => style.Children.Add(target));
	}

	[PresentationTestMethod]
	public void ContainerHeightQueriesMatches()
	{
		using var app = UnitTestApplication.Start();
		var root = new LayoutTestRoot
		{
			ClientSize = new Size(400, 400)
		};
		var containerQuery1 = new ContainerQuery(x => new HeightQuery(x, StyleQueryComparisonOperator.LessThanOrEquals, 500));
		containerQuery1.Children.Add(new Style(x => x.Is<Border>())
		{
			Setters = { new Setter(Control.HeightProperty, 200.0) }
		});
		var containerQuery2 = new ContainerQuery(x => new HeightQuery(x, StyleQueryComparisonOperator.GreaterThan, 500));
		containerQuery2.Children.Add(new Style(x => x.Is<Border>())
		{
			Setters = { new Setter(Control.HeightProperty, 500.0) }
		});
		root.Styles.Add(containerQuery1);
		root.Styles.Add(containerQuery2);
		var child = new Border
		{
			Name = "Child",
			VerticalAlignment = VerticalAlignment.Stretch
		};
		var stack = new StackPanel();
		stack.Children.Add(child);
		var border = new Border
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Child = stack,
			Name = "Parent"
		};
		Container.SetSizing(border, ContainerSizing.Height);

		root.Child = border;

		root.LayoutManager.ExecuteInitialLayoutPass();
		CornerstoneTest.AreEqual(200, child.Height);

		root.ClientSize = new Size(600, 600);
		root.InvalidateMeasure();

		root.LayoutManager.ExecuteLayoutPass();
		CornerstoneTest.AreEqual(500, child.Height);
	}

	[PresentationTestMethod]
	public void ContainerHeightQueriesMatchesName()
	{
		using var app = UnitTestApplication.Start();
		var root = new LayoutTestRoot
		{
			ClientSize = new Size(600, 600)
		};
		var containerQuery1 = new ContainerQuery(x => new HeightQuery(x, StyleQueryComparisonOperator.LessThanOrEquals, 500));
		containerQuery1.Children.Add(new Style(x => x.Is<Border>())
		{
			Setters = { new Setter(Control.HeightProperty, 200.0) }
		});
		var containerQuery2 = new ContainerQuery(x => new HeightQuery(x, StyleQueryComparisonOperator.LessThanOrEquals, 450), "TEST");
		containerQuery2.Children.Add(new Style(x => x.Is<Border>())
		{
			Setters = { new Setter(Control.HeightProperty, 300.0) }
		});
		root.Styles.Add(containerQuery1);
		root.Styles.Add(containerQuery2);
		var child = new Border
		{
			Name = "Child",
			VerticalAlignment = VerticalAlignment.Stretch
		};
		var controlInner = new ContentControl
		{
			Width = 400,
			Height = 400,
			Content = child,
			Name = "Inner"
		};
		Container.SetSizing(controlInner, ContainerSizing.Height);
		Container.SetName(controlInner, "TEST");
		var stack = new StackPanel();
		stack.Children.Add(controlInner);
		var border = new Border
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Child = stack,
			Name = "Parent"
		};
		Container.SetSizing(border, ContainerSizing.Height);

		root.Child = border;

		root.LayoutManager.ExecuteInitialLayoutPass();

		root.LayoutManager.ExecuteLayoutPass();
		CornerstoneTest.AreEqual(300, child.Height);
	}

	[PresentationTestMethod]
	public void ContainerWidthQueriesMatches()
	{
		using var app = UnitTestApplication.Start();
		var root = new LayoutTestRoot
		{
			ClientSize = new Size(400, 400)
		};
		var containerQuery1 = new ContainerQuery(x => new WidthQuery(x, StyleQueryComparisonOperator.LessThanOrEquals, 500));
		containerQuery1.Children.Add(new Style(x => x.Is<Border>())
		{
			Setters = { new Setter(Control.WidthProperty, 200.0) }
		});
		var containerQuery2 = new ContainerQuery(x => new WidthQuery(x, StyleQueryComparisonOperator.GreaterThan, 500));
		containerQuery2.Children.Add(new Style(x => x.Is<Border>())
		{
			Setters = { new Setter(Control.WidthProperty, 500.0) }
		});
		root.Styles.Add(containerQuery1);
		root.Styles.Add(containerQuery2);
		var child = new Border
		{
			Name = "Child",
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		var stack = new StackPanel();
		stack.Children.Add(child);
		var border = new Border
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Child = stack,
			Name = "Parent"
		};
		Container.SetSizing(border, ContainerSizing.Width);

		root.Child = border;

		root.LayoutManager.ExecuteInitialLayoutPass();
		CornerstoneTest.AreEqual(200, child.Width);

		root.ClientSize = new Size(600, 600);
		root.InvalidateMeasure();

		root.LayoutManager.ExecuteLayoutPass();
		CornerstoneTest.AreEqual(500, child.Width);
	}

	[PresentationTestMethod]
	public void ContainerWidthQueriesMatchesName()
	{
		using var app = UnitTestApplication.Start();
		var root = new LayoutTestRoot
		{
			ClientSize = new Size(600, 600)
		};
		var containerQuery1 = new ContainerQuery(x => new WidthQuery(x, StyleQueryComparisonOperator.LessThanOrEquals, 500));
		containerQuery1.Children.Add(new Style(x => x.Is<Border>())
		{
			Setters = { new Setter(Control.WidthProperty, 200.0) }
		});
		var containerQuery2 = new ContainerQuery(x => new WidthQuery(x, StyleQueryComparisonOperator.LessThanOrEquals, 500), "TEST");
		containerQuery2.Children.Add(new Style(x => x.Is<Border>())
		{
			Setters = { new Setter(Control.WidthProperty, 300.0) }
		});
		root.Styles.Add(containerQuery1);
		root.Styles.Add(containerQuery2);
		var child = new Border
		{
			Name = "Child",
			HorizontalAlignment = HorizontalAlignment.Stretch
		};
		var controlInner = new ContentControl
		{
			Width = 400,
			Height = 400,
			Content = child,
			Name = "Inner"
		};
		Container.SetSizing(controlInner, ContainerSizing.Width);
		Container.SetName(controlInner, "TEST");
		var stack = new StackPanel();
		stack.Children.Add(controlInner);
		var border = new Border
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Child = stack,
			Name = "Parent"
		};
		Container.SetSizing(border, ContainerSizing.Width);

		root.Child = border;

		root.LayoutManager.ExecuteInitialLayoutPass();

		root.LayoutManager.ExecuteLayoutPass();
		CornerstoneTest.AreEqual(300, child.Width);
	}

	#endregion
}