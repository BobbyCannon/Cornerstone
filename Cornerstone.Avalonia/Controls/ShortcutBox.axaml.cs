#region References

using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Cornerstone.Input;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.Controls;

[SourceReflection]
public sealed partial class ShortcutBox : CornerstoneTemplatedControl
{
	#region Fields

	private TextBlock _display;
	private bool _isWaitingForSecondKey;

	#endregion

	#region Constructors

	public ShortcutBox()
	{
		Focusable = true;
	}

	#endregion

	#region Properties

	[StyledProperty]
	public partial ShortcutBinding Binding { get; set; }

	[StyledProperty]
	public partial Gamepad Gamepad { get; set; }

	[StyledProperty]
	public partial HorizontalAlignment HorizontalContentAlignment { get; set; }

	[StyledProperty]
	public partial object InnerRightContent { get; set; }

	[StyledProperty]
	public partial bool IsTriggered { get; set; }

	[StyledProperty]
	public partial string Label { get; set; }

	[StyledProperty]
	public partial string Watermark { get; set; }

	#endregion

	#region Methods

	public void Reset()
	{
		_isWaitingForSecondKey = false;
		Binding?.Reset();
		RefreshDisplay();
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);
		Binding ??= new ShortcutBinding();
		_display = e.NameScope.Get<TextBlock>("PART_Display");
		RefreshDisplay();
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		if (Binding == null)
		{
			base.OnKeyDown(e);
			return;
		}

		// Ignore modifier-only keys when not in chord mode (optional, but cleaner)
		if (IsModifierOnly(e.Key) && !_isWaitingForSecondKey)
		{
			base.OnKeyDown(e);
			return;
		}

		if (_isWaitingForSecondKey)
		{
			// This is the second key of the chord
			Binding.UpdateChordBinding(e.Key, e.KeyModifiers);
			_isWaitingForSecondKey = false;
		}
		else
		{
			// First key
			if (e.Key == Key.Escape)
			{
				Reset();
				e.Handled = true;
				return;
			}

			Binding.UpdateBinding(e.Key, e.KeyModifiers);

			// If user wants a chord, they can press another key quickly
			// For now we always allow chord by waiting for second key
			// You can make this optional later with a property like "AllowChords"

			_isWaitingForSecondKey = true;

		}

		// Show first part + "..." while waiting
		RefreshDisplay();
		e.Handled = true;

		base.OnKeyDown(e);
	}

	protected override void OnLostFocus(FocusChangedEventArgs e)
	{
		// Cancel chord input if focus is lost
		if (_isWaitingForSecondKey)
		{
			_isWaitingForSecondKey = false;
			RefreshDisplay();
		}
		base.OnLostFocus(e);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == BindingProperty)
		{
			if (change.OldValue is ShortcutBinding oldValue)
			{
				oldValue.PropertyChanged -= BindingOnPropertyChanged;
			}
			if (change.NewValue is ShortcutBinding newValue)
			{
				newValue.PropertyChanged += BindingOnPropertyChanged;
			}
			RefreshDisplay();
		}

		if (change.Property == GamepadProperty)
		{
			if (change.OldValue is Gamepad oldValue)
			{
				oldValue.ButtonChanged -= GamepadOnButtonChanged;
			}
			if (change.NewValue is Gamepad newValue)
			{
				newValue.ButtonChanged += GamepadOnButtonChanged;
			}
		}

		base.OnPropertyChanged(change);
	}

	private void BindingOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		RefreshDisplay();
	}

	private void GamepadOnButtonChanged(object sender, Gamepad.ButtonState e)
	{
		if (!IsFocused)
		{
			return;
		}

		CornerstoneApplication.CornerstoneDispatcher.Dispatch(() =>
		{
			Binding.GamepadButton = e.Button;
			RefreshDisplay();
		});
	}

	private static bool IsModifierOnly(Key key)
	{
		return key is Key.LeftCtrl or Key.RightCtrl or
			Key.LeftShift or Key.RightShift or
			Key.LeftAlt or Key.RightAlt or
			Key.LWin or Key.RWin;
	}

	private void RefreshDisplay()
	{
		if ((_display == null) || (Binding == null))
		{
			return;
		}

		if (_isWaitingForSecondKey)
		{
			// Visual feedback while waiting for second key of chord
			var firstPart = Binding.ToString(); // reuse existing method
			_display.Text = string.IsNullOrEmpty(firstPart)
				? "Press second key..."
				: $"{firstPart}, ...";
		}
		else
		{
			_display.Text = Binding.ToString(); // Uses updated ToString() with chords
		}
	}

	#endregion
}