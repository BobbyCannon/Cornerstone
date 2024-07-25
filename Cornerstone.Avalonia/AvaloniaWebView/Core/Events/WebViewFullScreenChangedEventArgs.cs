#region References

using System;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Core.Events;

public class WebViewFullScreenChangedEventArgs : EventArgs
{
	#region Properties

	public bool IsFullScreen { get; internal set; }

	public object RawArgs { get; set; }

	#endregion
}