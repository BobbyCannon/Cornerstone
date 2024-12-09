#region References

using System;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView;

public class WebViewNavigationEventArgs : EventArgs
{
	#region Properties

	public Uri Request { get; init; }

	#endregion
}