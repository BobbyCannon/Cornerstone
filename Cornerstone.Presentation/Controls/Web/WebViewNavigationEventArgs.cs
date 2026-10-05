#region References

using System;
using Cornerstone.Presentation.Controls;

#endregion

namespace Cornerstone.Presentation.Controls.Web;

public class WebViewNavigationEventArgs : EventArgs
{
	#region Properties

	public Uri Uri { get; init; }

	#endregion
}