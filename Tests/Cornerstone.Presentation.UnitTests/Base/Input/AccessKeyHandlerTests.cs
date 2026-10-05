#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Input;

[TestClass]
public class AccessKeyHandlerTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldNotRaiseAccessKeyForRegisteredAccessKeyNotMatchingKeySymbol()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var button = new Button();
			var root = new TestRoot(button);
			var target = new AccessKeyHandler();
			var raised = 0;

			KeyboardDevice.Instance?.SetFocusedElement(button, NavigationMethod.Unspecified, KeyModifiers.None);

			target.SetOwner(root);
			target.Register("A", button);
			button.AddHandler(AccessKeyHandler.AccessKeyEvent, (_, _) => ++raised);

			KeyDown(root, Key.LeftAlt);
			CornerstoneTest.AreEqual(0, raised);

			KeyDown(root, Key.A, "q", KeyModifiers.Alt);
			CornerstoneTest.AreEqual(0, raised);

			KeyUp(root, Key.A, "q", KeyModifiers.Alt);
			KeyUp(root, Key.LeftAlt);

			CornerstoneTest.AreEqual(0, raised);
		}
	}

	[PresentationTestMethod]
	public void ShouldOpenMainMenuOnAltKeyUp()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target = new AccessKeyHandler();
			var menu = new FakeMenu();
			var root = new TestRoot(menu);

			KeyboardDevice.Instance?.SetFocusedElement(menu, NavigationMethod.Unspecified,
				KeyModifiers.None);

			target.SetOwner(root);
			target.MainMenu = menu;

			KeyDown(root, Key.LeftAlt);
			CornerstoneTest.AreEqual(0, menu.TimesOpenCalled);

			KeyUp(root, Key.LeftAlt);
			CornerstoneTest.AreEqual(1, menu.TimesOpenCalled);
		}
	}

	[PresentationTestMethod]
	public void ShouldOpenMainMenuOnAltKeyUpWhenNothingIsFocused()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var target = new AccessKeyHandler();
			var menu = new FakeMenu();
			var root = new TestRoot(menu);

			CornerstoneTest.IsNull(KeyboardDevice.Instance?.FocusedElement);

			target.SetOwner(root);
			target.MainMenu = menu;

			KeyDown(root, Key.LeftAlt);
			CornerstoneTest.AreEqual(0, menu.TimesOpenCalled);

			KeyUp(root, Key.LeftAlt);
			CornerstoneTest.AreEqual(1, menu.TimesOpenCalled);
		}
	}

	[PresentationTestMethod]
	[DataRow("A", Key.A, "a")]
	[DataRow("A", Key.Q, "a")]
	[DataRow("é", Key.D2, "é")]
	[DataRow("2", Key.D2, "2")]
	public void ShouldRaiseAccessKeyForRegisteredAccessKeyMatchingKeySymbol(string registered, Key key, string keySymbol)
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var button = new Button();
			var root = new TestRoot(button);
			var target = new AccessKeyHandler();
			var raised = 0;

			KeyboardDevice.Instance?.SetFocusedElement(button, NavigationMethod.Unspecified, KeyModifiers.None);

			target.SetOwner(root);
			target.Register(registered, button);
			button.AddHandler(AccessKeyHandler.AccessKeyEvent, (s, e) => ++raised);

			KeyDown(root, Key.LeftAlt);
			CornerstoneTest.AreEqual(0, raised);

			KeyDown(root, key, keySymbol, KeyModifiers.Alt);
			CornerstoneTest.AreEqual(1, raised);

			KeyUp(root, key, keySymbol, KeyModifiers.Alt);
			KeyUp(root, Key.LeftAlt);

			CornerstoneTest.AreEqual(1, raised);
		}
	}

	[PresentationTestMethod]
	[DataRow(false, 0)]
	[DataRow(true, 1)]
	public void ShouldRaiseAccessKeyForRegisteredAccessKeyWhenEffectivelyEnabled(bool enabled, int expected)
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var button = new Button();
			var root = new TestRoot(button) { IsEnabled = enabled };
			var target = new AccessKeyHandler();
			var raised = 0;

			KeyboardDevice.Instance?.SetFocusedElement(button, NavigationMethod.Unspecified, KeyModifiers.None);

			target.SetOwner(root);
			target.Register("A", button);
			button.AddHandler(AccessKeyHandler.AccessKeyEvent, (s, e) => ++raised);

			KeyDown(root, Key.LeftAlt);
			CornerstoneTest.AreEqual(0, raised);

			KeyDown(root, Key.A, "a", KeyModifiers.Alt);
			CornerstoneTest.AreEqual(expected, raised);

			KeyUp(root, Key.A, "a", KeyModifiers.Alt);
			KeyUp(root, Key.LeftAlt);
			CornerstoneTest.AreEqual(expected, raised);
		}
	}

	[PresentationTestMethod]
	public void ShouldRaiseAccessKeyForSystemKeyEventWithKeySymbol()
	{
		// Regression test for #20961: on Windows, WM_SYSKEYDOWN (Alt+key) previously
		// left KeySymbol null, breaking access keys. MapVirtualKey now provides KeySymbol.
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var button = new Button();
			var root = new TestRoot(button);
			var target = new AccessKeyHandler();
			var raised = 0;

			KeyboardDevice.Instance?.SetFocusedElement(button, NavigationMethod.Unspecified, KeyModifiers.None);

			target.SetOwner(root);
			target.Register("F", button);
			button.AddHandler(AccessKeyHandler.AccessKeyEvent, (s, e) => ++raised);

			KeyDown(root, Key.LeftAlt);
			CornerstoneTest.AreEqual(0, raised);

			// MapVirtualKey provides lowercase KeySymbol for system key events
			KeyDown(root, Key.F, "f", KeyModifiers.Alt);
			CornerstoneTest.AreEqual(1, raised);
		}
	}

	[PresentationTestMethod]
	public void ShouldRaiseAccessKeyWhenFocusIsOnDescendant()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var button = new Button();
			var root = new TestRoot(button);
			var target = new AccessKeyHandler();
			var raised = 0;

			KeyboardDevice.Instance?.SetFocusedElement(button, NavigationMethod.Unspecified, KeyModifiers.None);

			target.SetOwner(root);
			target.Register("A", button);
			button.AddHandler(AccessKeyHandler.AccessKeyEvent, (s, e) => ++raised);

			KeyDown(button, Key.LeftAlt);
			KeyDown(button, Key.A, "a", KeyModifiers.Alt);

			CornerstoneTest.AreEqual(1, raised);
		}
	}

	[PresentationTestMethod]
	public void ShouldRaiseAccessKeyWhenNothingIsFocused()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var button = new Button();
			var root = new TestRoot(button);
			var target = new AccessKeyHandler();
			var raised = 0;

			CornerstoneTest.IsNull(KeyboardDevice.Instance?.FocusedElement);

			target.SetOwner(root);
			target.Register("A", button);
			button.AddHandler(AccessKeyHandler.AccessKeyEvent, (s, e) => ++raised);

			KeyDown(root, Key.LeftAlt);
			KeyDown(root, Key.A, "a", KeyModifiers.Alt);

			CornerstoneTest.AreEqual(1, raised);
		}
	}

	[PresentationTestMethod]
	public void ShouldRaiseAccessKeyWhenOwnerItselfIsFocused()
	{
		using (UnitTestApplication.Start(TestServices.RealFocus))
		{
			var button = new Button();
			var root = new TestRoot(button);
			var target = new AccessKeyHandler();
			var raised = 0;

			KeyboardDevice.Instance?.SetFocusedElement(root, NavigationMethod.Unspecified, KeyModifiers.None);

			target.SetOwner(root);
			target.Register("A", button);
			button.AddHandler(AccessKeyHandler.AccessKeyEvent, (s, e) => ++raised);

			KeyDown(root, Key.LeftAlt);
			KeyDown(root, Key.A, "a", KeyModifiers.Alt);

			CornerstoneTest.AreEqual(1, raised);
		}
	}

	[PresentationTestMethod]
	public void ShouldRaiseKeyEventsForAltKey()
	{
		var root = new TestRoot();
		var target = new AccessKeyHandler();
		var events = new List<string>();

		target.SetOwner(root);
		root.KeyDown += (s, e) => events.Add($"KeyDown {e.Key}");
		root.KeyUp += (s, e) => events.Add($"KeyUp {e.Key}");

		KeyDown(root, Key.LeftAlt);
		KeyUp(root, Key.LeftAlt);

		CornerstoneTest.AreEqual(new[]
		{
			"KeyDown LeftAlt",
			"KeyUp LeftAlt"
		}, events);
	}

	[PresentationTestMethod]
	public void ShouldRaiseKeyEventsForAltKeyWithMainMenu()
	{
		var root = new TestRoot();
		var target = new AccessKeyHandler();
		var menu = new StubMainMenu();
		var events = new List<string>();

		target.SetOwner(root);
		target.MainMenu = menu;

		root.KeyDown += (s, e) => events.Add($"KeyDown {e.Key}");
		root.KeyUp += (s, e) => events.Add($"KeyUp {e.Key}");

		KeyDown(root, Key.LeftAlt);
		KeyUp(root, Key.LeftAlt);
		KeyDown(root, Key.LeftAlt);
		KeyUp(root, Key.LeftAlt);

		CornerstoneTest.AreEqual(new[]
		{
			"KeyDown LeftAlt",
			"KeyUp LeftAlt",
			"KeyDown LeftAlt",
			"KeyUp LeftAlt"
		}, events);
	}

	[PresentationTestMethod]
	public void ShouldRaiseKeyEventsForRegisteredAccessKey()
	{
		var button = new Button();
		var root = new TestRoot(button);
		var target = new AccessKeyHandler();
		var events = new List<string>();

		target.SetOwner(root);
		target.Register("A", button);
		root.KeyDown += (s, e) => events.Add($"KeyDown {e.Key}");
		root.KeyUp += (s, e) => events.Add($"KeyUp {e.Key}");

		KeyDown(root, Key.LeftAlt);
		KeyDown(root, Key.A, "a", KeyModifiers.Alt);
		KeyUp(root, Key.A, "a", KeyModifiers.Alt);
		KeyUp(root, Key.LeftAlt);

		// AccessKeyHandler marks the Alt+A KeyDown as Handled once it matches a registered
		// access key, so a plain KeyDown subscriber doesn't see it. KeyUp is unaffected: only
		// KeyUp for the Alt key itself is handled, not arbitrary registered access keys.
		CornerstoneTest.AreEqual(new[]
		{
			"KeyDown LeftAlt",
			"KeyUp A",
			"KeyUp LeftAlt"
		}, events);
	}

	[PresentationTestMethod]
	public void ShouldRaiseKeyEventsForUnregisteredAccessKey()
	{
		var root = new TestRoot();
		var target = new AccessKeyHandler();
		var events = new List<string>();

		target.SetOwner(root);
		root.KeyDown += (s, e) => events.Add($"KeyDown {e.Key}");
		root.KeyUp += (s, e) => events.Add($"KeyUp {e.Key}");

		KeyDown(root, Key.LeftAlt);
		KeyDown(root, Key.A, "a", KeyModifiers.Alt);
		KeyUp(root, Key.A, "a", KeyModifiers.Alt);
		KeyUp(root, Key.LeftAlt);

		CornerstoneTest.AreEqual(new[]
		{
			"KeyDown LeftAlt",
			"KeyDown A",
			"KeyUp A",
			"KeyUp LeftAlt"
		}, events);
	}

	[PresentationTestMethod]
	public void ShouldRaiseKeyEventsForUnregisteredAccessKeyWithMainMenu()
	{
		var root = new TestRoot();
		var target = new AccessKeyHandler();
		var menu = new StubMainMenu();
		var events = new List<string>();

		target.SetOwner(root);
		target.MainMenu = menu;
		root.KeyDown += (s, e) => events.Add($"KeyDown {e.Key}");
		root.KeyUp += (s, e) => events.Add($"KeyUp {e.Key}");

		KeyDown(root, Key.LeftAlt);
		KeyDown(root, Key.A, "a", KeyModifiers.Alt);
		KeyUp(root, Key.A, "a", KeyModifiers.Alt);
		KeyUp(root, Key.LeftAlt);

		CornerstoneTest.AreEqual(new[]
		{
			"KeyDown LeftAlt",
			"KeyDown A",
			"KeyUp A",
			"KeyUp LeftAlt"
		}, events);
	}

	private static void KeyDown(IInputElement target, Key key, string keySymbol = null, KeyModifiers modifiers = KeyModifiers.None)
	{
		target.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = key,
			KeySymbol = keySymbol,
			KeyModifiers = modifiers
		});
	}

	private static void KeyUp(IInputElement target, Key key, string keySymbol = null, KeyModifiers modifiers = KeyModifiers.None)
	{
		target.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyUpEvent,
			Key = key,
			KeySymbol = keySymbol,
			KeyModifiers = modifiers
		});
	}

	#endregion

	#region Classes

	private class FakeMenu : Menu
	{
		#region Properties

		public int TimesOpenCalled { get; set; }

		#endregion

		#region Methods

		public override void Open()
		{
			TimesOpenCalled++;
		}

		#endregion
	}

	#endregion
}