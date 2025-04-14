#region References

using System;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Document;

/// <summary>
/// Describes a change of the document text.
/// This class is thread-safe.
/// </summary>
public class TextChangeEventArgs : EventArgs
{
	#region Constructors

	/// <summary>
	/// Creates a new TextChangeEventArgs object.
	/// </summary>
	public TextChangeEventArgs(int offset, string removedText, string insertedText)
	{
		if (offset < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(offset), offset, "offset must not be negative");
		}
		Offset = offset;
		RemovedText = removedText != null ? new StringTextSource(removedText) : StringTextSource.Empty;
		InsertedText = insertedText != null ? new StringTextSource(insertedText) : StringTextSource.Empty;
	}

	/// <summary>
	/// Creates a new TextChangeEventArgs object.
	/// </summary>
	public TextChangeEventArgs(int offset, ITextSource removedText, ITextSource insertedText)
	{
		if (offset < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(offset), offset, "offset must not be negative");
		}
		Offset = offset;
		RemovedText = removedText ?? StringTextSource.Empty;
		InsertedText = insertedText ?? StringTextSource.Empty;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The text that was inserted.
	/// </summary>
	public ITextSource InsertedText { get; }

	/// <summary>
	/// The number of characters inserted.
	/// </summary>
	public int InsertionLength => InsertedText.TextLength;

	/// <summary>
	/// The offset at which the change occurs.
	/// </summary>
	public int Offset { get; }

	/// <summary>
	/// The number of characters removed.
	/// </summary>
	public int RemovalLength => RemovedText.TextLength;

	/// <summary>
	/// The text that was removed.
	/// </summary>
	public ITextSource RemovedText { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Gets the new offset where the specified offset moves after this document change.
	/// </summary>
	public virtual int GetNewOffset(int offset, AnchorMovementType movementType = AnchorMovementType.Default)
	{
		if ((offset >= Offset) && (offset <= (Offset + RemovalLength)))
		{
			if (movementType == AnchorMovementType.BeforeInsertion)
			{
				return Offset;
			}
			return Offset + InsertionLength;
		}
		if (offset > Offset)
		{
			return (offset + InsertionLength) - RemovalLength;
		}
		return offset;
	}

	/// <summary>
	/// Creates TextChangeEventArgs for the reverse change.
	/// </summary>
	public virtual TextChangeEventArgs Invert()
	{
		return new TextChangeEventArgs(Offset, InsertedText, RemovedText);
	}

	#endregion
}