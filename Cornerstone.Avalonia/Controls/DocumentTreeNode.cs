#region References

using System.Collections.ObjectModel;

#endregion

namespace Cornerstone.Avalonia.Controls;

/// <summary>
/// Folder or document node for the documentation catalog tree.
/// </summary>
public sealed class DocumentTreeNode
{
	#region Constructors

	public DocumentTreeNode(string name, string documentId = null)
	{
		Name = name ?? string.Empty;
		DocumentId = documentId;
		Children = new ObservableCollection<DocumentTreeNode>();
	}

	#endregion

	#region Properties

	public ObservableCollection<DocumentTreeNode> Children { get; }

	/// <summary>
	/// Catalog document id when this is a leaf markdown file; null for folders.
	/// </summary>
	public string DocumentId { get; }

	public bool IsExpanded { get; set; }

	public bool IsFolder => string.IsNullOrEmpty(DocumentId);

	public string Name { get; }

	#endregion
}