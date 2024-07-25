#region References

using System;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Enums;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Core.Events;

public class WebViewNewWindowEventArgs : EventArgs
{
	#region Properties

	public object RawArgs { get; set; }
	public Uri Url { get; set; }
	public required UrlRequestStrategy UrlLoadingStrategy { get; set; }

	#endregion
}