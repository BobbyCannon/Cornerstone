#region References

using System;
using Avalonia;
using Cornerstone.Avalonia.AvaloniaWebView.Shared.Core;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Shared;

public static class AppBuilderExtensions
{
	#region Methods

	public static AppBuilder ConfigureAvaloniaHandlers(this AppBuilder builder, Action<IAvaloniaHandlerCollection> configureDelegate)
	{
		AvaloniaHandlerCollection list = new();
		configureDelegate?.Invoke(list);
		return builder;
	}

	#endregion
}