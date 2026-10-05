#region References

using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

[TestClass]
public class TouchTestHelper
{
	#region Fields

	private int _clickCount;
	private ulong _nextStamp = 1;
	private readonly Pointer _pointer = new(Pointer.GetNextFreeId(), PointerType.Touch, true);

	#endregion

	#region Properties

	public IInputElement Captured => _pointer.Captured;

	#endregion

	#region Methods

	public void Cancel()
	{
		_pointer.CaptureLost(CaptureSource.Platform);
	}

	public void Down(Interactive target, Point position = default, KeyModifiers modifiers = default)
	{
		Down(target, target, position, modifiers);
	}

	public void Down(Interactive target, Interactive source, Point position = default, KeyModifiers modifiers = default)
	{
		_pointer.Capture((IInputElement) target);
		source.RaiseEvent(new PointerPressedEventArgs(source, _pointer, source, position, Timestamp(),
			new(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
			modifiers, ++_clickCount));
	}

	public void Move(Interactive target, in Point position, KeyModifiers modifiers = default)
	{
		Move(target, target, position, modifiers);
	}

	public void Move(Interactive target, Interactive source, in Point position, KeyModifiers modifiers = default)
	{
		var e = new PointerEventArgs(InputElement.PointerMovedEvent, source, _pointer, target, position,
			Timestamp(), new(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), modifiers);
		if (_pointer.CapturedGestureRecognizer != null)
		{
			_pointer.CapturedGestureRecognizer.PointerMovedInternal(e);
		}
		else
		{
			target.RaiseEvent(e);
		}
	}

	public void Tap(Interactive target, Point position = default, KeyModifiers modifiers = default)
	{
		Tap(target, target, position, modifiers);
	}

	public void Tap(Interactive target, Interactive source, Point position = default, KeyModifiers modifiers = default)
	{
		Down(target, source, position, modifiers);
		Up(target, source, position, modifiers);
	}

	public void Up(Interactive target, Point position = default, KeyModifiers modifiers = default)
	{
		Up(target, target, position, modifiers);
	}

	public void Up(Interactive target, Interactive source, Point position = default, KeyModifiers modifiers = default)
	{
		var e = new PointerReleasedEventArgs(source, _pointer, target, position, Timestamp(),
			new(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), modifiers, MouseButton.Left);

		if (_pointer.CapturedGestureRecognizer != null)
		{
			_pointer.CapturedGestureRecognizer.PointerReleasedInternal(e);
		}
		else
		{
			source.RaiseEvent(e);
		}

		Cancel();
	}

	private ulong Timestamp()
	{
		return _nextStamp++;
	}

	#endregion
}