#region References

using System;

#endregion

namespace Cornerstone.Input;

public class KeyboardStub : Keyboard
{
	#region Fields

	private bool _isMonitoring;

	#endregion

	#region Properties

	public override bool IsMonitoring => _isMonitoring;

	#endregion

	#region Methods

	public override bool IsKeyDown(params KeyboardKey[] keys)
	{
		return false;
	}

	public override bool IsKeyUp(params KeyboardKey[] keys)
	{
		return false;
	}

	public override bool IsTogglingKeyInEffect(KeyboardKey key)
	{
		return false;
	}

	public override InputBuilder SendInput(InputBuilder builder, TimeSpan delay)
	{
		return builder;
	}

	public override Keyboard StartMonitoring()
	{
		_isMonitoring = true;
		return this;
	}

	public override Keyboard StopMonitoring()
	{
		_isMonitoring = false;
		return this;
	}

	protected override InputBuilder GetInputBuilder()
	{
		return null;
	}

	protected override InputBuilder GetInputBuilder(KeyStroke[] keyStrokes)
	{
		return null;
	}

	protected override InputBuilder GetInputBuilder(KeyboardModifier modifier, KeyboardKey[] keys)
	{
		return null;
	}

	protected override InputBuilder GetInputBuilder(string text, bool textInputAsKeyPresses)
	{
		return null;
	}

	protected override InputBuilder GetInputBuilder(KeyboardKey[] keys)
	{
		return null;
	}

	protected override InputBuilder SendInput(InputBuilder builder)
	{
		return null;
	}

	#endregion
}