#region References

using System.Reflection;
using Cornerstone.Avalonia.Android.Common;
using Cornerstone.Avalonia.AvaloniaWebView.Core;
using Microsoft.Extensions.FileProviders;

#endregion

namespace Cornerstone.Avalonia.Android;

internal class BlazorWebViewHandlerProvider : IPlatformBlazorWebViewProvider
{
	#region Properties

	public string Scheme => "https";

	#endregion

	#region Methods

	public IFileProvider CreateFileProvider(Assembly assembly, string contentRootDirFullPath)
	{
		return new AndroidAssetFileProvider(assembly, contentRootDirFullPath);
	}

	#endregion
}