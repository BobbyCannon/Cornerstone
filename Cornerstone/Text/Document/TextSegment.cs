#region References

using System;
using System.Diagnostics;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Text.Document;

/// <summary>
/// A segment that can be put into a <see cref="TextSegmentCollection{T}" />.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="TextRange" /> can be stand-alone or part of a <see cref="TextSegmentCollection{T}" />.
/// If the segment is stored inside a TextSegmentCollection, its Offset and Length will be updated by that collection.
/// </para>
/// <para>
/// When the document changes, the offsets of all text segments in the TextSegmentCollection will be adjusted accordingly.
/// Start offsets move like <see cref="AnchorMovementType"> AnchorMovementType.AfterInsertion </see>,
/// end offsets move like <see cref="AnchorMovementType"> AnchorMovementType.BeforeInsertion </see>
/// (i.e. the segment will always stay as small as possible).
/// </para>
/// <para>
/// If a document change causes a segment to be deleted completely, it will be reduced to length 0, but segments are
/// never automatically removed from the collection.
/// Segments with length 0 will never expand due to document changes, and they move as <c> AfterInsertion </c>.
/// </para>
/// <para>
/// Thread-safety: a TextSegmentCollection that is connected to a <see cref="TextEditorDocument" /> may only be used on that document's owner thread.
/// A disconnected TextSegmentCollection is safe for concurrent reads, but concurrent access is not safe when there are writes.
/// Keep in mind that reading the Offset properties of a text segment inside the collection is a read access on the
/// collection; and setting an Offset property of a text segment is a write access on the collection.
/// </para>
/// </remarks>
/// <seealso cref="IRange" />
/// <seealso cref="AnchorRange" />
/// <seealso cref="TextSegmentCollection{T}" />
public class TextRange : IRange
{
	#region Properties

	/// <summary>
	/// Gets/Sets the end offset of the segment.
	/// </summary>
	/// <remarks>
	/// Setting the end offset will change the length, the start offset will stay constant.
	/// </remarks>
	public int EndIndex
	{
		get => StartOffset + Length;
		set
		{
			var newLength = value - StartOffset;
			if (newLength < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(value), "EndOffset must be greater or equal to StartOffset");
			}
			Length = newLength;
		}
	}

	/// <summary>
	/// Gets/Sets the length of the segment.
	/// </summary>
	/// <remarks>
	/// Setting the length will change the end offset, the start offset will stay constant.
	/// </remarks>
	public int Length
	{
		get => SegmentLength;
		set
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(value), "Length must not be negative");
			}
			if (SegmentLength != value)
			{
				SegmentLength = value;
				OwnerTree?.UpdateAugmentedData(this);
				OnSegmentChanged();
			}
		}
	}

	/// <summary>
	/// Gets/Sets the start offset of the segment.
	/// </summary>
	/// <remarks>
	/// When setting the start offset, the end offset will change, too: the Length of the segment will stay constant.
	/// </remarks>
	public int StartOffset
	{
		get
		{
			// If the segment is not connected to a tree, we store the offset in "nodeLength".
			// Otherwise, "nodeLength" contains the distance to the start offset of the previous node
			Debug.Assert(!((OwnerTree == null) && (Parent != null)));
			Debug.Assert(!((OwnerTree == null) && (Left != null)));

			var n = this;
			var offset = n.NodeLength;
			if (n.Left != null)
			{
				offset += n.Left.TotalNodeLength;
			}
			while (n.Parent != null)
			{
				if (n == n.Parent.Right)
				{
					if (n.Parent.Left != null)
					{
						offset += n.Parent.Left.TotalNodeLength;
					}
					offset += n.Parent.NodeLength;
				}
				n = n.Parent;
			}
			return offset;
		}
		set
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(value), "Offset must not be negative");
			}
			if (StartOffset != value)
			{
				// need a copy of the variable because ownerTree.Remove() sets this.ownerTree to null
				var ownerTree = OwnerTree;
				if (ownerTree != null)
				{
					ownerTree.Remove(this);
					NodeLength = value;
					ownerTree.Add(this);
				}
				else
				{
					NodeLength = value;
				}
				OnSegmentChanged();
			}
		}
	}

	/// <summary>
	/// Gets whether this segment is connected to a TextSegmentCollection and will automatically
	/// update its offsets.
	/// </summary>
	protected bool IsConnectedToCollection => OwnerTree != null;

	/// <summary>
	/// The color of the segment in the red/black tree.
	/// </summary>
	internal bool Color { get; set; }

	/// <summary>
	/// distanceToMaxEnd = Max(segmentLength,
	/// left.distanceToMaxEnd + left.Offset - Offset,
	/// left.distanceToMaxEnd + right.Offset - Offset)
	/// </summary>
	internal int DistanceToMaxEnd { get; set; }

	internal TextRange Left { get; set; }

	internal TextRange LeftMost
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

	/// <summary>
	/// The "length" of the node (distance to previous node)
	/// </summary>
	internal int NodeLength { get; set; }

	internal ISegmentTree OwnerTree { get; set; }
	internal TextRange Parent { get; set; }

	/// <summary>
	/// Gets the inorder predecessor of the node.
	/// </summary>
	internal TextRange Predecessor
	{
		get
		{
			if (Left != null)
			{
				return Left.RightMost;
			}
			var node = this;
			TextRange oldNode;
			do
			{
				oldNode = node;
				node = node.Parent;
				// go up until we are coming out of a right subtree
			} while ((node != null) && (node.Left == oldNode));
			return node;
		}
	}

	internal TextRange Right { get; set; }

	internal TextRange RightMost
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
	/// The length of the segment (do not confuse with nodeLength).
	/// </summary>
	internal int SegmentLength { get; set; }

	/// <summary>
	/// Gets the inorder successor of the node.
	/// </summary>
	internal TextRange Successor
	{
		get
		{
			if (Right != null)
			{
				return Right.LeftMost;
			}
			var node = this;
			TextRange oldNode;
			do
			{
				oldNode = node;
				node = node.Parent;
				// go up until we are coming out of a left subtree
			} while ((node != null) && (node.Right == oldNode));
			return node;
		}
	}

	/// <summary>
	/// The total "length" of this subtree.
	/// </summary>
	internal int TotalNodeLength { get; set; } // totalNodeLength = nodeLength + left.totalNodeLength + right.totalNodeLength

	int IRange.StartIndex => StartOffset;

	#endregion

	#region Methods

	/// <inheritdoc />
	public override string ToString()
	{
		return $"[{GetType().Name} Offset={StartOffset} Length={Length} EndOffset={EndIndex}]";
	}

	/// <summary>
	/// This method gets called when the StartOffset/Length/EndOffset properties are set.
	/// It is not called when StartOffset/Length/EndOffset change due to document changes
	/// </summary>
	protected virtual void OnSegmentChanged()
	{
	}

	#if DEBUG
	internal string ToDebugString()
	{
		return
			$"[nodeLength={NodeLength} totalNodeLength={TotalNodeLength} distanceToMaxEnd={DistanceToMaxEnd} MaxEndOffset={StartOffset + DistanceToMaxEnd}]";
	}
	#endif

	#endregion
}