#region References

using Avalonia.Input;
using Avalonia.Platform;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView;

public interface IWebViewAdapter : IWebView
{
	#region Properties

	IPlatformHandle PlatformHandle { get; }

	#endregion

	#region Methods

	bool HandleKeyDown(Key key, KeyModifiers keyModifiers);

	void HandleResize(int width, int height, float zoom);

	#endregion
}