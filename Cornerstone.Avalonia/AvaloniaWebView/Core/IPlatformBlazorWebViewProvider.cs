#region References

using System.Reflection;
using Microsoft.Extensions.FileProviders;

#endregion

namespace Cornerstone.Avalonia.AvaloniaWebView.Core;

public interface IPlatformBlazorWebViewProvider
{
	#region Properties

	string Scheme { get; }

	#endregion

	#region Methods

	IFileProvider CreateFileProvider(Assembly assembly, string contentRootDirFullPath);

	#endregion
}