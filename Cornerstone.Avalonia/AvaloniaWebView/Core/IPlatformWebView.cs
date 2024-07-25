#region References

using System;
using System.Threading.Tasks;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Core;

public interface IPlatformWebView : IWebViewControl, IDisposable, IAsyncDisposable
{
	#region Properties

	bool IsInitialized { get; }
	IntPtr NativeHandler { get; }
	object PlatformViewContext { get; }

	#endregion

	#region Methods

	Task<bool> Initialize();

	#endregion
}