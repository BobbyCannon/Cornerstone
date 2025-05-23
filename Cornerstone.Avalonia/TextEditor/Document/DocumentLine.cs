﻿#region References

using System;
using System.Diagnostics;
using System.Globalization;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Document;

/// <summary>
/// Represents a line inside a <see cref="TextEditorDocument" />.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="TextEditorDocument.Lines" /> collection contains one DocumentLine instance
/// for every line in the document. This collection is read-only to user code and is automatically
/// updated to reflect the current document content.
/// </para>
/// <para>
/// Internally, the DocumentLine instances are arranged in a binary tree that allows for both efficient updates and lookup.
/// Converting between offset and line number is possible in O(lg N) time,
/// and the data structure also updates all offsets in O(lg N) whenever a line is inserted or removed.
/// </para>
/// </remarks>
public sealed partial class DocumentLine : IDocumentLine
{
	#region Fields

	private byte _delimiterLength;
	#if DEBUG
	// Required for thread safety check which is done only in debug builds.
	// To save space, we don't store the document reference in release builds as we don't need it there.
	private readonly TextEditorDocument _document;
	#endif

	private bool _isDeleted;

	private int _totalLength;

	#endregion

	#region Constructors

	internal DocumentLine(TextEditorDocument document)
	{
		#if DEBUG
		Debug.Assert(document != null);
		_document = document;
		#endif
	}

	#endregion

	#region Properties

	/// <summary>
	/// <para> Gets the length of the line delimiter. </para>
	/// <para>
	/// The value is 1 for single <c> "\r" </c> or <c> "\n" </c>, 2 for the <c> "\r\n" </c> sequence;
	/// and 0 for the last line in the document.
	/// </para>
	/// </summary>
	/// <remarks>
	/// This property is still available even if the line was deleted;
	/// in that case, it contains the line delimiters length before the deletion.
	/// </remarks>
	public int DelimiterLength
	{
		get
		{
			DebugVerifyAccess();
			return _delimiterLength;
		}
		internal set
		{
			Debug.Assert((value >= 0) && (value <= 2));
			_delimiterLength = (byte) value;
		}
	}

	/// <summary>
	/// Gets the end offset of the line in the document's text (the offset before the line delimiter).
	/// Runtime: O(log n)
	/// </summary>
	/// <exception cref="InvalidOperationException"> The line was deleted. </exception>
	/// <remarks> EndOffset = <see cref="StartIndex" /> + <see cref="Length" />. </remarks>
	public int EndIndex => StartIndex + Length;

	/// <summary>
	/// Gets if this line was deleted from the document.
	/// </summary>
	public bool IsDeleted
	{
		get
		{
			DebugVerifyAccess();
			return _isDeleted;
		}
		internal set => _isDeleted = value;
	}

	/// <summary>
	/// Gets the length of this line. The length does not include the line delimiter. O(1)
	/// </summary>
	/// <remarks>
	/// This property is still available even if the line was deleted;
	/// in that case, it contains the line's length before the deletion.
	/// </remarks>
	public int Length
	{
		get
		{
			DebugVerifyAccess();
			return _totalLength - _delimiterLength;
		}
	}

	/// <summary>
	/// Gets the number of this line.
	/// Runtime: O(log n)
	/// </summary>
	/// <exception cref="InvalidOperationException"> The line was deleted. </exception>
	public int LineNumber
	{
		get
		{
			if (IsDeleted)
			{
				throw new InvalidOperationException();
			}
			return DocumentLineTree.GetIndexFromNode(this) + 1;
		}
	}

	/// <summary>
	/// Gets the next line in the document.
	/// </summary>
	/// <returns> The line following this line, or null if this is the last line. </returns>
	public DocumentLine NextLine
	{
		get
		{
			DebugVerifyAccess();

			if (Right != null)
			{
				return Right.LeftMost;
			}
			var node = this;
			DocumentLine oldNode;
			do
			{
				oldNode = node;
				node = node.Parent;
				// we are on the way up from the right part, don't output node again
			} while ((node != null) && (node.Right == oldNode));
			return node;
		}
	}

	/// <summary>
	/// Gets the previous line in the document.
	/// </summary>
	/// <returns> The line before this line, or null if this is the first line. </returns>
	public DocumentLine PreviousLine
	{
		get
		{
			DebugVerifyAccess();

			if (Left != null)
			{
				return Left.RightMost;
			}
			var node = this;
			DocumentLine oldNode;
			do
			{
				oldNode = node;
				node = node.Parent;
				// we are on the way up from the left part, don't output node again
			} while ((node != null) && (node.Left == oldNode));
			return node;
		}
	}

	/// <summary>
	/// Gets the starting offset of the line in the document's text.
	/// Runtime: O(log n)
	/// </summary>
	/// <exception cref="InvalidOperationException"> The line was deleted. </exception>
	public int StartIndex
	{
		get
		{
			if (IsDeleted)
			{
				throw new InvalidOperationException();
			}
			return DocumentLineTree.GetOffsetFromNode(this);
		}
	}

	/// <summary>
	/// Gets the length of this line, including the line delimiter. O(1)
	/// </summary>
	/// <remarks>
	/// This property is still available even if the line was deleted;
	/// in that case, it contains the line's length before the deletion.
	/// </remarks>
	public int TotalLength
	{
		get
		{
			DebugVerifyAccess();
			return _totalLength;
		}
		// this is set by DocumentLineTree
		internal set => _totalLength = value;
	}

	IDocumentLine IDocumentLine.NextLine => NextLine;

	IDocumentLine IDocumentLine.PreviousLine => PreviousLine;

	#endregion

	#region Methods

	/// <summary>
	/// Gets a string with debug output showing the line number and offset.
	/// Does not include the line's text.
	/// </summary>
	public override string ToString()
	{
		if (IsDeleted)
		{
			return "[DocumentLine deleted]";
		}
		return string.Format(
			CultureInfo.InvariantCulture,
			"[DocumentLine Number={0} Offset={1} Length={2}]", LineNumber, StartIndex, Length);
	}

	[Conditional("DEBUG")]
	private void DebugVerifyAccess()
	{
		#if DEBUG
		_document.DebugVerifyAccess();
		#endif
	}

	#endregion
}

/// <summary>
/// A line inside a <see cref="ITextEditorDocument" />.
/// </summary>
public interface IDocumentLine : IRange
{
	#region Properties

	/// <summary>
	/// Gets the length of the line terminator.
	/// Returns 1 or 2; or 0 at the end of the document.
	/// </summary>
	int DelimiterLength { get; }

	/// <summary>
	/// Gets whether the line was deleted.
	/// </summary>
	bool IsDeleted { get; }

	/// <summary>
	/// Gets the number of this line.
	/// The first line has the number 1.
	/// </summary>
	int LineNumber { get; }

	/// <summary>
	/// Gets the next line. Returns null if this is the last line in the document.
	/// </summary>
	IDocumentLine NextLine { get; }

	/// <summary>
	/// Gets the previous line. Returns null if this is the first line in the document.
	/// </summary>
	IDocumentLine PreviousLine { get; }

	/// <summary>
	/// Gets the length of this line, including the line delimiter.
	/// </summary>
	int TotalLength { get; }

	#endregion
}