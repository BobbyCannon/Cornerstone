#region References

using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Harfbuzz;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class TextSelectionCanvasTests : ScopedTestBase
{
	#region Properties

	private static TestServices Services =>
		TestServices.MockThreadingInterface.With(
			standardCursorFactory: new StubCursorFactory(),
			renderInterface: new HeadlessPlatformRenderInterface(),
			textShaperImpl: new HarfBuzzTextShaper(),
			fontManagerImpl: new TestFontManager(),
			keyboardDevice: () => new KeyboardDevice(),
			keyboardNavigation: () => new KeyboardNavigationHandler(),
			inputManager: new InputManager(),
			assetLoader: new StandardAssetLoader());

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void TextSelectionHandleMovesWithScrolling()
	{
		using (UnitTestApplication.Start(Services))
		{
			var touchHelper = new TouchTestHelper();
			var rootBorder = new Border
			{
				Width = 200,
				Height = 600
			};
			var visualLayerManager = new VisualLayerManager
			{
				Child = rootBorder,
				EnableTextSelectorLayer = true
			};
			var impl = CreateMockTopLevelImpl();
			var topLevel = new TestTopLevel(impl)
			{
				Template = CreateTopLevelTemplate(),
				Content = visualLayerManager
			};

			var focusedTextBox = CreateTextBox();

			var panel = new StackPanel
			{
				Orientation = Orientation.Vertical,
				Spacing = 20,
				Children =
				{
					focusedTextBox,
					CreateTextBox(),
					CreateTextBox(),
					CreateTextBox(),
					CreateTextBox(),
					CreateTextBox(),
					CreateTextBox(),
					CreateTextBox(),
					CreateTextBox(),
					CreateTextBox(),
					CreateTextBox(),
					CreateTextBox(),
					CreateTextBox(),
					CreateTextBox(),
					CreateTextBox(),
					CreateTextBox(),
					CreateTextBox(),
					CreateTextBox(),
					CreateTextBox(),
					CreateTextBox(),
					CreateTextBox(),
					CreateTextBox(),
					CreateTextBox(),
					CreateTextBox()
				}
			};

			var scrollViewer = new ScrollViewer
			{
				Template = new FuncControlTemplate<ScrollViewer>(ScrollViewerTests.CreateTemplate),
				Content = panel
			};

			rootBorder.Child = scrollViewer;

			topLevel.LayoutManager.ExecuteInitialLayoutPass();

			touchHelper.Tap(focusedTextBox);

			topLevel.LayoutManager.ExecuteLayoutPass();

			var presenter = focusedTextBox.FindDescendantOfType<TextPresenter>()!;
			var canvas = presenter.TextSelectionHandleCanvas;

			CornerstoneTest.IsNotNull(canvas);

			var handle = canvas.Children.FirstOrDefault() as TextSelectionHandle;

			CornerstoneTest.IsNotNull(handle);

			CornerstoneTest.AreEqual(new Point(25.5, 14.5), handle.GetTopLeft());

			scrollViewer.Offset = new Vector(0, 50);

			topLevel.LayoutManager.ExecuteLayoutPass();

			CornerstoneTest.AreEqual(new Point(25.5, -35.5), handle.GetTopLeft());
		}

		TextBox CreateTextBox()
		{
			return new TextBox
			{
				Template = TextBoxTests.CreateTemplate(),
				Text = "Test",
				Width = 150
			};
		}
	}

	private static StubWindowImpl CreateMockTopLevelImpl()
	{
		var topLevel = new StubWindowImpl();
		return topLevel;
	}

	private static FuncControlTemplate<TestTopLevel> CreateTopLevelTemplate()
	{
		return new FuncControlTemplate<TestTopLevel>((x, scope) =>
			new ContentPresenter
			{
				Name = "PART_ContentPresenter",
				[!ContentPresenter.ContentProperty] = x[!ContentControl.ContentProperty]
			}.RegisterInNameScope(scope));
	}

	#endregion

	#region Classes

	private class TestTopLevel(ITopLevelImpl impl) : TopLevel(impl)
	{
	}

	#endregion
}