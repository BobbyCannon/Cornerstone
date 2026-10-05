#region References

using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Theme;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class TreeViewBringIntoViewTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void BringIntoViewShouldNotScrollWhenItemIsAlreadyVisible()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var (root, treeView) = CreateTarget([CreateHeader(100), CreateHeader(500), CreateHeader(900)]);

		root.LayoutManager.ExecuteInitialLayoutPass();

		var scrollViewer = GetScrollViewer(treeView);
		CornerstoneTest.AreEqual(0, scrollViewer.Offset.X);

		var item = CornerstoneTest.IsType<TreeViewItem>(treeView.ContainerFromIndex(0));
		item.BringIntoView();
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.AreEqual(0, scrollViewer.Offset.X);
	}

	[PresentationTestMethod]
	public void BringIntoViewShouldRevealANestedItem()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		// A narrow item nested three levels deep, under wide ancestors that make the tree scroll.
		var nestedHeader = CreateHeader(100);
		var nestedItem = new TreeViewItem { Header = nestedHeader };
		var item = nestedItem;

		for (var i = 0; i < 3; i++)
		{
			item = new TreeViewItem
			{
				Header = CreateHeader(900),
				IsExpanded = true,
				ItemsSource = new[] { item }
			};
		}

		var (root, treeView) = CreateTarget([item]);

		root.LayoutManager.ExecuteInitialLayoutPass();

		var scrollViewer = GetScrollViewer(treeView);

		scrollViewer.Offset = new Vector(scrollViewer.Extent.Width - scrollViewer.Viewport.Width, 0);
		root.LayoutManager.ExecuteLayoutPass();
		CornerstoneTest.IsTrue(scrollViewer.Offset.X > 0);

		nestedItem.BringIntoView();
		root.LayoutManager.ExecuteLayoutPass();

		var headerBounds = new Rect(nestedHeader.Bounds.Size).TransformToAABB(nestedHeader.TransformToVisual(scrollViewer)!.Value);

		CornerstoneTest.IsTrue((headerBounds.Left >= -0.5) && (headerBounds.Right <= (scrollViewer.Viewport.Width + 0.5)));
	}

	[PresentationTestMethod]
	public void BringIntoViewShouldScrollBackToItemScrolledOffToTheLeft()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var (root, treeView) = CreateTarget([CreateHeader(100), CreateHeader(500), CreateHeader(900)]);

		root.LayoutManager.ExecuteInitialLayoutPass();

		var scrollViewer = GetScrollViewer(treeView);

		scrollViewer.Offset = new Vector(scrollViewer.Extent.Width - scrollViewer.Viewport.Width, 0);
		root.LayoutManager.ExecuteLayoutPass();

		var startOffset = scrollViewer.Offset.X;
		CornerstoneTest.IsTrue(startOffset > 0);

		// The first item is narrow and now completely off to the left:
		// bringing it into view must scroll back so that its header becomes visible.
		var item = CornerstoneTest.IsType<TreeViewItem>(treeView.ContainerFromIndex(0));
		item.BringIntoView();
		root.LayoutManager.ExecuteLayoutPass();

		CornerstoneTest.IsTrue(scrollViewer.Offset.X < 30);
	}

	private static Border CreateHeader(double width)
	{
		return new()
		{
			Width = width,
			Height = 20,
			HorizontalAlignment = HorizontalAlignment.Left,
			Background = Brushes.Red
		};
	}

	private static (TestRoot Root, TreeView TreeView) CreateTarget(Control[] headers)
	{
		var treeView = new TreeView
		{
			Width = 400,
			Height = 200,
			ItemsSource = headers
		};

		var root = new TestRoot { Width = 400, Height = 200 };
		root.Resources.MergedDictionaries.Add(new CornerstoneTheme());
		root.Child = treeView;

		return (root, treeView);
	}

	private static ScrollViewer GetScrollViewer(TreeView treeView)
	{
		return treeView.GetVisualDescendants().OfType<ScrollViewer>().Single();
	}

	#endregion
}