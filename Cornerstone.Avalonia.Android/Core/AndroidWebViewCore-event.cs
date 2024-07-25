#region References

using System;
using Avalonia.Controls;

#endregion

namespace Cornerstone.Avalonia.Android.Core;

partial class AndroidWebViewCore
{
	#region Methods

	private void Handler_PlatformHandlerChanged(object sender, EventArgs e)
	{
	}

	private void HostControl_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		//e.Handled = true;
	}

	private void RegisterEvents()
	{
		_handler.SizeChanged += HostControl_SizeChanged;
		_handler.PlatformHandlerChanged += Handler_PlatformHandlerChanged;
	}

	private void UnregisterEvents()
	{
		_handler.SizeChanged -= HostControl_SizeChanged;
		_handler.PlatformHandlerChanged -= Handler_PlatformHandlerChanged;
	}

	#endregion
}