namespace Cornerstone.Avalonia.Windows.Helpers;

internal static class BlazorScriptHelper
{
	#region Constants

	public const string BlazorStaredScript =
		"""
		    Blazor.start();
		""";

	public const string BlazorStartingScript =
		"""
			window.external = {
				sendMessage: message => {
					window.chrome.webview.postMessage(message);
				},
				receiveMessage: callback => {
					window.chrome.webview.addEventListener('message', e => callback(e.data));
				}
			};
		""";

	#endregion
}