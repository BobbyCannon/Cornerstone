#region References

using System;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Core.Events;

public class WebViewMessageReceivedEventArgs : EventArgs
{
	#region Properties

	public required string Message { get; set; }

	public string MessageAsJson { get; set; }

	public object RawArgs { get; set; }
	public required Uri Source { get; set; }

	#endregion
}