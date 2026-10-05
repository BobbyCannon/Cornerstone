#region References

using System.Threading.Tasks;
using Cornerstone.Automation.Web;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Web;

#endregion

namespace Cornerstone.Automation.Presentation;

/// <summary>
/// Automates an in-process Cornerstone WebView.
/// </summary>
public partial class WebViewAutomation : WebAutomation
{
	#region Fields

	private readonly WebView _webView;

	#endregion

	#region Constructors

	public WebViewAutomation(WebView webView)
	{
		_webView = webView;
	}

	#endregion

	#region Methods

	public override Task<string> ExecuteJavaScriptAsync(string script)
	{
		if (_webView == null)
		{
			return Task.FromResult(string.Empty);
		}

		return _webView.InvokeScriptAsync(script) ?? Task.FromResult(string.Empty);
	}

	protected override string GetUri()
	{
		return _webView?.Uri?.AbsoluteUri ?? string.Empty;
	}

	protected override void NavigateTo(string uri)
	{
		_webView.Navigate(uri);
	}

	#endregion
}