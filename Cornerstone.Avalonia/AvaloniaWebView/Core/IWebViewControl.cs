#region References

using System;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Core;

public interface IWebViewControl
{
	#region Properties

	bool IsCanGoBack { get; }
	bool IsCanGoForward { get; }

	#endregion

	#region Methods

	Task<string> ExecuteScriptAsync(string javaScript);

	bool GoBack();
	bool GoForward();

	bool Navigate(Uri uri);
	bool NavigateToString(string htmlContent);

	bool OpenDevToolsWindow();

	bool PostWebMessageAsJson(string webMessageAsJson, Uri baseUri);
	bool PostWebMessageAsString(string webMessageAsString, Uri baseUri);
	bool Reload();
	bool Stop();

	#endregion
}