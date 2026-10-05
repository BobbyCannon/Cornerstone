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

	public override bool IsMonitoring => _isMonitoring;

	#endregion

	#region Methods

	public override Point GetCursorPosition()
	{
		return new Point(0, 0);
	}

	public override Mouse MoveTo(int x, int y)
	{
		return this;
	}

	public override Mouse StartMonitoring()
	{
		_isMonitoring = true;
		return this;
	}

	public override Mouse StopMonitoring()
	{
		_isMonitoring = false;
		return this;
	}

	protected override InputBuilder GetInputBuilder()
	{
		return null;
	}

	protected override void RefreshState()
	{
	}

	protected override InputBuilder SendInput(InputBuilder builder)
	{
		return builder;
	}

	#endregion
}