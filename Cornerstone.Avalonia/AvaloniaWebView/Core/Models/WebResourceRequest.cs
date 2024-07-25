namespace Cornerstone.Avalonia.AvaloniaWebView.Core.Models;

public class WebResourceRequest
{
	#region Properties

	public required bool AllowFallbackOnHostPage { get; set; }
	public required string RequestUri { get; set; }

	#endregion
}