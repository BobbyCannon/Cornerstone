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

	/// <inheritdoc />
	public override bool IsMonitoring => _isMonitoring;

	#endregion

	#region Methods

	/// <inheritdoc />
	public override bool IsKeyDown(params KeyboardKey[] keys)
	{
		return false;
	}

	/// <inheritdoc />
	public override bool IsKeyUp(params KeyboardKey[] keys)
	{
		return false;
	}

	/// <inheritdoc />
	public override bool IsTogglingKeyInEffect(KeyboardKey key)
	{
		return false;
	}

	/// <inheritdoc />
	public override InputBuilder SendInput(InputBuilder builder, TimeSpan delay)
	{
		return builder;
	}

	/// <inheritdoc />
	public override Keyboard StartMonitoring()
	{
		_isMonitoring = true;
		return this;
	}

	/// <inheritdoc />
	public override Keyboard StopMonitoring()
	{
		_isMonitoring = false;
		return this;
	}

	/// <inheritdoc />
	protected override InputBuilder GetInputBuilder()
	{
		return null;
	}

	/// <inheritdoc />
	protected override InputBuilder GetInputBuilder(KeyStroke[] keyStrokes)
	{
		return null;
	}

	/// <inheritdoc />
	protected override InputBuilder GetInputBuilder(KeyboardModifier modifier, KeyboardKey[] keys)
	{
		return null;
	}

	/// <inheritdoc />
	protected override InputBuilder GetInputBuilder(string text, bool textInputAsKeyPresses)
	{
		return null;
	}

	/// <inheritdoc />
	protected override InputBuilder GetInputBuilder(KeyboardKey[] keys)
	{
		return null;
	}

	/// <inheritdoc />
	protected override InputBuilder SendInput(InputBuilder builder)
	{
		return null;
	}

	#endregion
}