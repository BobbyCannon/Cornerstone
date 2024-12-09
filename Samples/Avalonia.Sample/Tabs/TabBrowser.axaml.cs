#region References

using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Cornerstone.Avalonia;
using Cornerstone.Runtime;

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
		Uri = "https://github.com/BobbyCannon/Cornerstone";
		HtmlContent = "<html> </html>";

		DataContext = this;
		InitializeComponent();
	}

	#endregion

	#region Properties

	public string HtmlContent { get; set; }

	public string Uri { get; set; }

	#endregion

	#region Methods

	public void SendScript(object sender, RoutedEventArgs routedEventArgs)
	{
		//WebView.InvokeScriptAsync("alert(\"aoeu\");");
		WebView.InvokeScriptAsync($"document.body.innerHTML = \"DateTime: {DateTimeProvider.RealTime.Now}\";");
	}

	public void SetContent(object sender, RoutedEventArgs routedEventArgs)
	{
		WebView.NavigateToString(HtmlEditor.Text);
	}

	/// <inheritdoc />
	protected override void OnLoaded(RoutedEventArgs e)
	{
		//WebView.WebViewNewWindowRequested += WebViewNewWindowRequested;
		HtmlEditor.Text = """
						<html>
							<body>
								The quick brown fox.
							</body>
						</html>
						""";
		base.OnLoaded(e);
	}

	/// <inheritdoc />
	protected override void OnUnloaded(RoutedEventArgs e)
	{
		//WebView.WebViewNewWindowRequested -= WebViewNewWindowRequested;
		base.OnUnloaded(e);
	}

	private void Refresh(object sender, RoutedEventArgs e)
	{
		WebView.Reload();
	}

	private void SampleSite(object sender, RoutedEventArgs e)
	{
		var button = (Button) sender;
		var uri = button.Content switch
		{
			"1" => "https://github.com",
			"2" => "https://bing.com",
			"3" => "https://google.com",
			"4" => "https://youtube.com",
			_ => null
		};

		if (!string.IsNullOrWhiteSpace(uri))
		{
			WebView.Navigate(uri);
		}
	}

	private void UpdateUri(object sender, RoutedEventArgs e)
	{
		if (System.Uri.TryCreate(Uri, UriKind.RelativeOrAbsolute, out var actualUri))
		{
			WebView.Navigate(actualUri);
		}
	}

	#endregion
}