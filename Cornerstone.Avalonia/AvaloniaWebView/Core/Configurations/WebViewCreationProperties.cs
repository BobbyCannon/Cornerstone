#region References

using System.Drawing;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Core.Configurations;

public class WebViewCreationProperties
{
	#region Properties

	public string AdditionalBrowserArguments { get; set; } = default;

	public bool AreDefaultContextMenusEnabled { get; set; } = false;
	public bool AreDevToolEnabled { get; set; } = true;

	public string BrowserExecutableFolder { get; set; } = default;

	public Color DefaultWebViewBackgroundColor { get; set; } = Color.Transparent;

	public bool? IsInPrivateModeEnabled { get; set; } = default;

	public bool IsStatusBarEnabled { get; set; } = false;

	public string Language { get; set; } = default;

	public string ProfileName { get; set; } = default;

	public string UserDataFolder { get; set; } = default;

	#endregion
}