#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Input;
using Cornerstone.Internal;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Editing;

/// <summary>
/// Base class for selections.
/// </summary>
public abstract class Selection
{
	#region Constructors

	/// <summary>
	/// Constructor for Selection.
	/// </summary>
	protected Selection(TextArea textArea)
	{
		TextArea = textArea ?? throw new ArgumentNullException(nameof(textArea));
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets whether virtual space is enabled for this selection.
	/// </summary>
	public virtual bool EnableVirtualSpace => TextArea.Options.EnableVirtualSpace;

	/// <summary>
	/// Gets the end position of the selection.
	/// </summary>
	public abstract TextViewPosition EndPosition { get; }

	/// <summary>
	/// Gets whether the selection is empty.
	/// </summary>
	public virtual bool IsEmpty => Length == 0;

	/// <summary>
	/// Gets whether the selection is multi-line.
	/// </summary>
	public virtual bool IsMultiline
	{
		get
		{
			var surroundingSegment = SurroundingSegment;
			if (surroundingSegment == null)
			{
				return false;
			}
			var start = surroundingSegment.Offset;
			var end = start + surroundingSegment.Length;
			var document = TextArea.Document;
			if (document == null)
			{
				throw ThrowUtil.NoDocumentAssigned();
			}
			return document.GetLineByOffset(start) != document.GetLineByOffset(end);
		}
	}

	/// <summary>
	/// Gets the selection length.
	/// </summary>
	public abstract int Length { get; }

	/// <summary>
	/// Gets the selected text segments.
	/// </summary>
	public abstract IEnumerable<SelectionSegment> Segments { get; }

	/// <summary>
	/// Gets the start position of the selection.
	/// </summary>
	public abstract TextViewPosition StartPosition { get; }

	/// <summary>
	/// Gets the smallest segment that contains all segments in this selection.
	/// May return null if the selection is empty.
	/// </summary>
	public abstract ISegment SurroundingSegment { get; }

	internal TextArea TextArea { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Gets whether the specified offset is included in the selection.
	/// </summary>
	/// <returns>
	/// True, if the selection contains the offset (selection borders inclusive);
	/// otherwise, false.
	/// </returns>
	public virtual bool Contains(int offset)
	{
		if (IsEmpty)
		{
			return false;
		}

		return SurroundingSegment.Contains(offset, 0) &&
			Segments.Any(s => s.Contains(offset, 0));
	}

	/// <summary>
	/// Creates a new simple selection that selects the text from startOffset to endOffset.
	/// </summary>
	public static Selection Create(TextArea textArea, int startOffset, int endOffset)
	{
		if (textArea == null)
		{
			throw new ArgumentNullException(nameof(textArea));
		}
		if (startOffset == endOffset)
		{
			return textArea.EmptySelection;
		}
		return new SimpleSelection(textArea,
			new TextViewPosition(textArea.Document.GetLocation(startOffset)),
			new TextViewPosition(textArea.Document.GetLocation(endOffset)));
	}

	/// <summary>
	/// Creates a new simple selection that selects the text in the specified segment.
	/// </summary>
	public static Selection Create(TextArea textArea, ISegment segment)
	{
		if (segment == null)
		{
			throw new ArgumentNullException(nameof(segment));
		}
		return Create(textArea, segment.Offset, segment.EndOffset);
	}

	/// <inheritdoc />
	public abstract override bool Equals(object obj);

	/// <inheritdoc />
	public abstract override int GetHashCode();

	/// <summary>
	/// Gets the selected text.
	/// </summary>
	public virtual string GetText()
	{
		var document = TextArea.Document;
		if (document == null)
		{
			throw ThrowUtil.NoDocumentAssigned();
		}
		StringBuilder b = null;
		string text = null;
		foreach (var s in Segments)
		{
			if (text != null)
			{
				if (b == null)
				{
					b = new StringBuilder(text);
				}
				else
				{
					b.Append(text);
				}
			}
			text = document.GetText(s);
		}
		if (b != null)
		{
			if (text != null)
			{
				b.Append(text);
			}
			return b.ToString();
		}
		return text ?? string.Empty;
	}

	/// <summary>
	/// Replaces the selection with the specified text.
	/// </summary>
	public abstract void ReplaceSelectionWithText(string newText);

	/// <summary>
	/// Returns a new selection with the changed end point.
	/// </summary>
	/// <exception cref="NotSupportedException"> Cannot set endpoint for empty selection </exception>
	public abstract Selection SetEndpoint(TextViewPosition endPosition);

	/// <summary>
	/// If this selection is empty, starts a new selection from <paramref name="startPosition" /> to
	/// <paramref name="endPosition" />, otherwise, changes the endpoint of this selection.
	/// </summary>
	public abstract Selection StartSelectionOrSetEndpoint(TextViewPosition startPosition, TextViewPosition endPosition);

	/// <summary>
	/// Updates the selection when the document changes.
	/// </summary>
	public abstract Selection UpdateOnDocumentChange(DocumentChangeEventArgs e);

	internal string AddSpacesIfRequired(string newText, TextViewPosition start, TextViewPosition end)
	{
		if (EnableVirtualSpace && InsertVirtualSpaces(newText, start, end))
		{
			var line = TextArea.Document.GetLineByNumber(start.Line);
			var lineText = TextArea.Document.GetText(line);
			var vLine = TextArea.TextView.GetOrConstructVisualLine(line);
			var colDiff = start.VisualColumn - vLine.VisualLengthWithEndOfLineMarker;
			if (colDiff > 0)
			{
				var additionalSpaces = "";
				if (!TextArea.Options.ConvertTabsToSpaces && (lineText.Trim('\t').Length == 0))
				{
					var tabCount = colDiff / TextArea.Options.IndentationSize;
					additionalSpaces = new string('\t', tabCount);
					colDiff -= tabCount * TextArea.Options.IndentationSize;
				}
				additionalSpaces += new string(' ', colDiff);
				return additionalSpaces + newText;
			}
		}
		return newText;
	}

	internal static Selection Create(TextArea textArea, TextViewPosition start, TextViewPosition end)
	{
		if (textArea == null)
		{
			throw new ArgumentNullException(nameof(textArea));
		}
		if ((textArea.Document.GetOffset(start.Location) == textArea.Document.GetOffset(end.Location)) && (start.VisualColumn == end.VisualColumn))
		{
			return textArea.EmptySelection;
		}
		return new SimpleSelection(textArea, start, end);
	}

	private bool InsertVirtualSpaces(string newText, TextViewPosition start, TextViewPosition end)
	{
		return (!string.IsNullOrEmpty(newText) || !(IsInVirtualSpace(start) && IsInVirtualSpace(end)))
			&& (newText != "\r\n")
			&& (newText != "\n")
			&& (newText != "\r");
	}

	private bool IsInVirtualSpace(TextViewPosition pos)
	{
		return pos.VisualColumn > TextArea.TextView.GetOrConstructVisualLine(TextArea.Document.GetLineByNumber(pos.Line)).VisualLength;
	}

	#endregion

	/// <summary>
	/// Creates a data object containing the selection's text.
	/// </summary>
	public virtual DataObject CreateDataObject(TextArea textArea)
	{
		var data = new DataObject();
		// Ensure we use the appropriate newline sequence for the OS
		var text = TextUtilities.NormalizeNewLines(GetText(), Environment.NewLine);
		data.Set(DataFormats.Text, text);
		return data;
	}
}