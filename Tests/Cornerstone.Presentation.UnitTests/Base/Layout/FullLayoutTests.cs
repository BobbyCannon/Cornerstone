#region References

using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Layout;

[TestClass]
public class FullLayoutTests
{
	#region Methods

	[PresentationTestMethod]
	public void GrandchildSizeChanged()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			Border border;
			TextBlock textBlock;

			var window = new Window
			{
				SizeToContent = SizeToContent.WidthAndHeight,
				Content = border = new Border
				{
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					Child = new Border
					{
						Child = textBlock = new TextBlock
						{
							Width = 400,
							Height = 400,
							Text = "Hello World!"
						}
					}
				}
			};

			window.Show();

			CornerstoneTest.AreEqual(new Size(400, 400), border.Bounds.Size);
			textBlock.Width = 200;
			window.LayoutManager.ExecuteLayoutPass();

			CornerstoneTest.AreEqual(new Size(200, 400), border.Bounds.Size);
		}
	}

	[PresentationTestMethod]
	public void TestScrollViewerWithTextBlock()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			ScrollViewer scrollViewer;
			TextBlock textBlock;

			var window = new Window
			{
				Width = 800,
				Height = 600,
				Content = scrollViewer = new ScrollViewer
				{
					Width = 200,
					Height = 200,
					HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					Content = textBlock = new TextBlock
					{
						Width = 400,
						Height = 400,
						Text = "Hello World!"
					}
				}
			};

			window.Resources["ScrollBarThickness"] = 10.0;

			window.Show();

			CornerstoneTest.AreEqual(new Size(800, 600), window.Bounds.Size);
			CornerstoneTest.AreEqual(new Size(200, 200), scrollViewer.Bounds.Size);
			CornerstoneTest.AreEqual(new Point(300, 200), Position(scrollViewer));
			CornerstoneTest.AreEqual(new Size(400, 400), textBlock.Bounds.Size);

			var scrollBars = scrollViewer.GetTemplateDescendants().OfType<ScrollBar>().ToList();
			var presenters = scrollViewer.GetTemplateDescendants().OfType<ScrollContentPresenter>().ToList();

			CornerstoneTest.AreEqual(2, scrollBars.Count);
			CornerstoneTest.Single(presenters);

			var presenter = presenters[0];
			CornerstoneTest.AreEqual(new Size(200, 200), presenter.Bounds.Size);

			var horzScroll = scrollBars.Single(x => x.Orientation == Orientation.Horizontal);
			var vertScroll = scrollBars.Single(x => x.Orientation == Orientation.Vertical);

			CornerstoneTest.IsTrue(horzScroll.IsVisible);
			CornerstoneTest.IsTrue(vertScroll.IsVisible);
		}
	}

	private static Point Position(Visual v)
	{
		return v.Bounds.Position;
	}

	#endregion
}