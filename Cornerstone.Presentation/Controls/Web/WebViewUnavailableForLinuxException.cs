#region References

using System;
using Cornerstone.Presentation.Controls;

#endregion

namespace Cornerstone.Presentation.Controls.Web;

/// <summary>
/// Linux WebView could not start the system WebKitGTK engine.
/// </summary>
public class WebViewUnavailableForLinuxException : InvalidOperationException
{
	#region Constructors

	public WebViewUnavailableForLinuxException(string message)
		: base(message)
	{
	}

	#endregion
}