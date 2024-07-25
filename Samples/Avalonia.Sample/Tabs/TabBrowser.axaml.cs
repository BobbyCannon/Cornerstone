#region References

using Avalonia.Interactivity;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Enums;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Events;
using Cornerstone.Avalonia.Controls;

#endregion

namespace Avalonia.Sample.Tabs;

public partial class TabBrowser : CornerstoneUserControl
{
	#region Constants

	public const string HeaderName = "Browser";

	#endregion

	#region Constructors

	public TabBrowser()
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		WebView.WebViewNewWindowRequested += WebViewNewWindowRequested;
		base.OnLoaded(e);
	}

	/// <inheritdoc />
	protected override void OnUnloaded(RoutedEventArgs e)
	{
		WebView.WebViewNewWindowRequested -= WebViewNewWindowRequested;
		base.OnUnloaded(e);
	}

	private void Reload(object sender, RoutedEventArgs e)
	{
		WebView.Reload();
	}

	private void WebViewNewWindowRequested(object sender, WebViewNewWindowEventArgs e)
	{
		e.UrlLoadingStrategy = UrlRequestStrategy.OpenInWebView;
	}

	#endregion
}