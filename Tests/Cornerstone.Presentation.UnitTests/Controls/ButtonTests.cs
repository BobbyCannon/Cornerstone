#region References

using System;
using System.Threading;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Overlays;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Controls.Utils;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MouseButton = Cornerstone.Presentation.Input.MouseButton;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ButtonTests : ScopedTestBase
{
	#region Fields

	private readonly MouseTestHelper _helper = new();

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void ButtonCommandParameterDoesNotChangeWhileExecution()
	{
		var target = new Button();
		object lastParameter = "A";
		var generator = new Random();
		var onlyOnce = false;
		var command = new TestCommand(parameter =>
			{
				if (!onlyOnce)
				{
					onlyOnce = true;
					target.CommandParameter = generator.Next();
				}
				lastParameter = parameter;
				return true;
			},
			parameter => { CornerstoneTest.AreEqual(lastParameter, parameter); });
		target.CommandParameter = lastParameter;
		target.Command = command;
		var root = new TestRoot { Child = target };

		(target as IClickableControl).RaiseClick();
	}

	[PresentationTestMethod]
	public void ButtonDoesNotRaiseClickWhenPointerReleasedOutside()
	{
		var root = new TestRoot();
		var target = new Button
		{
			Width = 100,
			Height = 100
		};
		root.Child = target;

		var clicked = false;

		target.Click += (s, e) => clicked = true;

		RaisePointerEntered(target);
		RaisePointerMove(target, new Point(50, 50));
		RaisePointerPressed(target, 1, MouseButton.Left, new Point(50, 50));
		RaisePointerExited(target);

		CornerstoneTest.AreEqual(_helper.Captured, target);

		RaisePointerReleased(target, MouseButton.Left, new Point(200, 50));

		CornerstoneTest.AreEqual(_helper.Captured, null);

		CornerstoneTest.IsFalse(clicked);
	}

	[PresentationTestMethod]
	public void ButtonDoesNotSubscribeToCommandCanExecuteChangedUntilAddedToLogicalTree()
	{
		var command = new TestCommand(true);
		var target = new Button
		{
			Command = command
		};

		CornerstoneTest.AreEqual(0, command.SubscriptionCount);
	}

	[PresentationTestMethod]
	public void ButtonInvokesCanExecuteWhenCommandParameterChanged()
	{
		var target = new Button();
		var raised = 0;

		target.Click += (s, e) => ++raised;

		target.RaiseEvent(new AccessKeyEventArgs("b", false));

		CornerstoneTest.AreEqual(1, raised);
	}

	[PresentationTestMethod]
	public void ButtonInvokesDoesntExecuteWhenButtonDisabled()
	{
		var target = new Button();
		var raised = 0;

		target.IsEnabled = false;
		target.Click += (s, e) => ++raised;

		target.RaiseEvent(new AccessKeyEventArgs("b", false));

		CornerstoneTest.AreEqual(0, raised);
	}

	[PresentationTestMethod]
	public void ButtonIsCancelShouldNotWorkWhenButtonIsNotEffectivelyVisible()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var raised = 0;
			var panel = new Panel();
			var target = new Button();
			panel.Children.Add(target);
			var window = new Window { Content = panel };
			window.Show();

			target.Click += (s, e) => ++raised;

			target.IsCancel = true;
			panel.IsVisible = false;
			window.RaiseEvent(CreateKeyDownEvent(Key.Escape));
			CornerstoneTest.AreEqual(0, raised);
		}
	}

	[PresentationTestMethod]
	public void ButtonIsCancelWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var raised = 0;
			var target = new Button();
			var window = new Window { Content = target };
			window.Show();

			target.Click += (s, e) => ++raised;

			target.IsCancel = false;
			window.RaiseEvent(CreateKeyDownEvent(Key.Escape));
			CornerstoneTest.AreEqual(0, raised);

			target.IsCancel = true;
			window.RaiseEvent(CreateKeyDownEvent(Key.Escape));
			CornerstoneTest.AreEqual(1, raised);

			target.IsCancel = false;
			window.RaiseEvent(CreateKeyDownEvent(Key.Escape));
			CornerstoneTest.AreEqual(1, raised);

			target.IsCancel = true;
			window.RaiseEvent(CreateKeyDownEvent(Key.Escape));
			CornerstoneTest.AreEqual(2, raised);

			window.Content = null;
			window.RaiseEvent(CreateKeyDownEvent(Key.Escape, target));
			CornerstoneTest.AreEqual(2, raised);
		}
	}

	[PresentationTestMethod]
	public void ButtonIsDefaultShouldNotWorkWhenButtonIsNotEffectivelyVisible()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var raised = 0;
			var panel = new Panel();
			var target = new Button();
			panel.Children.Add(target);
			var window = new Window { Content = panel };
			window.Show();

			target.Click += (s, e) => ++raised;

			target.IsDefault = true;
			panel.IsVisible = false;
			window.RaiseEvent(CreateKeyDownEvent(Key.Enter));
			CornerstoneTest.AreEqual(0, raised);
		}
	}

	[PresentationTestMethod]
	public void ButtonIsDefaultWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var raised = 0;
			var target = new Button();
			var window = new Window { Content = target };
			window.Show();

			target.Click += (s, e) => ++raised;

			target.IsDefault = false;
			window.RaiseEvent(CreateKeyDownEvent(Key.Enter));
			CornerstoneTest.AreEqual(0, raised);

			target.IsDefault = true;
			window.RaiseEvent(CreateKeyDownEvent(Key.Enter));
			CornerstoneTest.AreEqual(1, raised);

			target.IsDefault = false;
			window.RaiseEvent(CreateKeyDownEvent(Key.Enter));
			CornerstoneTest.AreEqual(1, raised);

			target.IsDefault = true;
			window.RaiseEvent(CreateKeyDownEvent(Key.Enter));
			CornerstoneTest.AreEqual(2, raised);

			window.Content = null;

			// To check if handler was raised on the button, when it's detached, we need to pass it as a source manually.
			window.RaiseEvent(CreateKeyDownEvent(Key.Enter, target));
			CornerstoneTest.AreEqual(2, raised);
		}
	}

	[PresentationTestMethod]
	public void ButtonIsDisabledWhenBoundCommandDoesntExist()
	{
		var target = new Button
		{
			[!Button.CommandProperty] = new Binding("Command")
		};

		CornerstoneTest.IsTrue(target.IsEnabled);
		CornerstoneTest.IsFalse(target.IsEffectivelyEnabled);
	}

	[PresentationTestMethod]
	public void ButtonIsDisabledWhenBoundCommandIsRemoved()
	{
		var viewModel = new
		{
			Command = new TestCommand(true)
		};

		var target = new Button
		{
			DataContext = viewModel,
			[!Button.CommandProperty] = new Binding("Command")
		};

		CornerstoneTest.IsTrue(target.IsEnabled);
		CornerstoneTest.IsTrue(target.IsEffectivelyEnabled);

		target.DataContext = null;

		CornerstoneTest.IsTrue(target.IsEnabled);
		CornerstoneTest.IsFalse(target.IsEffectivelyEnabled);
	}

	[PresentationTestMethod]
	public void ButtonIsDisabledWhenCommandIsDisabled()
	{
		var command = new TestCommand(false);
		var target = new Button
		{
			Command = command
		};
		var root = new TestRoot { Child = target };

		CornerstoneTest.IsFalse(target.IsEffectivelyEnabled);
		command.IsEnabled = true;
		CornerstoneTest.IsTrue(target.IsEffectivelyEnabled);
		command.IsEnabled = false;
		CornerstoneTest.IsFalse(target.IsEffectivelyEnabled);
	}

	[PresentationTestMethod]
	public void ButtonIsDisabledWhenCommandIsEnabledButIsEnabledIsFalse()
	{
		var command = new TestCommand(true);
		var target = new Button
		{
			IsEnabled = false,
			Command = command
		};

		var root = new TestRoot { Child = target };

		CornerstoneTest.IsFalse(((IInputElement) target).IsEffectivelyEnabled);
	}

	[PresentationTestMethod]
	public void ButtonIsDisabledWhenDisabledBoundCommandIsAdded()
	{
		var viewModel = new
		{
			Command = new TestCommand(false)
		};

		var target = new Button
		{
			DataContext = new object(),
			[!Button.CommandProperty] = new Binding("Command")
		};

		CornerstoneTest.IsTrue(target.IsEnabled);
		CornerstoneTest.IsFalse(target.IsEffectivelyEnabled);

		target.DataContext = viewModel;

		CornerstoneTest.IsTrue(target.IsEnabled);
		CornerstoneTest.IsFalse(target.IsEffectivelyEnabled);
	}

	[PresentationTestMethod]
	public void ButtonIsEnabledWhenBoundCommandIsAdded()
	{
		var viewModel = new
		{
			Command = new TestCommand(true)
		};

		var target = new Button
		{
			DataContext = new object(),
			[!Button.CommandProperty] = new Binding("Command")
		};
		var root = new TestRoot { Child = target };

		Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, CancellationToken.None);

		CornerstoneTest.IsTrue(target.IsEnabled);
		CornerstoneTest.IsFalse(target.IsEffectivelyEnabled);

		target.DataContext = viewModel;

		CornerstoneTest.IsTrue(target.IsEnabled);
		CornerstoneTest.IsTrue(target.IsEffectivelyEnabled);
	}

	[PresentationTestMethod]
	public void ButtonLetterSpacingAffectsTextBlockChildInContentPresenter()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
		{
			var button = new Button
			{
				Content = "Test Text",
				LetterSpacing = 3.5
			};
			var root = new TestRoot { Child = button };

			button.ApplyTemplate();
			button.Presenter?.UpdateChild();

			// Find the TextBlock that was created by ContentPresenter
			var presenter = button.Presenter;
			CornerstoneTest.IsNotNull(presenter);

			var textBlock = presenter.Child as TextBlock;
			CornerstoneTest.IsNotNull(textBlock);

			// Verify LetterSpacing inherited to the TextBlock
			CornerstoneTest.AreEqual(3.5, textBlock.LetterSpacing);

			// Force a measure to create TextLayout
			textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

			// Verify the TextLayout actually has the LetterSpacing value
			var textLayout = textBlock.TextLayout;
			CornerstoneTest.IsNotNull(textLayout);
			CornerstoneTest.AreEqual(3.5, textLayout.LetterSpacing);
		}
	}

	[PresentationTestMethod]
	public void ButtonLetterSpacingCanBeSetAndRetrieved()
	{
		var button = new Button { LetterSpacing = 2.5 };
		CornerstoneTest.AreEqual(2.5, button.LetterSpacing);
	}

	[PresentationTestMethod]
	public void ButtonLetterSpacingCanBeSetToNegativeValue()
	{
		var button = new Button { LetterSpacing = -1.5 };
		CornerstoneTest.AreEqual(-1.5, button.LetterSpacing);
	}

	[PresentationTestMethod]
	public void ButtonLetterSpacingCanBeSetToZero()
	{
		var button = new Button { LetterSpacing = 5.0 };
		button.LetterSpacing = 0;
		CornerstoneTest.AreEqual(0, button.LetterSpacing);
	}

	[PresentationTestMethod]
	public void ButtonLetterSpacingDefaultValueIsZero()
	{
		var button = new Button();
		CornerstoneTest.AreEqual(0, button.LetterSpacing);
	}

	[PresentationTestMethod]
	public void ButtonLetterSpacingPropagatesToContentPresenter()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var button = new Button
			{
				Content = "Test",
				LetterSpacing = 3.0
			};
			var root = new TestRoot { Child = button };

			button.ApplyTemplate();

			var presenter = button.Presenter;
			CornerstoneTest.IsNotNull(presenter);
			CornerstoneTest.AreEqual(3.0, presenter.LetterSpacing);
		}
	}

	[PresentationTestMethod]
	public void ButtonLetterSpacingPropertyInheritsThroughVisualTree()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var button = new Button
			{
				Content = "Test",
				LetterSpacing = 2.0
			};
			var root = new TestRoot { Child = button };

			button.ApplyTemplate();
			button.Presenter?.UpdateChild();

			// Verify the property value is accessible on the presenter
			var presenter = button.Presenter;
			CornerstoneTest.IsNotNull(presenter);
			CornerstoneTest.AreEqual(2.0, presenter.LetterSpacing);
		}
	}

	[PresentationTestMethod]
	public void ButtonLetterSpacingUpdatesContentPresenterWhenChanged()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var button = new Button
			{
				Content = "Test",
				LetterSpacing = 1.0
			};
			var root = new TestRoot { Child = button };

			button.ApplyTemplate();
			var presenter = button.Presenter;
			CornerstoneTest.IsNotNull(presenter);

			button.LetterSpacing = 5.0;

			CornerstoneTest.AreEqual(5.0, presenter.LetterSpacing);
		}
	}

	[PresentationTestMethod]
	public void ButtonLetterSpacingWorksWithLargeValues()
	{
		var button = new Button { LetterSpacing = 100.0 };
		CornerstoneTest.AreEqual(100.0, button.LetterSpacing);
	}

	[PresentationTestMethod]
	public void ButtonRaisesClick()
	{
		var renderer = new StubHitTester();
		var pt = new Point(50, 50);
		renderer.SetHitTestPoint((p, r, f) => r.Bounds.Contains(p) ? new[] { r } : Array.Empty<Visual>());

		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var root = new Window { HitTesterOverride = renderer };
		var target = new Button
		{
			Width = 100,
			Height = 100,
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Left
		};
		root.Content = target;
		root.Show();

		var clicked = false;

		target.Click += (s, e) => clicked = true;

		RaisePointerEntered(target);
		RaisePointerMove(target, pt);
		RaisePointerPressed(target, 1, MouseButton.Left, pt);

		CornerstoneTest.AreEqual(_helper.Captured, target);

		RaisePointerReleased(target, MouseButton.Left, pt);

		CornerstoneTest.AreEqual(_helper.Captured, null);

		CornerstoneTest.IsTrue(clicked);
	}

	[PresentationTestMethod]
	public void ButtonSubscribesToCommandCanExecuteChangedWhenAddedToLogicalTree()
	{
		var command = new TestCommand(true);
		var target = new Button { Command = command };
		var root = new TestRoot { Child = target };

		CornerstoneTest.AreEqual(1, command.SubscriptionCount);
	}

	[PresentationTestMethod]
	public void ButtonUnpressedWhenDisabled()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new Button
			{
				// Disabling a control implies focus loss, and focus loss
				// has its own code path to un-press the button. So we have
				// to avoid hitting that path to get an accurate result.
				Focusable = false
			};

			var window = new Window { Content = target };
			window.Show();

			RaisePointerPressed(target, 1, MouseButton.Left, new Point(50, 50));

			CornerstoneTest.IsTrue(target.IsPressed);
			CornerstoneTest.IsFalse(target.IsFocused);
			target.IsEnabled = false;
			CornerstoneTest.IsFalse(target.IsPressed);
		}
	}

	[PresentationTestMethod]
	public void ButtonUnpressedWhenFocusLost()
	{
		using (UnitTestApplication.Start(TestServices.FocusableWindow))
		{
			var target = new Button();
			var other = new Button();

			var window = new Window { Content = new StackPanel { Children = { target, other } } };
			window.Show();

			RaisePointerPressed(target, 1, MouseButton.Left, new Point(50, 50));

			CornerstoneTest.IsTrue(target.IsPressed);
			CornerstoneTest.IsTrue(target.IsFocused);
			CornerstoneTest.IsTrue(other.Focus());
			CornerstoneTest.IsFalse(target.IsPressed);
		}
	}

	[PresentationTestMethod]
	public void ButtonUnsubscribesFromCommandCanExecuteChangedWhenRemovedFromLogicalTree()
	{
		var command = new TestCommand(true);
		var target = new Button { Command = command };
		var root = new TestRoot { Child = target };

		root.Child = null;
		CornerstoneTest.AreEqual(0, command.SubscriptionCount);
	}

	[PresentationTestMethod]
	public void ButtonWithRenderTransformRaisesClick()
	{
		var renderer = new StubHitTester();
		var pt = new Point(150, 50);
		renderer.SetHitTestPoint((p, r, f) => r.Bounds.Contains(p) ? new[] { r } : Array.Empty<Visual>());

		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var root = new Window { HitTesterOverride = renderer };
		var target = new Button
		{
			Width = 100,
			Height = 100,
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Left,
			RenderTransform = new TranslateTransform { X = 100, Y = 0 }
		};
		root.Content = target;
		root.Show();

		//actual bounds of button should  be 100,0,100,100 x -> translated 100 pixels
		//so mouse with x=150 coordinates should trigger click
		//button shouldn't count on bounds to calculate pointer is in the over or not, but
		//on avalonia event system, as renderer hit test will properly calculate whether to send
		//mouse over events to button based on rendered bounds
		//note: button also may have not rectangular shape and only renderer hit testing is reliable

		var clicked = false;

		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

		target.Click += (s, e) => clicked = true;

		RaisePointerEntered(target);
		RaisePointerMove(target, pt);
		RaisePointerPressed(target, 1, MouseButton.Left, pt);

		CornerstoneTest.AreEqual(_helper.Captured, target);

		RaisePointerReleased(target, MouseButton.Left, pt);

		CornerstoneTest.AreEqual(_helper.Captured, null);

		CornerstoneTest.IsTrue(clicked);
	}

	[PresentationTestMethod]
	public void RaisesClickWhenAccessKeyRaised()
	{
		var raised = 0;
		var kd = new KeyboardDevice();
		using var app = UnitTestApplication.Start(TestServices.StyledWindow
			.With(
				accessKeyHandler: () => new AccessKeyHandler(),
				keyboardDevice: () => kd)
		);

		var impl = CreateMockTopLevelImpl();
		var command = new TestCommand(p => p is bool value && value, _ => raised++);

		Button target;
		var root = new TestTopLevel(impl)
		{
			Template = CreateTemplate(),
			Content = target = new Button
			{
				Content = "_A",
				Command = command,
				Template = new FuncControlTemplate<Button>((parent, scope) =>
				{
					return new ContentPresenter
					{
						Name = "PART_ContentPresenter",
						[~ContentPresenter.ContentProperty] = new TemplateBinding(Button.ContentProperty),
						[~ContentPresenter.ContentTemplateProperty] = new TemplateBinding(Button.ContentProperty),
						RecognizesAccessKey = true
					}.RegisterInNameScope(scope);
				})
			}
		};

		root.ApplyTemplate();
		root.Presenter!.UpdateChild();
		target.ApplyTemplate();
		target.Presenter!.UpdateChild();
		kd.SetFocusedElement(target, NavigationMethod.Unspecified, KeyModifiers.None);

		Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, CancellationToken.None);

		const Key accessKey = Key.A;
		const string accessKeySymbol = "a";
		target.CommandParameter = true;

		RaiseAccessKey(root, accessKey, accessKeySymbol);

		CornerstoneTest.AreEqual(1, raised);

		target.CommandParameter = false;

		RaiseAccessKey(root, accessKey, accessKeySymbol);

		CornerstoneTest.AreEqual(1, raised);

		static FuncControlTemplate<TestTopLevel> CreateTemplate()
		{
			return new FuncControlTemplate<TestTopLevel>((x, scope) =>
				new ContentPresenter
				{
					Name = "PART_ContentPresenter",
					[~ContentPresenter.ContentProperty] = new TemplateBinding(ContentControl.ContentProperty),
					[~ContentPresenter.ContentTemplateProperty] = new TemplateBinding(ContentControl.ContentTemplateProperty)
				}.RegisterInNameScope(scope));
		}

		static StubWindowImpl CreateMockTopLevelImpl(bool setupProperties = false)
		{
			var topLevel = new StubWindowImpl();
			if (setupProperties)
			{
			}
			return topLevel;
		}

		static void RaiseAccessKey(IInputElement target, Key accessKey, string keySymbol)
		{
			KeyDown(target, Key.LeftAlt);
			KeyDown(target, accessKey, keySymbol, KeyModifiers.Alt);
			KeyUp(target, accessKey, keySymbol, KeyModifiers.Alt);
			KeyUp(target, Key.LeftAlt, null);
		}

		static void KeyDown(IInputElement target, Key key, string keySymbol = null, KeyModifiers modifiers = KeyModifiers.None)
		{
			target.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = key,
				KeySymbol = keySymbol,
				KeyModifiers = modifiers
			});
		}

		static void KeyUp(IInputElement target, Key key, string keySymbol = null, KeyModifiers modifiers = KeyModifiers.None)
		{
			target.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyUpEvent,
				Key = key,
				KeySymbol = keySymbol,
				KeyModifiers = modifiers
			});
		}
	}

	[PresentationTestMethod]
	public void ClickModePressRaisesClickOnPointerPressed()
	{
		var renderer = new StubHitTester();
		var pt = new Point(50, 50);
		renderer.SetHitTestPoint((p, r, f) => r.Bounds.Contains(p) ? new[] { r } : Array.Empty<Visual>());

		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var root = new Window { HitTesterOverride = renderer };
		var target = new Button
		{
			Width = 100,
			Height = 100,
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Left,
			ClickMode = ClickMode.Press
		};
		root.Content = target;
		root.Show();

		var clicks = 0;
		target.Click += (_, _) => clicks++;

		RaisePointerPressed(target, 1, MouseButton.Left, pt);

		CornerstoneTest.AreEqual(1, clicks);
		CornerstoneTest.IsTrue(target.IsPressed);

		RaisePointerReleased(target, MouseButton.Left, pt);

		CornerstoneTest.AreEqual(1, clicks);
		CornerstoneTest.IsFalse(target.IsPressed);
	}

	[PresentationTestMethod]
	public void EscapeClosesButtonFlyout()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var target = new Button();
		var flyout = new Flyout { Content = new Border { Width = 10, Height = 10 } };
		target.Flyout = flyout;
		var window = new Window { Content = target };
		window.Show();

		((IClickableControl)target).RaiseClick();
		CornerstoneTest.IsTrue(flyout.IsOpen);

		target.RaiseEvent(CreateKeyDownEvent(Key.Escape));

		CornerstoneTest.IsFalse(flyout.IsOpen);
	}

	[PresentationTestMethod]
	public void FocusedButtonEnterRaisesClick()
	{
		using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

		var target = new Button();
		var window = new Window { Content = target };
		window.Show();
		target.Focus();

		var clicks = 0;
		target.Click += (_, _) => clicks++;
		target.RaiseEvent(CreateKeyDownEvent(Key.Enter));

		CornerstoneTest.AreEqual(1, clicks);
		CornerstoneTest.IsFalse(target.IsDefault);
	}

	[PresentationTestMethod]
	public void FocusedButtonSpaceClicksOnKeyUp()
	{
		using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

		var target = new Button();
		var window = new Window { Content = target };
		window.Show();
		target.Focus();

		var clicks = 0;
		target.Click += (_, _) => clicks++;

		target.RaiseEvent(CreateKeyDownEvent(Key.Space));
		CornerstoneTest.IsTrue(target.IsPressed);
		CornerstoneTest.AreEqual(0, clicks);

		target.RaiseEvent(CreateKeyUpEvent(Key.Space));
		CornerstoneTest.IsFalse(target.IsPressed);
		CornerstoneTest.AreEqual(1, clicks);
	}

	[PresentationTestMethod]
	public void FocusedButtonSpacePressClicksOnKeyDown()
	{
		using var _ = UnitTestApplication.Start(TestServices.FocusableWindow);

		var target = new Button { ClickMode = ClickMode.Press };
		var window = new Window { Content = target };
		window.Show();
		target.Focus();

		var clicks = 0;
		target.Click += (_, _) => clicks++;

		target.RaiseEvent(CreateKeyDownEvent(Key.Space));

		CornerstoneTest.IsTrue(target.IsPressed);
		CornerstoneTest.AreEqual(1, clicks);
	}

	[PresentationTestMethod]
	public void HandledClickDoesNotExecuteCommand()
	{
		var executed = false;
		var target = new Button
		{
			Command = new TestCommand(_ => true, _ => executed = true)
		};
		target.Click += (_, e) => e.Handled = true;

		((IClickableControl)target).RaiseClick();

		CornerstoneTest.IsFalse(executed);
	}

	[PresentationTestMethod]
	public void RaisesClickOpensAndClosesFlyout()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var target = new Button();
		var flyout = new Flyout { Content = new Border { Width = 10, Height = 10 } };
		target.Flyout = flyout;
		var window = new Window { Content = target };
		window.Show();

		((IClickableControl)target).RaiseClick();
		CornerstoneTest.IsTrue(flyout.IsOpen);

		((IClickableControl)target).RaiseClick();
		CornerstoneTest.IsFalse(flyout.IsOpen);
	}

	[PresentationTestMethod]
	public void RightButtonDoesNotPress()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var target = new Button();
		var window = new Window { Content = target };
		window.Show();

		RaisePointerPressed(target, 1, MouseButton.Right, new Point(50, 50));

		CornerstoneTest.IsFalse(target.IsPressed);
	}

	[PresentationTestMethod]
	public void ShouldNotFireClickEventOnSpaceKeyWhenItIsNotFocus()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var raised = 0;
			var target = new TextBox();
			var button = new Button
			{
				Content = target
			};

			var window = new Window { Content = button };
			window.Show();

			button.Click += (s, e) => ++raised;
			target.Focus();
			target.RaiseEvent(CreateKeyDownEvent(Key.Space));
			target.RaiseEvent(CreateKeyUpEvent(Key.Space));
			CornerstoneTest.AreEqual(0, raised);
		}
	}

	private KeyEventArgs CreateKeyDownEvent(Key key, Interactive source = null)
	{
		return new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, Source = source };
	}

	private KeyEventArgs CreateKeyUpEvent(Key key, Interactive source = null)
	{
		return new KeyEventArgs { RoutedEvent = InputElement.KeyUpEvent, Key = key, Source = source };
	}

	private void RaisePointerEntered(Button button)
	{
		_helper.Enter(button);
	}

	private void RaisePointerExited(Button button)
	{
		_helper.Leave(button);
	}

	private void RaisePointerMove(Button button, Point pos)
	{
		_helper.Move(button, pos);
	}

	private void RaisePointerPressed(Button button, int clickCount, MouseButton mouseButton, Point position)
	{
		_helper.Down(button, mouseButton, position, clickCount: clickCount);
	}

	private void RaisePointerReleased(Button button, MouseButton mouseButton, Point pt)
	{
		_helper.Up(button, mouseButton, pt);
	}

	#endregion

	#region Classes

	private class TestTopLevel(ITopLevelImpl impl) : TopLevel(impl)
	{
	}

	#endregion
}