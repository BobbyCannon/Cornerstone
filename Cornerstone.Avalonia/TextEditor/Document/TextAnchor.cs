#region References

using System;
using Cornerstone.Collections;
using Cornerstone.Internal;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Document;

/// <summary>
/// The TextAnchor class references an offset (a position between two characters).
/// It automatically updates the offset when text is inserted/removed in front of the anchor.
/// </summary>
/// <remarks>
/// <para>
/// Use the <see cref="Offset" /> property to get the offset from a text anchor.
/// Use the <see cref="TextEditorDocument.CreateAnchor" /> method to create an anchor from an offset.
/// </para>
/// <para>
/// The document will automatically update all text anchors; and because it uses weak references to do so,
/// the garbage collector can simply collect the anchor object when you don't need it anymore.
/// </para>
/// <para>
/// Moreover, the document is able to efficiently update a large number of anchors without having to look
/// at each anchor object individually. Updating the offsets of all anchors usually only takes time logarithmic
/// to the number of anchors. Retrieving the <see cref="Offset" /> property also runs in O(lg N).
/// </para>
/// <inheritdoc cref="IsDeleted" />
/// <inheritdoc cref="MovementType" />
/// <para>
/// If you want to track a segment, you can use the <see cref="AnchorRange" /> class which
/// implements <see cref="IRange" /> using two text anchors.
/// </para>
/// </remarks>
/// <example>
/// Usage:
/// <code>TextAnchor anchor = document.CreateAnchor(offset);
/// ChangeMyDocument();
/// int newOffset = anchor.Offset;
/// </code>
/// </example>
public sealed class TextAnchor : ITextAnchor
{
	#region Constructors

	internal TextAnchor(TextEditorDocument document)
	{
		Document = document;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the column number of this anchor.
	/// </summary>
	/// <exception cref="InvalidOperationException"> Thrown when trying to get the Offset from a deleted anchor. </exception>
	public int Column
	{
		get
		{
			var offset = Offset;
			return (offset - Document.GetLineByOffset(offset).StartIndex) + 1;
		}
	}

	/// <summary>
	/// Gets the document owning the anchor.
	/// </summary>
	public TextEditorDocument Document { get; }

	/// <inheritdoc />
	public bool IsDeleted
	{
		get
		{
			Document.DebugVerifyAccess();
			return Node == null;
		}
	}

	/// <summary>
	/// Gets the line number of the anchor.
	/// </summary>
	/// <exception cref="InvalidOperationException"> Thrown when trying to get the Offset from a deleted anchor. </exception>
	public int Line => Document.GetLineByOffset(Offset).LineNumber;

	/// <summary>
	/// Gets the text location of this anchor.
	/// </summary>
	/// <exception cref="InvalidOperationException"> Thrown when trying to get the Offset from a deleted anchor. </exception>
	public TextLocation Location => Document.GetLocation(Offset);

	/// <inheritdoc />
	public AnchorMovementType MovementType { get; set; }

	/// <summary>
	/// Gets the offset of the text anchor.
	/// </summary>
	/// <exception cref="InvalidOperationException"> Thrown when trying to get the Offset from a deleted anchor. </exception>
	public int Offset
	{
		get
		{
			Document.DebugVerifyAccess();

			var n = Node;
			if (n == null)
			{
				throw new InvalidOperationException();
			}

			var offset = n.Length;
			if (n.Left != null)
			{
				offset += n.Left.TotalLength;
			}
			while (n.Parent != null)
			{
				if (n == n.Parent.Right)
				{
					if (n.Parent.Left != null)
					{
						offset += n.Parent.Left.TotalLength;
					}
					offset += n.Parent.Length;
				}
				n = n.Parent;
			}
			return offset;
		}
	}

	/// <inheritdoc />
	public bool SurviveDeletion { get; set; }

	internal TextAnchorNode Node { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override string ToString()
	{
		return "[TextAnchor Offset=" + Offset + "]";
	}

	internal void OnDeleted(DelayedEvents delayedEvents)
	{
		Node = null;
		delayedEvents.DelayedRaise(Deleted, this, EventArgs.Empty);
	}

	#endregion

	#region Events

	/// <inheritdoc />
	public event EventHandler Deleted;

	#endregion
}