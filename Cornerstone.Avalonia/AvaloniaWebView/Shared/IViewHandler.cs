#region References

using Avalonia.Controls;
using Cornerstone.Avalonia.AvaloniaWebView.Core;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Shared;

public interface IViewHandler
{
	#region Properties

	Control AttachableControl { get; }
	IPlatformWebView PlatformWebView { get; }

	#endregion
}