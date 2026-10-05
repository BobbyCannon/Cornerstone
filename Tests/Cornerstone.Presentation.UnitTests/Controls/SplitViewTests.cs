#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Navigation;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class SplitViewTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void SplitViewCancelCloseShouldPreventPaneFromClosing()
	{
		var splitView = new SplitView();
		splitView.IsPaneOpen = true;

		splitView.PaneClosing += (x, e) => { e.Cancel = true; };

		splitView.IsPaneOpen = false;

		CornerstoneTest.IsTrue(splitView.IsPaneOpen);
	}

	[PresentationTestMethod]
	public void SplitViewEscapeKeyClosesLightDismissablePane()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow
			.With(globalClock: new MockGlobalClock()));
		var wnd = new Window
		{
			Width = 1280,
			Height = 720
		};
		var button = new Button();
		var splitView = new SplitView
		{
			Pane = button
		};
		wnd.Content = splitView;
		wnd.Show();

		splitView.IsPaneOpen = true;

		button.RaiseEvent(new KeyEventArgs
		{
			Key = Key.Escape,
			RoutedEvent = InputElement.KeyDownEvent
		});

		CornerstoneTest.IsFalse(splitView.IsPaneOpen);

		splitView.DisplayMode = SplitViewDisplayMode.Inline;

		splitView.IsPaneOpen = true;

		button.RaiseEvent(new KeyEventArgs
		{
			Key = Key.Escape,
			RoutedEvent = InputElement.KeyDownEvent
		});

		CornerstoneTest.IsTrue(splitView.IsPaneOpen);
	}

	[PresentationTestMethod]
	public void SplitViewPaneClosingShouldFireBeforePaneClosed()
	{
		var splitView = new SplitView();
		splitView.IsPaneOpen = true;

		var handledClosing = false;
		splitView.PaneClosing += (x, e) => { handledClosing = true; };

		splitView.PaneClosed += (x, e) => { CornerstoneTest.IsTrue(handledClosing); };

		splitView.IsPaneOpen = false;
	}

	[PresentationTestMethod]
	public void SplitViewPaneOpeningShouldFireBeforePaneOpened()
	{
		var splitView = new SplitView();

		var handledOpening = false;
		splitView.PaneOpening += (x, e) => { handledOpening = true; };

		splitView.PaneOpened += (x, e) => { CornerstoneTest.IsTrue(handledOpening); };

		splitView.IsPaneOpen = true;
	}

	[PresentationTestMethod]
	public void SplitViewPointerClosesPaneInOverlayMode()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow
			.With(globalClock: new MockGlobalClock()));
		var wnd = new Window
		{
			Width = 1280,
			Height = 720
		};
		var splitView = new SplitView();
		wnd.Content = splitView;
		wnd.Show();

		splitView.IsPaneOpen = true;

		splitView.RaiseEvent(new PointerReleasedEventArgs(splitView,
			null!, wnd, new Point(1270, 30), 0,
			new PointerPointProperties(),
			KeyModifiers.None,
			MouseButton.Left));

		CornerstoneTest.IsFalse(splitView.IsPaneOpen);

		// Inline shouldn't close the pane
		splitView.DisplayMode = SplitViewDisplayMode.Inline;
		splitView.IsPaneOpen = true;

		splitView.RaiseEvent(new PointerReleasedEventArgs(splitView,
			null!, wnd, new Point(1270, 30), 0,
			new PointerPointProperties(),
			KeyModifiers.None,
			MouseButton.Left));

		CornerstoneTest.IsTrue(splitView.IsPaneOpen);
	}

	[PresentationTestMethod]
	public void SplitViewPointerShouldNotClosePaneIfOverPane()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow
			.With(globalClock: new MockGlobalClock()));
		var wnd = new Window
		{
			Width = 1280,
			Height = 720
		};
		var clickBorder = new Border
		{
			Width = 100,
			Height = 100,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top
		};
		var splitView = new SplitView
		{
			Pane = clickBorder
		};
		wnd.Content = splitView;
		wnd.Show();

		splitView.IsPaneOpen = true;

		clickBorder.RaiseEvent(new PointerReleasedEventArgs(splitView,
			null!, wnd, new Point(5, 5), 0,
			new PointerPointProperties(),
			KeyModifiers.None,
			MouseButton.Left));

		CornerstoneTest.IsTrue(splitView.IsPaneOpen);
	}

	[PresentationTestMethod]
	public void SplitViewShouldntClosePanelWhenIsPaneOpenTrueThenDisplayModeChanged()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow
			.With(globalClock: new MockGlobalClock()));
		var wnd = new Window
		{
			Width = 1280,
			Height = 720
		};
		var splitView = new SplitView();
		splitView.DisplayMode = SplitViewDisplayMode.CompactOverlay;
		wnd.Content = splitView;
		wnd.Show();

		splitView.IsPaneOpen = true;

		splitView.RaiseEvent(new PointerReleasedEventArgs(splitView,
			null!, wnd, new Point(1270, 30), 0,
			new PointerPointProperties(),
			KeyModifiers.None,
			MouseButton.Left));

		CornerstoneTest.IsFalse(splitView.IsPaneOpen);

		// Inline shouldn't close the pane
		splitView.IsPaneOpen = true;

		// Change the display mode once the pane is already open.
		splitView.DisplayMode = SplitViewDisplayMode.Inline;

		splitView.RaiseEvent(new PointerReleasedEventArgs(splitView,
			null!, wnd, new Point(1270, 30), 0,
			new PointerPointProperties(),
			KeyModifiers.None,
			MouseButton.Left));

		CornerstoneTest.IsTrue(splitView.IsPaneOpen);
	}

	[PresentationTestMethod]
	public void SplitViewTemplateSettingsAreCorrectForDisplayModes()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow
			.With(globalClock: new MockGlobalClock()));
		var wnd = new Window
		{
			Width = 1280,
			Height = 720
		};
		var splitView = new SplitView();
		wnd.Content = splitView;
		wnd.Show();

		var zeroGridLength = new GridLength(0);
		var compactLength = splitView.CompactPaneLength;
		var compactGridLength = new GridLength(compactLength);

		// Overlay is default DisplayMode
		CornerstoneTest.AreEqual(0, splitView.TemplateSettings.ClosedPaneWidth);
		CornerstoneTest.AreEqual(zeroGridLength, splitView.TemplateSettings.PaneColumnGridLength);

		splitView.DisplayMode = SplitViewDisplayMode.CompactOverlay;
		CornerstoneTest.AreEqual(compactLength, splitView.TemplateSettings.ClosedPaneWidth);
		CornerstoneTest.AreEqual(compactGridLength, splitView.TemplateSettings.PaneColumnGridLength);

		splitView.DisplayMode = SplitViewDisplayMode.Inline;
		CornerstoneTest.AreEqual(0, splitView.TemplateSettings.ClosedPaneWidth);
		CornerstoneTest.AreEqual(GridLength.Auto, splitView.TemplateSettings.PaneColumnGridLength);

		splitView.DisplayMode = SplitViewDisplayMode.CompactInline;
		CornerstoneTest.AreEqual(compactLength, splitView.TemplateSettings.ClosedPaneWidth);
		CornerstoneTest.AreEqual(GridLength.Auto, splitView.TemplateSettings.PaneColumnGridLength);
	}

	[PresentationTestMethod]
	public void SplitViewTemplateSettingsUpdateWithCompactPaneLength()
	{
		var splitView = new SplitView();

		// CompactInline:
		//    - ClosedPaneWidth = CompactPaneLength
		//    - PaneColumnGridLength = Auto
		splitView.DisplayMode = SplitViewDisplayMode.CompactInline;

		var compactLength = splitView.CompactPaneLength;

		CornerstoneTest.AreEqual(GridLength.Auto, splitView.TemplateSettings.PaneColumnGridLength);
		CornerstoneTest.AreEqual(compactLength, splitView.TemplateSettings.ClosedPaneWidth);

		splitView.CompactPaneLength = 100;

		CornerstoneTest.AreEqual(GridLength.Auto, splitView.TemplateSettings.PaneColumnGridLength);
		CornerstoneTest.AreEqual(100, splitView.TemplateSettings.ClosedPaneWidth);

		// CompactOverlay:
		//    - ClosedPaneWidth = CompactPaneLength
		//    - PaneColumnGridLength = GridLength { CompactPaneLength, Pixel }
		splitView.DisplayMode = SplitViewDisplayMode.CompactOverlay;
		splitView.CompactPaneLength = 50;

		CornerstoneTest.AreEqual(new GridLength(50), splitView.TemplateSettings.PaneColumnGridLength);
		CornerstoneTest.AreEqual(50, splitView.TemplateSettings.ClosedPaneWidth);

		// Value shouldn't change for these - changing the display mode will update
		// the template settings with the right value
		splitView.DisplayMode = SplitViewDisplayMode.Inline;
		splitView.CompactPaneLength = 1;

		CornerstoneTest.AreEqual(GridLength.Auto, splitView.TemplateSettings.PaneColumnGridLength);
		CornerstoneTest.AreEqual(0, splitView.TemplateSettings.ClosedPaneWidth);

		splitView.DisplayMode = SplitViewDisplayMode.Overlay;
		splitView.CompactPaneLength = 2;

		CornerstoneTest.AreEqual(new GridLength(0), splitView.TemplateSettings.PaneColumnGridLength);
		CornerstoneTest.AreEqual(0, splitView.TemplateSettings.ClosedPaneWidth);
	}

	[PresentationTestMethod]
	public void TopLevelBackRequestedClosesLightDismissablePane()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow
			.With(globalClock: new MockGlobalClock()));
		var wnd = new Window
		{
			Width = 1280,
			Height = 720
		};
		var splitView = new SplitView();
		wnd.Content = splitView;
		wnd.Show();

		splitView.IsPaneOpen = true;

		wnd.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent));

		CornerstoneTest.IsFalse(splitView.IsPaneOpen);

		splitView.DisplayMode = SplitViewDisplayMode.Inline;
		splitView.IsPaneOpen = true;

		wnd.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent));

		CornerstoneTest.IsTrue(splitView.IsPaneOpen);
	}

	[PresentationTestMethod]
	[DataRow(SplitViewDisplayMode.Overlay)]
	[DataRow(SplitViewDisplayMode.CompactOverlay)]
	public void TopLevelBackRequestedShouldNotBeHandledWhenPaneIsClosed(SplitViewDisplayMode displayMode)
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow
			.With(globalClock: new MockGlobalClock()));
		var wnd = new Window
		{
			Width = 1280,
			Height = 720
		};
		var splitView = new SplitView
		{
			DisplayMode = displayMode
		};
		wnd.Content = splitView;
		wnd.Show();

		// Pane is closed: the SplitView must ignore the event so back navigation can proceed.
		CornerstoneTest.IsFalse(splitView.IsPaneOpen);

		var closedArgs = new RoutedEventArgs(TopLevel.BackRequestedEvent);
		wnd.RaiseEvent(closedArgs);

		CornerstoneTest.IsFalse(closedArgs.Handled);

		// Pane is open: the SplitView should close it and handle the event.
		splitView.IsPaneOpen = true;

		var openArgs = new RoutedEventArgs(TopLevel.BackRequestedEvent);
		wnd.RaiseEvent(openArgs);

		CornerstoneTest.IsTrue(openArgs.Handled);
		CornerstoneTest.IsFalse(splitView.IsPaneOpen);
	}

	[PresentationTestMethod]
	public void WithDefaultIsPaneOpenValueShouldHaveClosedPseudoClassSet()
	{
		// Testing this control Pseudo Classes requires placing the SplitView on a window
		// prior to asserting them, because some of the pseudo classes are set either when
		// the template is applied or the control is attached to the visual tree
		using var app = UnitTestApplication.Start(TestServices.StyledWindow
			.With(globalClock: new MockGlobalClock()));
		var wnd = new Window
		{
			Width = 1280,
			Height = 720
		};
		var splitView = new SplitView();
		wnd.Content = splitView;
		wnd.Show();

		CornerstoneTest.Contains(":closed".Equals, splitView.Classes);
	}

	#endregion
}