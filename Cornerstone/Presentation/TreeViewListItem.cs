#region References

using System;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Presentation;

/// <inheritdoc />
public class TreeViewListItem : HierarchyListItem
{
	#region Constructors

	/// <inheritdoc />
	public TreeViewListItem(Guid id, IDispatcher dispatcher)
		: base(dispatcher)
	{
		Id = id;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The unique ID of the tree list item. Must be globally unique
	/// </summary>
	public Guid Id { get; }

	/// <summary>
	/// Represents the expanded state of the tree view item.
	/// </summary>
	public bool IsExpanded { get; set; }

	/// <summary>
	/// Represents the selected state of the tree view item.
	/// </summary>
	public bool IsSelected { get; set; }

	/// <summary>
	/// The name of the tree view list item.
	/// </summary>
	public string Name { get; set; }

	#endregion
}