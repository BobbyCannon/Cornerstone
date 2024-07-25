namespace Cornerstone.Avalonia.AvaloniaWebView.Core;

public interface IVirtualWebView
{
	#region Properties

	IPlatformWebView PlatformView { get; }
	object VirtualViewObject { get; }

	#endregion
}