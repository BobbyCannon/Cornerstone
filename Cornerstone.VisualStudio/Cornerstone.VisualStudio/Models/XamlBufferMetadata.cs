#region References

using System;
using Microsoft.VisualStudio.Text;
using CompletionMetadata = Cornerstone.VisualStudio.Core.AssemblyMetadata.Metadata;

#endregion

namespace Cornerstone.VisualStudio.Models;

internal class XamlBufferMetadata
{
	#region Properties

	public System.Collections.Generic.IReadOnlyList<string> AssemblyPaths { get; set; }

	public CompletionMetadata CompletionMetadata { get; set; }

	public bool NeedInvalidation { get; set; } = true;

	#endregion
}

/// <summary>
/// Completion and manipulators register on view-created, which can fire during
/// <c>IVsCodeWindow.SetBuffer</c>. Attach this before SetBuffer, and treat .cxaml/.axaml
/// as our XAML even if the property is not on the buffer yet.
/// </summary>
internal static class XamlBufferMetadataHelper
{
	public static XamlBufferMetadata Ensure(ITextBuffer buffer)
	{
		if (buffer == null)
		{
			return null;
		}

		return buffer.Properties.GetOrCreateSingletonProperty(
			typeof(XamlBufferMetadata),
			() => new XamlBufferMetadata());
	}

	public static bool HasMetadata(ITextBuffer buffer)
	{
		return buffer != null && buffer.Properties.ContainsProperty(typeof(XamlBufferMetadata));
	}

	public static bool IsCornerstoneXamlBuffer(ITextBuffer buffer)
	{
		if (HasMetadata(buffer))
		{
			return true;
		}

		if (buffer == null)
		{
			return false;
		}

		if (!buffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument document) ||
			document?.FilePath == null)
		{
			return false;
		}

		var path = document.FilePath;
		return path.EndsWith(".cxaml", StringComparison.OrdinalIgnoreCase) ||
			path.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase);
	}
}