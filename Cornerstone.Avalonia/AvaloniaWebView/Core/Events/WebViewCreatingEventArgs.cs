#region References

using System;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Core.Events;

public class WebViewCreatingEventArgs : EventArgs
{
	#region Properties

	public bool Handled { get; set; }

	public object RawArgs { get; set; }

	#endregion
}