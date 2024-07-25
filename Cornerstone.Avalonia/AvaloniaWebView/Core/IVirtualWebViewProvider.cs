#region References

using System;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Core;

public interface IVirtualWebViewProvider
{
	#region Properties

	string AppHostAddress { get; }
	Uri BaseUri { get; }

	#endregion
}