#region References

using System;

#endregion

namespace Cornerstone.Avalonia.WebView;

public class WebViewNavigationEventArgs : EventArgs
{
	#region Properties

	public Uri Request { get; init; }

	#endregion
}