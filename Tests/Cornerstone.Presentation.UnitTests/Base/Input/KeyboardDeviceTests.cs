#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.UnitTests.Base.Utilities;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Input;

[TestClass]
public class KeyboardDeviceTests
{
	#region Methods

	[PresentationTestMethod]
	public void CanChangeKeyBindingsInKeybindingEventHandler()
	{
		var target = new KeyboardDevice();
		var button = new Button();
		var root = new TestRoot(button);
		var raised = 0;

		button.KeyBindings.Add(new KeyBinding
		{
			Gesture = new KeyGesture(Key.O, KeyModifiers.Control),
			Command = new DelegateCommand(() =>
			{
				button.KeyBindings.Clear();
				++raised;
			})
		});

		target.SetFocusedElement(button, NavigationMethod.Pointer, 0);
		target.ProcessRawEvent(
			new RawKeyEventArgs(
				target,
				0,
				root,
				RawKeyEventType.KeyDown,
				Key.O,
				RawInputModifiers.Control,
				PhysicalKey.O,
				"o"));

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void CancelledFocusChangeShouldNotSendGotFocusEvent()
	{
		var target = new KeyboardDevice();
		var focused = new Control();
		var root = new TestRoot();
		var focusCancelled = false;

		focused.GettingFocus += (s, e) => { focusCancelled = e.TryCancel(); };

		focused.GotFocus += (s, e) => { focusCancelled = false; };

		target.SetFocusedElement(
			focused,
			NavigationMethod.Unspecified,
			KeyModifiers.None);

		CornerstoneTest.IsTrue(focusCancelled);
	}

	[PresentationTestMethod]
	public void ControlFocusShouldBeSetBeforeFocusedElementRaisesPropertyChanged()
	{
		var target = new KeyboardDevice();
		var focused = new Control();
		var root = new TestRoot();
		var gotFocusRaised = 0;
		var propertyChangedRaised = 0;

		focused.GotFocus += (s, e) => ++gotFocusRaised;

		target.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == nameof(target.FocusedElement))
			{
				CornerstoneTest.AreEqual(1, gotFocusRaised);
				++propertyChangedRaised;
			}
		};

		target.SetFocusedElement(
			focused,
			NavigationMethod.Unspecified,
			KeyModifiers.None);

		CornerstoneTest.AreEqual(1, propertyChangedRaised);
	}

	[PresentationTestMethod]
	public void KeypressesShouldBeSentToFocusedElement()
	{
		var target = new KeyboardDevice();
		var focused = new Control();
		var root = new TestRoot();
		var raised = 0;

		target.SetFocusedElement(
			focused,
			NavigationMethod.Unspecified,
			KeyModifiers.None);

		focused.KeyDown += (s, e) => ++raised;

		target.ProcessRawEvent(
			new RawKeyEventArgs(
				target,
				0,
				root,
				RawKeyEventType.KeyDown,
				Key.A,
				RawInputModifiers.None,
				PhysicalKey.A,
				"a"));

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void KeypressesShouldBeSentToRootIfNoFocusedElement()
	{
		using (UnitTestApplication.Start(TestServices.FocusableWindow))
		{
			var window = new Window();
			window.FocusManager.Focus(null);
			var raised = 0;
			window.KeyDown += (sender, ev) =>
			{
				if ((sender == window) && (ev.RoutedEvent == InputElement.KeyDownEvent))
				{
					raised++;
				}
			};
			KeyboardDevice.Instance!.ProcessRawEvent(
				new RawKeyEventArgs(
					KeyboardDevice.Instance,
					0,
					window.GetInputRoot()!,
					RawKeyEventType.KeyDown,
					Key.A,
					RawInputModifiers.None,
					PhysicalKey.A,
					"a"));
			CornerstoneTest.AreEqual(1, raised);
		}
	}

	[PresentationTestMethod]
	public void RedirectedFocusShouldChangeFocusedElement()
	{
		var target = new KeyboardDevice();
		var first = new Control();
		var second = new Control();
		var stack = new StackPanel();
		stack.Children.AddRange(new[] { first, second });
		var root = new TestRoot(stack);

		first.GettingFocus += (s, e) => { e.TrySetNewFocusedElement(second); };

		target.SetFocusedElement(
			first,
			NavigationMethod.Unspecified,
			KeyModifiers.None);

		CornerstoneTest.IsTrue(second.IsFocused);
	}

	[PresentationTestMethod]
	public void TextInputShouldBeSentToFocusedElement()
	{
		var target = new KeyboardDevice();
		var focused = new Control();
		var root = new TestRoot();
		var raised = 0;

		target.SetFocusedElement(
			focused,
			NavigationMethod.Unspecified,
			KeyModifiers.None);

		focused.TextInput += (s, e) => ++raised;

		target.ProcessRawEvent(
			new RawTextInputEventArgs(
				target,
				0,
				root,
				"Foo"));

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void TextInputShouldBeSentToRootIfNoFocusedElement()
	{
		using (UnitTestApplication.Start(TestServices.FocusableWindow))
		{
			var window = new Window();
			window.FocusManager.Focus(null);
			var raised = 0;
			window.TextInput += (sender, ev) =>
			{
				if ((sender == window) && (ev.RoutedEvent == InputElement.TextInputEvent))
				{
					raised++;
				}
			};
			KeyboardDevice.Instance!.ProcessRawEvent(
				new RawTextInputEventArgs(
					KeyboardDevice.Instance,
					0,
					window.GetInputRoot()!,
					"Foo"));
			CornerstoneTest.AreEqual(1, raised);
		}
	}

	#endregion
}