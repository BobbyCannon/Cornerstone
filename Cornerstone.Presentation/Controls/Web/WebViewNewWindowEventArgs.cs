using Cornerstone.Presentation.Controls;

namespace Cornerstone.Presentation.Controls.Web;

public class WebViewNewWindowEventArgs : WebViewNavigationEventArgs
{
	#region Properties

	public bool Handled { get; set; }

	#endregion
}