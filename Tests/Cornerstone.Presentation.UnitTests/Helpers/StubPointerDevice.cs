#region References

using System;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Input.Raw;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

public sealed class StubPointer : IPointer
{
	#region Fields

	#endregion

	#region Constructors

	public StubPointer(PointerType type = PointerType.Mouse)
	{
		Type = type;
	}

	#endregion

	#region Properties

	public IInputElement Captured { get; set; }

	public int Id { get; set; }

	public bool IsPrimary { get; set; }

	public PointerType Type { get; }

	#endregion

	#region Methods

	public void Capture(IInputElement control)
	{
		Captured = control;
	}

	#endregion
}

public sealed class StubPointerDevice : IPointerDevice
{
	#region Fields

	private Action<RawInputEventArgs> _processRawEvent;

	#endregion

	#region Constructors

	public StubPointerDevice(IPointer pointer = null, PointerType pointerType = PointerType.Mouse)
	{
		Pointer = pointer ?? new StubPointer(pointerType);
	}

	#endregion

	#region Properties

	public IPointer Pointer { get; private set; }

	#endregion

	#region Methods

	public void ProcessRawEvent(RawInputEventArgs ev)
	{
		_processRawEvent?.Invoke(ev);
	}

	public void SetPointer(IPointer pointer)
	{
		Pointer = pointer;
	}

	public void SetProcessRawEvent(Action<RawInputEventArgs> handler)
	{
		_processRawEvent = handler;
	}

	public IPointer TryGetPointer(RawPointerEventArgs ev)
	{
		return Pointer;
	}

	#endregion
}