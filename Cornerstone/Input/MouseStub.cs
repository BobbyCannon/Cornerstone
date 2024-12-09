#region References

using System.Drawing;

#endregion

namespace Cornerstone.Input;

public class MouseStub : Mouse
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
	public override Point GetCursorPosition()
	{
		return new Point(0, 0);
	}

	/// <inheritdoc />
	public override Mouse LeftButtonClick()
	{
		return this;
	}

	/// <inheritdoc />
	public override Mouse LeftButtonClick(int x, int y)
	{
		return this;
	}

	/// <inheritdoc />
	public override Mouse MiddleButtonClick(int x, int y)
	{
		return this;
	}

	/// <inheritdoc />
	public override Mouse MoveTo(int x, int y)
	{
		return this;
	}

	/// <inheritdoc />
	public override Mouse RightButtonClick(int x, int y)
	{
		return this;
	}

	/// <inheritdoc />
	public override Mouse StartMonitoring()
	{
		_isMonitoring = true;
		return this;
	}

	/// <inheritdoc />
	public override Mouse StopMonitoring()
	{
		_isMonitoring = false;
		return this;
	}

	/// <inheritdoc />
	protected override InputBuilder GetInputBuilder()
	{
		return null;
	}

	#endregion
}