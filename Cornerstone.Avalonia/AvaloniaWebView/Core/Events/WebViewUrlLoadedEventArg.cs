#region References

using System;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Core.Events;

public class WebViewUrlLoadedEventArg : EventArgs
{
	#region Properties

	public bool IsSuccess { get; internal set; }
	public object RawArgs { get; set; }

	#endregion
}