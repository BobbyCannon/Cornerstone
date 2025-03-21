#region References

using System;

#endregion

namespace Cornerstone.Text.Document;

/// <summary>
/// A TextAnchorNode is placed in the TextAnchorTree.
/// It describes a section of text with a text anchor at the end of the section.
/// A weak reference is used to refer to the TextAnchor. (to save memory, we derive from WeakReference instead of referencing it)
/// </summary>
internal sealed class TextAnchorNode : WeakReference
{
	#region Constructors

	public TextAnchorNode(TextAnchor anchor) : base(anchor)
	{
	}

	#endregion

	#region Properties

	internal bool Color { get; set; }

	internal TextAnchorNode Left { get; set; }

	internal TextAnchorNode LeftMost
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

	internal int Length { get; set; }

	internal TextAnchorNode Parent { get; set; }

	/// <summary>
	/// Gets the inorder predecessor of the node.
	/// </summary>
	internal TextAnchorNode Predecessor
	{
		get
		{
			if (Left != null)
			{
				return Left.RightMost;
			}
			var node = this;
			TextAnchorNode oldNode;
			do
			{
				oldNode = node;
				node = node.Parent;
				// go up until we are coming out of a right subtree
			} while ((node != null) && (node.Left == oldNode));
			return node;
		}
	}

	internal TextAnchorNode Right { get; set; }

	internal TextAnchorNode RightMost
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
	internal TextAnchorNode Successor
	{
		get
		{
			if (Right != null)
			{
				return Right.LeftMost;
			}
			var node = this;
			TextAnchorNode oldNode;
			do
			{
				oldNode = node;
				node = node.Parent;
				// go up until we are coming out of a left subtree
			} while ((node != null) && (node.Right == oldNode));
			return node;
		}
	}

	internal int TotalLength { get; set; } // totalLength = length + left.totalLength + right.totalLength

	#endregion

	#region Methods

	public override string ToString()
	{
		return "[TextAnchorNode Length=" + Length + " TotalLength=" + TotalLength + " Target=" + Target + "]";
	}

	#endregion
}