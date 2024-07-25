#region References

using System.IO;
using System.Reflection;
using Cornerstone.Avalonia.AvaloniaWebView.Core;
using Microsoft.Extensions.FileProviders;

#endregion

namespace Cornerstone.Avalonia.Windows;

internal class BlazorWebViewHandlerProvider : IPlatformBlazorWebViewProvider
{
	#region Properties

	public string Scheme => "https";

	#endregion

	#region Methods

	public IFileProvider CreateFileProvider(Assembly assembly, string contentRootDirFullPath)
	{
		return Directory.Exists(contentRootDirFullPath)
			? new PhysicalFileProvider(contentRootDirFullPath)
			: new NullFileProvider();
	}

	#endregion
}