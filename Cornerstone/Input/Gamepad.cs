#region References

using System;
using Cornerstone.Presentation;
using Cornerstone.Threading;

#endregion

namespace Cornerstone.Input;

/// <summary>
/// Represents the gamepad(s) and allows for simulated input.
/// </summary>
public abstract class Gamepad : WorkerManager
{
	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="Gamepad" />.
	/// </summary>
	protected Gamepad(IDispatcher dispatcher) : base(10, dispatcher)
	{
		State = new GamepadState();
	}

	#endregion

	#region Properties

	/// <summary>
	/// The last state of the gamepad when monitoring.
	/// </summary>
	public GamepadState State { get; }

	#endregion

	#region Methods

	protected virtual void OnButtonChanged(GamepadButton button, bool pressed)
	{
		ButtonChanged?.Invoke(this, new (button, pressed));
	}

	protected virtual void OnChanged(GamepadState e)
	{
		Changed?.Invoke(this, e);
	}

	#endregion

	#region Events

	/// <summary>
	/// Called when a gamepad button changed.
	/// </summary>
	public event EventHandler<GamepadButton, bool> ButtonChanged;

	/// <summary>
	/// Called when the gamepad has changed any state.
	/// </summary>
	public event EventHandler<GamepadState> Changed;

	#endregion
}