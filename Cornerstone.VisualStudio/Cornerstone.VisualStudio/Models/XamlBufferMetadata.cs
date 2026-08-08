#region References

using CompletionMetadata = Cornerstone.VisualStudio.Core.AssemblyMetadata.Metadata;

#endregion

namespace Cornerstone.VisualStudio.Models;

internal class XamlBufferMetadata
{
	#region Properties

	public CompletionMetadata CompletionMetadata { get; set; }

	public bool NeedInvalidation { get; set; } = true;

	#endregion
}