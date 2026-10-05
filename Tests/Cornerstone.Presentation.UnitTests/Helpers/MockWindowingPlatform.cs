#region References

using System;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Rendering.Composition;

#endregion

namespace Cornerstone.Presentation.UnitTests.Helpers;

public class MockWindowingPlatform : IWindowingPlatform
{
	#region Fields

	private readonly Func<IWindowBaseImpl, IPopupImpl> _popupImpl;
	private readonly Func<ITrayIconImpl> _trayIconImpl;
	private readonly Func<IWindowImpl> _windowImpl;
	private static readonly Size sscreenSize = new(1280, 1024);

	#endregion

	#region Constructors

	public MockWindowingPlatform(
		Func<IWindowImpl> windowImpl = null,
		Func<IWindowBaseImpl, IPopupImpl> popupImpl = null,
		Func<ITrayIconImpl> trayIconImpl = null)
	{
		_windowImpl = windowImpl;
		_popupImpl = popupImpl;
		_trayIconImpl = trayIconImpl;
	}

	#endregion

	#region Methods

	public ITopLevelImpl CreateEmbeddableTopLevel()
	{
		return CreateEmbeddableWindow();
	}

	public IWindowImpl CreateEmbeddableWindow()
	{
		throw new NotImplementedException();
	}

	public static StubWindowImpl CreatePopupMock(IWindowBaseImpl parent)
	{
		return new StubWindowImpl(popupParent: parent);
	}

	public static StubScreenImpl CreateScreenMock()
	{
		var bounds = new PixelRect(0, 0, (int) sscreenSize.Width, (int) sscreenSize.Height);
		var screen = new MockScreen(96, bounds, bounds, true);
		return new StubScreenImpl(screen);
	}

	public ITrayIconImpl CreateTrayIcon()
	{
		return _trayIconImpl?.Invoke();
	}

	public IWindowImpl CreateWindow()
	{
		if (_windowImpl is object)
		{
			return _windowImpl();
		}

		var stub = CreateWindowMock();

		if (_popupImpl is object)
		{
			stub.CreatePopupHandler = () => _popupImpl(stub);
		}

		return stub;
	}

	public static StubWindowImpl CreateWindowMock(double initialWidth = 800, double initialHeight = 600, Compositor compositor = null)
	{
		var windowImpl = new StubWindowImpl(initialWidth, initialHeight, compositor);
		windowImpl.SetMaxAutoSizeHint(sscreenSize);
		windowImpl.AutoActivateOnShow = true;
		windowImpl.RaiseClosedOnDispose = true;
		return windowImpl;
	}

	public void GetWindowsZOrder(ReadOnlySpan<IWindowImpl> windows, Span<long> zOrder)
	{
		zOrder.Clear();
	}

	#endregion
}