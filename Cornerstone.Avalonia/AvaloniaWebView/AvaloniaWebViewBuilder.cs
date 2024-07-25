#region References

using System;
using Cornerstone.Avalonia.AvaloniaWebView.Core.Configurations;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView;

public static class AvaloniaWebViewBuilder
{
	#region Methods

	public static void Initialize(Action<WebViewCreationProperties> configDelegate)
	{
		WebViewCreationProperties creationProperties = new();
		configDelegate?.Invoke(creationProperties);
		//WebViewLocator.s_Registrator.RegisterInstance<WebViewCreationProperties>(creationProperties);
		CornerstoneApplication.PlatformDependencies.AddSingleton(creationProperties);
	}

	#endregion
}