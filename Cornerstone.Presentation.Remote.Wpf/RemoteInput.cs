#region References

using System.Collections.Generic;
using System.Windows.Input;
using Cornerstone.Presentation.Remote.Protocol.Input;
using CsMouseButton = Cornerstone.Presentation.Remote.Protocol.Input.MouseButton;
using WpfMouseButton = System.Windows.Input.MouseButton;

#endregion

namespace Cornerstone.Presentation.Remote.Wpf;

/// <summary>
/// Maps WPF pointer state onto remote protocol input messages.
/// </summary>
public static class RemoteInput
{
	#region Methods

	public static CsMouseButton GetButton(WpfMouseButton button)
	{
		switch (button)
		{
			case WpfMouseButton.Left:
				return CsMouseButton.Left;
			case WpfMouseButton.Middle:
				return CsMouseButton.Middle;
			case WpfMouseButton.Right:
				return CsMouseButton.Right;
			default:
				return CsMouseButton.None;
		}
	}

	public static InputModifiers[] GetModifiers(MouseEventArgs e)
	{
		var result = new List<InputModifiers>();
		if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)
		{
			result.Add(InputModifiers.Alt);
		}
		if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
		{
			result.Add(InputModifiers.Control);
		}
		if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
		{
			result.Add(InputModifiers.Shift);
		}
		if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0)
		{
			result.Add(InputModifiers.Windows);
		}
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			result.Add(InputModifiers.LeftMouseButton);
		}
		if (e.RightButton == MouseButtonState.Pressed)
		{
			result.Add(InputModifiers.RightMouseButton);
		}
		if (e.MiddleButton == MouseButtonState.Pressed)
		{
			result.Add(InputModifiers.MiddleMouseButton);
		}

		return result.ToArray();
	}

	#endregion
}