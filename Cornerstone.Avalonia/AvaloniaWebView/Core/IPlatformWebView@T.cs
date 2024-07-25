namespace Cornerstone.Avalonia.AvaloniaWebView.Core;

public interface IPlatformWebView<T> : IPlatformWebView
{
	#region Properties

	T PlatformView { get; }

	#endregion
}