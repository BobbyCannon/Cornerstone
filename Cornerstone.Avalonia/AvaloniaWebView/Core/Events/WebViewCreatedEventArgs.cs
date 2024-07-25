#region References

using System;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Core.Events;

public class WebViewCreatedEventArgs : EventArgs
{
	#region Properties

	public required bool IsSucceed { get; set; }

	public string Message { get; set; }

	public object RawArgs { get; set; }

	#endregion
}