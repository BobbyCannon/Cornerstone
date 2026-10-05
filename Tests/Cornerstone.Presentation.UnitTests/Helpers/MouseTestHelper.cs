#region References

using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.VisualTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

[TestClass]
public class MouseTestHelper(PointerType pointerType = PointerType.Mouse)
{
	#region Fields

	private ulong _nextStamp = 1;
	private readonly Pointer _pointer = new(Pointer.GetNextFreeId(), pointerType, true);

	private MouseButton _pressedButton;

	private RawInputModifiers _pressedButtons;

	#endregion

	#region Properties

	public IInputElement Captured => _pointer.Captured;

	#endregion

	#region Methods

	public void Click(Interactive target, MouseButton button = MouseButton.Left, Point? position = null,
		KeyModifiers modifiers = default)
	{
		Click(target, target, button, position, modifiers);
	}

	public void Click(Interactive target, Interactive source, MouseButton button = MouseButton.Left,
		Point? position = null, KeyModifiers modifiers = default)
	{
		Down(target, source, button, position, modifiers);
		var captured = _pointer.Captured as Interactive ?? source;
		Up(captured, captured, button, position, modifiers);
	}

	public void DoubleClick(Interactive target, MouseButton button = MouseButton.Left, Point? position = null,
		KeyModifiers modifiers = default)
	{
		DoubleClick(target, target, button, position, modifiers);
	}

	public void DoubleClick(Interactive target, Interactive source, MouseButton button = MouseButton.Left,
		Point? position = null, KeyModifiers modifiers = default)
	{
		Down(target, source, button, position, modifiers, 1);
		var captured = _pointer.Captured as Interactive ?? source;
		Up(captured, captured, button, position, modifiers);
		Down(target, source, button, position, modifiers, 2);
	}

	public void Down(Interactive target, MouseButton mouseButton = MouseButton.Left, Point? position = null,
		KeyModifiers modifiers = default, int clickCount = 1)
	{
		Down(target, target, mouseButton, position, modifiers, clickCount);
	}

	public void Down(Interactive target, Interactive source, MouseButton mouseButton = MouseButton.Left,
		Point? position = null, KeyModifiers modifiers = default, int clickCount = 1)
	{
		_pressedButtons |= Convert(mouseButton);
		var props = new PointerPointProperties(_pressedButtons,
			mouseButton == MouseButton.Left ? PointerUpdateKind.LeftButtonPressed
			: mouseButton == MouseButton.Middle ? PointerUpdateKind.MiddleButtonPressed
			: mouseButton == MouseButton.Right ? PointerUpdateKind.RightButtonPressed : PointerUpdateKind.Other
		);
		if (ButtonCount(props) > 1)
		{
			Move(target, source, position ?? default);
		}
		else
		{
			_pressedButton = mouseButton;
			_pointer.Capture((IInputElement) target);
			source.RaiseEvent(new PointerPressedEventArgs(source, _pointer, GetRoot(target), position ?? MidpointRelativeToRoot(target), Timestamp(), props,
				modifiers, clickCount));
		}
	}

	public void Enter(Interactive target)
	{
		target.RaiseEvent(new PointerEventArgs(InputElement.PointerEnteredEvent, target, _pointer, target, default,
			Timestamp(), new PointerPointProperties(_pressedButtons, PointerUpdateKind.Other), KeyModifiers.None));
	}

	public void Leave(Interactive target)
	{
		target.RaiseEvent(new PointerEventArgs(InputElement.PointerExitedEvent, target, _pointer, target, default,
			Timestamp(), new PointerPointProperties(_pressedButtons, PointerUpdateKind.Other), KeyModifiers.None));
	}

	public void Move(Interactive target, in Point position, KeyModifiers modifiers = default)
	{
		Move(target, target, position, modifiers);
	}

	public void Move(Interactive target, Interactive source, in Point position, KeyModifiers modifiers = default)
	{
		var e = new PointerEventArgs(InputElement.PointerMovedEvent, source, _pointer, GetRoot(target), position,
			Timestamp(), new PointerPointProperties(_pressedButtons, PointerUpdateKind.Other), modifiers);

		if (_pointer.CapturedGestureRecognizer != null)
		{
			_pointer.CapturedGestureRecognizer.PointerMovedInternal(e);
		}
		else
		{
			target.RaiseEvent(e);
		}
	}

	public void Up(Interactive target, MouseButton mouseButton = MouseButton.Left, Point? position = null,
		KeyModifiers modifiers = default)
	{
		Up(target, target, mouseButton, position, modifiers);
	}

	public void Up(Interactive target, Interactive source, MouseButton mouseButton = MouseButton.Left,
		Point? position = null, KeyModifiers modifiers = default)
	{
		var conv = Convert(mouseButton);
		_pressedButtons = (_pressedButtons | conv) ^ conv;
		var props = new PointerPointProperties(_pressedButtons,
			mouseButton == MouseButton.Left ? PointerUpdateKind.LeftButtonReleased
			: mouseButton == MouseButton.Middle ? PointerUpdateKind.MiddleButtonReleased
			: mouseButton == MouseButton.Right ? PointerUpdateKind.RightButtonReleased : PointerUpdateKind.Other
		);
		if (ButtonCount(props) == 0)
		{
			var e = new PointerReleasedEventArgs(source, _pointer, GetRoot(target), position ?? MidpointRelativeToRoot(target),
				Timestamp(), props, modifiers, _pressedButton);

			if (_pointer.CapturedGestureRecognizer != null)
			{
				_pointer.CapturedGestureRecognizer.PointerReleasedInternal(e);
			}
			else
			{
				target.RaiseEvent(e);
			}

			_pointer.CaptureLost(CaptureSource.Explicit);
		}
		else
		{
			Move(target, source, position ?? default);
		}
	}

	private int ButtonCount(PointerPointProperties props)
	{
		var rv = 0;
		if (props.IsLeftButtonPressed)
		{
			rv++;
		}
		if (props.IsMiddleButtonPressed)
		{
			rv++;
		}
		if (props.IsRightButtonPressed)
		{
			rv++;
		}
		return rv;
	}

	private RawInputModifiers Convert(MouseButton mouseButton)
	{
		return mouseButton switch
		{
			MouseButton.Left => RawInputModifiers.LeftMouseButton,
			MouseButton.Right => RawInputModifiers.RightMouseButton,
			MouseButton.Middle => RawInputModifiers.MiddleMouseButton,
			_ => RawInputModifiers.None
		};
	}

	private Visual GetRoot(Interactive source)
	{
		return source?.GetVisualRoot() ?? source;
	}

	private Point MidpointRelativeToRoot(Interactive element)
	{
		var root = GetRoot(element);
		return element.TranslatePoint(new(element.Bounds.Width / 2, element.Bounds.Height / 2), root).GetValueOrDefault();
	}

	private ulong Timestamp()
	{
		return _nextStamp++;
	}

	#endregion
}