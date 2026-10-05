#region References

using System;
using System.Windows.Input;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;
using Cornerstone.Presentation.LogicalTree;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ICommandSource = Cornerstone.Presentation.Input.ICommandSource;
using KeyGesture = Cornerstone.Presentation.Input.KeyGesture;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

internal class HotKeyedTextBox : TextBox, ICommandSource
{
	#region Fields

	public static readonly StyledProperty<KeyGesture> HotKeyProperty =
		HotKeyManager.HotKeyProperty.AddOwner<HotKeyedTextBox>();

	private readonly DelegateCommand _command;

	private KeyGesture _hotkey;

	#endregion

	#region Constructors

	public HotKeyedTextBox()
	{
		_command = new DelegateCommand(() => Focus());
	}

	#endregion

	#region Properties

	public ICommand Command => _command;

	public object CommandParameter => null;

	public KeyGesture HotKey
	{
		get => GetValue(HotKeyProperty);
		set => SetValue(HotKeyProperty, value);
	}

	protected override Type StyleKeyOverride => typeof(TextBox);

	#endregion

	#region Methods

	public void CanExecuteChanged(object sender, EventArgs e)
	{
	}

	protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		if (_hotkey != null)
		{
			SetValue(HotKeyProperty, _hotkey);
		}

		base.OnAttachedToLogicalTree(e);
	}

	protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
	{
		if (HotKey != null)
		{
			_hotkey = HotKey;
			SetValue(HotKeyProperty, null);
		}

		base.OnDetachedFromLogicalTree(e);
	}

	#endregion

	#region Classes

	private class DelegateCommand : ICommand
	{
		#region Fields

		private readonly Action _action;

		#endregion

		#region Constructors

		public DelegateCommand(Action action)
		{
			_action = action;
		}

		#endregion

		#region Methods

		public bool CanExecute(object parameter)
		{
			return true;
		}

		public void Execute(object parameter)
		{
			_action();
		}

		#endregion

		#region Events

		public event EventHandler CanExecuteChanged
		{
			add { }
			remove { }
		}

		#endregion
	}

	#endregion
}

[TestClass]
public class HotKeyedControlsTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void HotKeyedTextBoxFocusPerformedOnHotkey()
	{
		using var _ = CreateServicesWithFocus();

		var keyboardDevice = new KeyboardDevice();
		var hotKeyedTextBox = new HotKeyedTextBox { HotKey = new KeyGesture(Key.F, KeyModifiers.Control) };
		var root = PreparedWindow();
		root.Content = hotKeyedTextBox;
		root.Show();

		CornerstoneTest.IsFalse(hotKeyedTextBox.IsFocused);

		keyboardDevice.ProcessRawEvent(
			new RawKeyEventArgs(
				keyboardDevice,
				0,
				root.InputRoot,
				RawKeyEventType.KeyDown,
				Key.F,
				RawInputModifiers.Control,
				PhysicalKey.F,
				"f"));

		CornerstoneTest.IsTrue(hotKeyedTextBox.IsFocused);
	}

	private static IDisposable CreateServicesWithFocus()
	{
		return UnitTestApplication.Start(
			TestServices.StyledWindow.With(
				windowingPlatform: new MockWindowingPlatform(
					null,
					window => MockWindowingPlatform.CreatePopupMock(window)),
				keyboardDevice: () => new KeyboardDevice()));
	}

	private static Window PreparedWindow(object content = null)
	{
		var platform = PresentationLocator.Current.GetRequiredService<IWindowingPlatform>();
		var windowImpl = platform.CreateWindow();
		var w = new Window(windowImpl) { Content = content };
		w.ApplyTemplate();
		return w;
	}

	#endregion
}