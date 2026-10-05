#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;

#endregion

namespace Cornerstone.Presentation.Controls.Web;

public interface IWebView : INotifyPropertyChanged
{
	#region Properties

	bool CanGoBack { get; }

	bool CanGoForward { get; }

	string Content { get; }

	byte[] Favicon { get; }

	string Title { get; }

	Uri Uri { get; set; }

	#endregion

	#region Methods

	IEnumerable<string> GetAvailableProfiles();

	string GetContent();

	bool GoBack();

	bool GoForward();

	Task<string> InvokeScriptAsync(string script);

	void Navigate(Uri uri);

	string NavigateToString(string text);

	void Reload();

	void Stop();

	#endregion

	#region Events

	event EventHandler<WebViewNavigationEventArgs> NavigationCompleted;

	event EventHandler<WebViewNavigationEventArgs> NavigationStarted;

	event EventHandler<WebViewNewWindowEventArgs> NewWindowRequested;

	#endregion
}