#region References

using System;
using System.Windows.Input;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Utils;

[TestClass]
public class HotKeyManagerTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(nameof(Button))]
	[DataRow(nameof(MenuItem))]
	public void HotKeyManagerShouldDoNotExecutedWhenIsEnabledFalse(string factoryName)
	{
		using (PresentationLocator.EnterScope())
		{
			var target = new KeyboardDevice();
			var isExecuted = false;
			PresentationLocator.CurrentMutable
				.Bind<IWindowingPlatform>().ToConstant(new MockWindowingPlatform());

			var gesture = new KeyGesture(Key.A, KeyModifiers.Control);

			var action = new Action<object>(parameter => { isExecuted = true; });

			var root = new Window();
			var element = (InputElement) CreateElement(factoryName, true, 0, action, root);
			CornerstoneTest.IsNotNull(element);

			element.IsEnabled = false;

			root.Template = CreateWindowTemplate();
			root.ApplyTemplate();
			root.Presenter!.ApplyTemplate();

			HotKeyManager.SetHotKey(element, gesture);

			target.ProcessRawEvent(new RawKeyEventArgs(target,
				0,
				root.InputRoot,
				RawKeyEventType.KeyDown,
				Key.A,
				RawInputModifiers.Control,
				PhysicalKey.A,
				"a"));

			CornerstoneTest.IsTrue(!isExecuted, $"{factoryName} Execution raised when IsEnabled is false.");
		}
	}

	[PresentationTestMethod]
	[DataRow(nameof(Button))]
	[DataRow(nameof(MenuItem))]
	public void HotKeyManagerShouldInvokeEventClickWhenCommandIsNull(string factoryName)
	{
		using (PresentationLocator.EnterScope())
		{
			var target = new KeyboardDevice();
			var clickExecutedCount = 0;
			PresentationLocator.CurrentMutable
				.Bind<IWindowingPlatform>().ToConstant(new MockWindowingPlatform());

			var gesture = new KeyGesture(Key.A, KeyModifiers.Control);

			void ClickableClick(object sender, RoutedEventArgs e)
			{
				clickExecutedCount++;
			}

			var root = new Window();
			var element = (InputElement) CreateElement(factoryName, false, 0, null, root);
			if (element is IClickableControl clickable)
			{
				clickable.Click += ClickableClick;
			}

			root.Template = CreateWindowTemplate();
			root.ApplyTemplate();
			root.Presenter!.ApplyTemplate();

			HotKeyManager.SetHotKey(element, gesture);

			target.ProcessRawEvent(new RawKeyEventArgs(target,
				0,
				root.InputRoot,
				RawKeyEventType.KeyDown,
				Key.A,
				RawInputModifiers.Control,
				PhysicalKey.A,
				"a"));

			element.IsEnabled = false;

			target.ProcessRawEvent(new RawKeyEventArgs(target,
				0,
				root.InputRoot,
				RawKeyEventType.KeyDown,
				Key.A,
				RawInputModifiers.Control,
				PhysicalKey.A,
				"a"));

			CornerstoneTest.IsTrue(clickExecutedCount == 1, $"{factoryName} Execution raised when IsEnabled is false.");
		}
	}

	[PresentationTestMethod]
	[DataRow(nameof(Button))]
	[DataRow(nameof(MenuItem))]
	public void HotKeyManagerShouldNotInvokeEventClickWhenCommandIsNotNull(string factoryName)
	{
		using (PresentationLocator.EnterScope())
		{
			var target = new KeyboardDevice();
			var clickExecutedCount = 0;
			var commandExecutedCount = 0;
			PresentationLocator.CurrentMutable
				.Bind<IWindowingPlatform>().ToConstant(new MockWindowingPlatform());

			var gesture = new KeyGesture(Key.A, KeyModifiers.Control);

			void DoExecute(object parameter)
			{
				commandExecutedCount++;
			}

			void ClickableClick(object sender, RoutedEventArgs e)
			{
				clickExecutedCount++;
			}

			var root = new Window();
			var element = (InputElement) CreateElement(factoryName, true, 0, DoExecute, root);
			if (element is IClickableControl clickable)
			{
				clickable.Click += ClickableClick;
			}

			root.Template = CreateWindowTemplate();
			root.ApplyTemplate();
			root.Presenter!.ApplyTemplate();

			HotKeyManager.SetHotKey(element, gesture);

			target.ProcessRawEvent(new RawKeyEventArgs(target,
				0,
				root.InputRoot,
				RawKeyEventType.KeyDown,
				Key.A,
				RawInputModifiers.Control,
				PhysicalKey.A,
				"a"));

			element.IsEnabled = false;

			target.ProcessRawEvent(new RawKeyEventArgs(target,
				0,
				root.InputRoot,
				RawKeyEventType.KeyDown,
				Key.A,
				RawInputModifiers.Control,
				PhysicalKey.A,
				"a"));

			CornerstoneTest.IsTrue(commandExecutedCount == 1, $"{factoryName} Execution raised when IsEnabled is false.");
			CornerstoneTest.IsTrue(clickExecutedCount == 0, $"{factoryName} Execution raised event Click.");
		}
	}

	[PresentationTestMethod]
	public void HotKeyManagerShouldRegisterAndUnregisterKeyBinding()
	{
		using (PresentationLocator.EnterScope())
		{
			PresentationLocator.CurrentMutable
				.Bind<IWindowingPlatform>().ToConstant(new MockWindowingPlatform());

			var gesture1 = new KeyGesture(Key.A, KeyModifiers.Control);
			var gesture2 = new KeyGesture(Key.B, KeyModifiers.Control);

			var tl = new Window();
			var button = new Button();
			tl.Content = button;
			tl.Template = CreateWindowTemplate();
			tl.ApplyTemplate();
			tl.Presenter!.ApplyTemplate();

			HotKeyManager.SetHotKey(button, gesture1);

			CornerstoneTest.AreEqual(gesture1, tl.KeyBindings[0].Gesture);

			HotKeyManager.SetHotKey(button, gesture2);
			CornerstoneTest.AreEqual(gesture2, tl.KeyBindings[0].Gesture);

			tl.Content = null;
			tl.Presenter.ApplyTemplate();

			CornerstoneTest.Empty(tl.KeyBindings);

			tl.Content = button;
			tl.Presenter.ApplyTemplate();

			CornerstoneTest.AreEqual(gesture2, tl.KeyBindings[0].Gesture);

			HotKeyManager.SetHotKey(button, null);
			CornerstoneTest.Empty(tl.KeyBindings);
		}
	}

	[PresentationTestMethod]
	[DataRow(nameof(Button))]
	[DataRow(nameof(MenuItem))]
	public void HotKeyManagerShouldUseCommandParameter(string factoryName)
	{
		using (PresentationLocator.EnterScope())
		{
			var target = new KeyboardDevice();
			var commandResult = 0;
			var expectedParameter = 1;
			PresentationLocator.CurrentMutable
				.Bind<IWindowingPlatform>().ToConstant(new MockWindowingPlatform());

			var gesture = new KeyGesture(Key.A, KeyModifiers.Control);

			var action = new Action<object>(parameter =>
			{
				if (parameter is int value)
				{
					commandResult = value;
				}
			});

			var root = new Window();
			var element = CreateElement(factoryName, true, expectedParameter, action, root);

			root.Template = CreateWindowTemplate();
			root.ApplyTemplate();
			root.Presenter!.ApplyTemplate();

			HotKeyManager.SetHotKey(element, gesture);

			target.ProcessRawEvent(new RawKeyEventArgs(target,
				0,
				root.InputRoot,
				RawKeyEventType.KeyDown,
				Key.A,
				RawInputModifiers.Control,
				PhysicalKey.A,
				"a"));

			CornerstoneTest.IsTrue(expectedParameter == commandResult, $"{factoryName} HotKey did not carry the CommandParameter.");
		}
	}

	private static PresentationObject CreateElement(
		string factoryName,
		bool withCommand,
		int expectedParameter,
		Action<object> action,
		Window root)
	{
		return (factoryName, withCommand) switch
		{
			(nameof(Button), true) => MakeButton(expectedParameter, action, root),
			(nameof(Button), false) => MakeButtonWithoutCommand(expectedParameter, action, root),
			(nameof(MenuItem), true) => MakeMenu(expectedParameter, action, root),
			(nameof(MenuItem), false) => MakeMenuWithoutCommand(expectedParameter, action, root),
			_ => throw new ArgumentOutOfRangeException(nameof(factoryName), factoryName, null)
		};
	}

	private static FuncControlTemplate CreateWindowTemplate()
	{
		return new FuncControlTemplate<Window>((parent, scope) =>
		{
			return new ContentPresenter
			{
				Name = "PART_ContentPresenter",
				[~ContentPresenter.ContentProperty] = parent[~ContentControl.ContentProperty]
			}.RegisterInNameScope(scope);
		});
	}

	private static PresentationObject MakeButton(int expectedParameter, Action<object> action, Window root)
	{
		var button = new Button
		{
			Command = new Command(action),
			CommandParameter = expectedParameter
		};

		root.Content = button;
		return button;
	}

	private static PresentationObject MakeButtonWithoutCommand(int expectedParameter, Action<object> action, Window root)
	{
		var button = new Button();

		root.Content = button;
		return button;
	}

	private static PresentationObject MakeMenu(int expectedParameter, Action<object> action, Window root)
	{
		var menuitem = new MenuItem
		{
			Command = new Command(action),
			CommandParameter = expectedParameter
		};
		var rootMenu = new Menu();

		rootMenu.Items.Add(menuitem);

		root.Content = rootMenu;
		return menuitem;
	}

	private static PresentationObject MakeMenuWithoutCommand(int expectedParameter, Action<object> action, Window root)
	{
		var menuitem = new MenuItem();
		var rootMenu = new Menu();

		rootMenu.Items.Add(menuitem);

		root.Content = rootMenu;
		return menuitem;
	}

	#endregion

	#region Classes

	private class Command : ICommand
	{
		#region Fields

		private readonly Action<object> _execute;

		#endregion

		#region Constructors

		public Command(Action<object> execute)
		{
			_execute = execute;
		}

		#endregion

		#region Methods

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			_execute?.Invoke(parameter);
		}

		#endregion

		#region Events

		#pragma warning disable 67 // Event not used
		public event EventHandler CanExecuteChanged;
		#pragma warning restore 67 // Event not used

		#endregion
	}

	#endregion
}