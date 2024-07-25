namespace Cornerstone.Avalonia.AvaloniaWebView.Core;

public interface IVirtualWebView<TVirtualView> : IVirtualWebView
{
	#region Properties

	TVirtualView VirtualView { get; }

	#endregion
}