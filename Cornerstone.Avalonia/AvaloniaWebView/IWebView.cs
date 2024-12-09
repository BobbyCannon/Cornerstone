#region References

using System;
using System.ComponentModel;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView;

public interface IWebView : INotifyPropertyChanged
{
	#region Properties

	bool CanGoBack { get; }

	bool CanGoForward { get; }

	byte[] Favicon { get; }

	string Title { get; }

	Uri Uri { get; set; }

	#endregion

	#region Methods

	bool GoBack();

	bool GoForward();

	Task<string> InvokeScriptAsync(string script);

	void Navigate(Uri uri);

	void NavigateToString(string text);

	void Reload();

	void Stop();

	#endregion

	#region Events

	event EventHandler<WebViewNavigationEventArgs> NavigationCompleted;

	event EventHandler<WebViewNavigationEventArgs> NavigationStarted;

	event EventHandler<WebViewNewWindowEventArgs> NewWindowRequested;

	#endregion
}