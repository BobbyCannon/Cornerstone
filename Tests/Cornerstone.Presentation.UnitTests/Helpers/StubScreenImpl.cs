#region References

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cornerstone.Presentation.Platform;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

public sealed class StubScreenImpl : IScreenImpl
{
	#region Fields

	private IReadOnlyList<Screen> _allScreens;
	private Screen _fromPoint;
	private Screen _fromRect;
	private Screen _fromTopLevel;
	private Screen _fromWindow;

	#endregion

	#region Constructors

	public StubScreenImpl()
	{
		_allScreens = Array.Empty<Screen>();
	}

	public StubScreenImpl(params Screen[] screens)
	{
		_allScreens = screens ?? Array.Empty<Screen>();
		if (_allScreens.Count > 0)
		{
			_fromWindow = _allScreens[0];
			_fromTopLevel = _allScreens[0];
			_fromPoint = _allScreens[0];
			_fromRect = _allScreens[0];
		}
	}

	#endregion

	#region Properties

	public IReadOnlyList<Screen> AllScreens
	{
		get => _allScreens;
		set => _allScreens = value ?? Array.Empty<Screen>();
	}

	public Action Changed { get; set; }

	public int ScreenCount => _allScreens.Count;

	#endregion

	#region Methods

	public Task<bool> RequestScreenDetails()
	{
		return Task.FromResult(true);
	}

	public Screen ScreenFromPoint(PixelPoint point)
	{
		return _fromPoint;
	}

	public Screen ScreenFromRect(PixelRect rect)
	{
		return _fromRect;
	}

	public Screen ScreenFromTopLevel(ITopLevelImpl topLevel)
	{
		return _fromTopLevel;
	}

	public Screen ScreenFromWindow(IWindowBaseImpl window)
	{
		return _fromWindow;
	}

	public void SetScreenFromPoint(Screen screen)
	{
		_fromPoint = screen;
	}

	public void SetScreenFromRect(Screen screen)
	{
		_fromRect = screen;
	}

	public void SetScreenFromTopLevel(Screen screen)
	{
		_fromTopLevel = screen;
	}

	public void SetScreenFromWindow(Screen screen)
	{
		_fromWindow = screen;
	}

	#endregion
}