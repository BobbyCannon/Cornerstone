#region References

using System.Collections.Generic;
using System.Diagnostics;
using Cornerstone.Avalonia.TextEditor.Document;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;

/// <summary>
/// A node in the text view's height tree.
/// </summary>
internal sealed class HeightTreeNode
{
	#region Fields

	/// <summary>
	/// List of the sections that hold this node collapsed.
	/// Invariant 1:
	/// For each document line in the range described by a CollapsedSection, exactly one ancestor
	/// contains that CollapsedSection.
	/// Invariant 2:
	/// A CollapsedSection is contained either in left+middle or middle+right or just middle.
	/// Invariant 3:
	/// Start and end of a CollapsedSection always contain the collapsedSection in their
	/// documentLine (middle node).
	/// </summary>
	internal List<CollapsedLineSection> CollapsedSections;

	internal bool Color;
	internal readonly DocumentLine DocumentLine;

	internal HeightTreeNode Left, Right, Parent;
	internal HeightTreeLineNode LineNode;

	/// <summary>
	/// The number of lines in this node and its child nodes.
	/// Invariant:
	/// totalCount = 1 + left.totalCount + right.totalCount
	/// </summary>
	internal int TotalCount;

	/// <summary>
	/// The total height of this node and its child nodes, excluding directly collapsed nodes.
	/// Invariant:
	/// totalHeight = left.IsDirectlyCollapsed ? 0 : left.totalHeight
	/// + lineNode.IsDirectlyCollapsed ? 0 : lineNode.Height
	/// + right.IsDirectlyCollapsed ? 0 : right.totalHeight
	/// </summary>
	internal double TotalHeight;

	#endregion

	#region Constructors

	internal HeightTreeNode()
	{
	}

	internal HeightTreeNode(DocumentLine documentLine, double height)
	{
		DocumentLine = documentLine;
		TotalCount = 1;
		LineNode = new HeightTreeLineNode(height);
		TotalHeight = height;
	}

	#endregion

	#region Properties

	internal bool IsDirectlyCollapsed => CollapsedSections != null;

	internal HeightTreeNode LeftMost
	{
		get
		{
			var node = this;
			while (node.Left != null)
			{
				node = node.Left;
			}
			return node;
		}
	}

	internal HeightTreeNode RightMost
	{
		get
		{
			var node = this;
			while (node.Right != null)
			{
				node = node.Right;
			}
			return node;
		}
	}

	/// <summary>
	/// Gets the inorder successor of the node.
	/// </summary>
	internal HeightTreeNode Successor
	{
		get
		{
			if (Right != null)
			{
				return Right.LeftMost;
			}
			var node = this;
			HeightTreeNode oldNode;
			do
			{
				oldNode = node;
				node = node.Parent;
				// go up until we are coming out of a left subtree
			} while ((node != null) && (node.Right == oldNode));
			return node;
		}
	}

	#endregion

	#region Methods

	internal void AddDirectlyCollapsed(CollapsedLineSection section)
	{
		if (CollapsedSections == null)
		{
			CollapsedSections = [];
			TotalHeight = 0;
		}
		Debug.Assert(!CollapsedSections.Contains(section));
		CollapsedSections.Add(section);
	}

	internal void RemoveDirectlyCollapsed(CollapsedLineSection section)
	{
		Debug.Assert(CollapsedSections.Contains(section));
		CollapsedSections.Remove(section);
		if (CollapsedSections.Count == 0)
		{
			CollapsedSections = null;
			TotalHeight = LineNode.TotalHeight;
			if (Left != null)
			{
				TotalHeight += Left.TotalHeight;
			}
			if (Right != null)
			{
				TotalHeight += Right.TotalHeight;
			}
		}
	}

	#endregion

	#if DEBUG
	public override string ToString()
	{
		return "[HeightTreeNode "
			+ DocumentLine.LineNumber + " CS=" + GetCollapsedSections(CollapsedSections)
			+ " Line.CS=" + GetCollapsedSections(LineNode.CollapsedSections)
			+ " Line.Height=" + LineNode.Height
			+ " TotalHeight=" + TotalHeight
			+ "]";
	}

	private static string GetCollapsedSections(List<CollapsedLineSection> list)
	{
		if (list == null)
		{
			return "{}";
		}
		return "{" +
			string.Join(",",
				list.ConvertAll(cs => cs.Id).ToArray())
			+ "}";
	}
	#endif
}