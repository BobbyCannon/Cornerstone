#region References

using System;
using System.IO;

#endregion

namespace Cornerstone.VisualStudio.Services;

/// <summary>
/// XAML preview stack. Avalonia uses .axaml + Avalonia.Remote.Protocol;
/// Cornerstone uses .cxaml + Cornerstone.Presentation.Remote.Protocol.
/// </summary>
public enum XamlPreviewPlatform
{
	Avalonia = 0,
	Cornerstone = 1
}

internal static class XamlPreviewPlatformResolver
{
	public static XamlPreviewPlatform FromPath(string path)
	{
		// Only .axaml uses the Avalonia preview host. .cxaml stays on Cornerstone.
		if (!string.IsNullOrWhiteSpace(path) &&
			path.EndsWith("." + CornerstoneConstants.Axaml, StringComparison.OrdinalIgnoreCase))
		{
			return XamlPreviewPlatform.Avalonia;
		}

		return XamlPreviewPlatform.Cornerstone;
	}

	public static bool IsXamlDocument(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return false;
		}

		var ext = Path.GetExtension(path);
		return ext.Equals("." + CornerstoneConstants.Cxaml, StringComparison.OrdinalIgnoreCase)
			|| ext.Equals("." + CornerstoneConstants.Axaml, StringComparison.OrdinalIgnoreCase);
	}
}
