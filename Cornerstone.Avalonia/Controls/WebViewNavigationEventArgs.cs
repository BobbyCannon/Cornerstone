#region References

using System;

#endregion

namespace Cornerstone.Avalonia.Controls;

public class WebViewNavigationEventArgs : EventArgs
{
	#region Properties

	public Uri Uri { get; init; }

	#endregion
}