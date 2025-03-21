namespace Cornerstone.Avalonia.WebView;

public class WebViewNewWindowEventArgs : WebViewNavigationEventArgs
{
	#region Properties

	public bool Handled { get; set; }

	#endregion
}