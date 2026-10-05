#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Overlays;
using Cornerstone.Presentation.Headless;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Platform;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Rendering;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class HeadlessProbeTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void CanRouteRealInputThroughHeadlessToplevel()
	{
		using var app = HeadlessUnitTestApplication.Start();

		var clicked = false;
		var button = new Button { Width = 100, Height = 30, Content = "hi" };
		button.Click += (_, _) => clicked = true;
		var window = new Window { Width = 400, Height = 300, Content = button };
		window.Show();
		app.RunJobs();

		var pt = button.Bounds.Center;
		window.MouseDown(pt, MouseButton.Left);
		window.MouseUp(pt, MouseButton.Left);
		app.RunJobs();

		CornerstoneTest.IsTrue(clicked);
	}

	[PresentationTestMethod]
	public void CanShowWindowAndLayOut()
	{
		using var app = HeadlessUnitTestApplication.Start();

		var button = new Button { Width = 100, Height = 30, Content = "hi" };
		var window = new Window { Width = 400, Height = 300, Content = button };
		window.Show();
		app.RunJobs();

		CornerstoneTest.AreEqual(new Size(100, 30), button.Bounds.Size);
		CornerstoneTest.AreEqual(new Size(400, 300), window.ClientSize);
	}

	[PresentationTestMethod]
	public void KeyboardInputReachesFocusedControl()
	{
		using var app = HeadlessUnitTestApplication.Start();

		var box = new TextBox { Width = 200, Height = 30 };
		var window = new Window { Width = 400, Height = 300, Content = box };
		window.Show();
		app.RunJobs();

		box.Focus();
		app.RunJobs();
		window.KeyTextInput("abc");
		app.RunJobs();

		CornerstoneTest.AreEqual("abc", box.Text);
	}

	[PresentationTestMethod]
	public void OverlayPopupRequiresAnAppliedWindowTemplate()
	{
		using var app = HeadlessUnitTestApplication.Start(new PresentationHeadlessPlatformOptions { OverlayPopups = true });

		// The overlay layer is looked up through the visual tree, so a window that was
		// never shown has nothing to find.
		var untemplated = new Popup { PlacementTarget = new Window() };
		var ex = Assert.Throws<InvalidOperationException>(() => untemplated.Open());
		CornerstoneTest.Contains(ex.Message, "no overlay layer is found");

		var window = new Window();
		window.Show();
		app.RunJobs();
		CornerstoneTest.IsNotNull(window.GetVisualDescendants().OfType<VisualLayerManager>().FirstOrDefault());

		var templated = new Popup { PlacementTarget = window };
		templated.Open();
		CornerstoneTest.IsType<OverlayPopupHost>(templated.Host);
	}

	[PresentationTestMethod]
	public void PlatformServicesAreRegisteredBeforeTheFirstWindowAndStayStable()
	{
		using var app = HeadlessUnitTestApplication.Start();

		object[] Resolve()
		{
			return
			[
				PresentationLocator.Current.GetService<IKeyboardDevice>(),
				PresentationLocator.Current.GetService<IPlatformSettings>(),
				PresentationLocator.Current.GetService<ICursorFactory>(),
				PresentationLocator.Current.GetService<PlatformHotkeyConfiguration>(),
				PresentationLocator.Current.GetService<IClipboard>(),
				PresentationLocator.Current.GetService<IRenderLoop>(),
				PresentationLocator.Current.GetService<IPlatformIconLoader>(),
				PresentationLocator.Current.GetService<KeyGestureFormatInfo>()
			];
		}

		var before = Resolve();
		CornerstoneTest.All(before, x => CornerstoneTest.IsNotNull(x));

		new Window().Show();
		app.RunJobs();

		// Initializing the platform lazily used to replace these mid-test, leaving anything that
		// resolved early holding a different instance than the window does.
		CornerstoneTest.AreEqual(before, Resolve(), ReferenceEqualityComparer.Instance);
	}

	[PresentationTestMethod]
	public void PopupOpensAsRealPopupRoot()
	{
		using var app = HeadlessUnitTestApplication.Start();

		var popup = new Popup { Placement = PlacementMode.Pointer, Child = new Border { Width = 50, Height = 50 } };
		var window = new Window { Width = 400, Height = 300, Content = new Panel { Children = { popup } } };
		window.Show();
		app.RunJobs();

		popup.Open();
		app.RunJobs();

		CornerstoneTest.IsType<PopupRoot>(popup.Host);
	}

	[PresentationTestMethod]
	public void PopupUsesOverlayPopupHostWhenOverlayPopupsEnabled()
	{
		using var app = HeadlessUnitTestApplication.Start(new PresentationHeadlessPlatformOptions { OverlayPopups = true });

		var popup = new Popup { Placement = PlacementMode.Pointer, Child = new Border { Width = 50, Height = 50 } };
		var window = new Window { Width = 400, Height = 300, Content = new Panel { Children = { popup } } };
		window.Show();
		app.RunJobs();

		popup.Open();
		app.RunJobs();

		CornerstoneTest.IsType<OverlayPopupHost>(popup.Host);
	}

	#endregion
}