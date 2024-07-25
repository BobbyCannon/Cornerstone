#region References

using System;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Core.Events;

public class WebViewUrlLoadingEventArg : EventArgs
{
	#region Properties

	public bool Cancel { get; set; }

	public object RawArgs { get; set; }
	public Uri Url { get; set; }

	#endregion
}