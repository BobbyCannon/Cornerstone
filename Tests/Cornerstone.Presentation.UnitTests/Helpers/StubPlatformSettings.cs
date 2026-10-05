#region References

using System;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Platform;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

public sealed class StubPlatformSettings : DefaultPlatformSettings
{
	#region Fields

	private Size _doubleTapSize;

	private TimeSpan _doubleTapTime;
	private TimeSpan _holdWaitDuration;
	private Size _tapSize;
	private bool _useDoubleTapSize;
	private bool _useDoubleTapTime;
	private bool _useTapSize;

	#endregion

	#region Constructors

	public StubPlatformSettings()
	{
		_holdWaitDuration = TimeSpan.FromMilliseconds(300);
		_tapSize = new Size(16, 16);
		_doubleTapSize = new Size(16, 16);
		_doubleTapTime = TimeSpan.FromMilliseconds(500);
	}

	#endregion

	#region Properties

	public override TimeSpan HoldWaitDuration => _holdWaitDuration;

	#endregion

	#region Methods

	public override Size GetDoubleTapSize(PointerType type)
	{
		return _useDoubleTapSize ? _doubleTapSize : base.GetDoubleTapSize(type);
	}

	public override TimeSpan GetDoubleTapTime(PointerType type)
	{
		return _useDoubleTapTime ? _doubleTapTime : base.GetDoubleTapTime(type);
	}

	public override Size GetTapSize(PointerType type)
	{
		return _useTapSize ? _tapSize : base.GetTapSize(type);
	}

	public void SetDoubleTapSize(Size size)
	{
		_doubleTapSize = size;
		_useDoubleTapSize = true;
	}

	public void SetDoubleTapTime(TimeSpan value)
	{
		_doubleTapTime = value;
		_useDoubleTapTime = true;
	}

	public void SetHoldWaitDuration(TimeSpan value)
	{
		_holdWaitDuration = value;
	}

	public void SetTapSize(Size size)
	{
		_tapSize = size;
		_useTapSize = true;
	}

	#endregion
}