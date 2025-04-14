#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using Cornerstone.Avalonia.TextEditor.Document;
using Cornerstone.Avalonia.TextEditor.Utils;
using Cornerstone.Collections;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Highlighting;

/// <summary>
/// Represents a highlighted document line.
/// </summary>
public class HighlightedLine
{
	#region Constructors

	/// <summary>
	/// Creates a new HighlightedLine instance.
	/// </summary>
	public HighlightedLine(ITextEditorDocument document, IDocumentLine documentLine)
	{
		//if (!document.Lines.Contains(documentLine))
		//	throw new ArgumentException("Line is null or not part of document");
		Document = document ?? throw new ArgumentNullException(nameof(document));
		DocumentLine = documentLine;
		Sections = new NullSafeCollection<HighlightedSection>();
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the document associated with this HighlightedLine.
	/// </summary>
	public ITextEditorDocument Document { get; }

	/// <summary>
	/// Gets the document line associated with this HighlightedLine.
	/// </summary>
	public IDocumentLine DocumentLine { get; }

	/// <summary>
	/// Gets the highlighted sections.
	/// The sections are not overlapping, but they may be nested.
	/// In that case, outer sections come in the list before inner sections.
	/// The sections are sorted by start offset.
	/// </summary>
	public IList<HighlightedSection> Sections { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Merges the additional line into this line.
	/// </summary>
	public void MergeWith(HighlightedLine additionalLine)
	{
		if (additionalLine == null)
		{
			return;
		}
		#if DEBUG
		ValidateInvariants();
		additionalLine.ValidateInvariants();
		#endif

		var pos = 0;
		var activeSectionEndOffsets = new Stack<int>();
		var lineEndOffset = DocumentLine.EndIndex;
		activeSectionEndOffsets.Push(lineEndOffset);
		foreach (var newSection in additionalLine.Sections)
		{
			var newSectionStart = newSection.StartIndex;
			// Track the existing sections using the stack, up to the point where
			// we need to insert the first part of the newSection
			while (pos < Sections.Count)
			{
				var s = Sections[pos];
				if (newSection.StartIndex < s.StartIndex)
				{
					break;
				}
				while (s.StartIndex > activeSectionEndOffsets.Peek())
				{
					activeSectionEndOffsets.Pop();
				}
				activeSectionEndOffsets.Push(s.StartIndex + s.Length);
				pos++;
			}
			// Now insert the new section
			// Create a copy of the stack so that we can track the sections we traverse
			// during the insertion process:
			var insertionStack = new Stack<int>(activeSectionEndOffsets.Reverse());
			// The stack enumerator reverses the order of the elements, so we call Reverse() to restore
			// the original order.
			int i;
			for (i = pos; i < Sections.Count; i++)
			{
				var s = Sections[i];
				if ((newSection.StartIndex + newSection.Length) <= s.StartIndex)
				{
					break;
				}
				// Insert a segment in front of s:
				Insert(ref i, ref newSectionStart, s.StartIndex, newSection.Color, insertionStack);

				while (s.StartIndex > insertionStack.Peek())
				{
					insertionStack.Pop();
				}
				insertionStack.Push(s.StartIndex + s.Length);
			}
			Insert(ref i, ref newSectionStart, newSection.StartIndex + newSection.Length, newSection.Color, insertionStack);
		}

		#if DEBUG
		ValidateInvariants();
		#endif
	}

	///// <summary>
	///// Produces HTML code for the line, with &lt;span class="colorName"&gt; tags.
	///// </summary>
	public string ToHtml(HtmlOptions options = null)
	{
		var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
		using (var htmlWriter = new HtmlRichTextWriter(stringWriter, options))
		{
			WriteTo(htmlWriter);
		}
		return stringWriter.ToString();
	}

	/// <summary>
	/// Produces HTML code for a section of the line, with &lt;span class="colorName"&gt; tags.
	/// </summary>
	public string ToHtml(int startOffset, int endOffset, HtmlOptions options = null)
	{
		var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
		using (var htmlWriter = new HtmlRichTextWriter(stringWriter, options))
		{
			WriteTo(htmlWriter, startOffset, endOffset);
		}
		return stringWriter.ToString();
	}

	/// <summary>
	/// Creates a <see cref="RichText" /> that stores the text and highlighting of this line.
	/// </summary>
	public RichText ToRichText()
	{
		return new RichText(Document.GetText(DocumentLine), ToRichTextModel());
	}

	/// <summary>
	/// Creates a <see cref="RichTextModel" /> that stores the highlighting of this line.
	/// </summary>
	public RichTextModel ToRichTextModel()
	{
		var builder = new RichTextModel();
		var startOffset = DocumentLine.StartIndex;
		foreach (var section in Sections)
		{
			builder.ApplyHighlighting(section.StartIndex - startOffset, section.Length, section.Color);
		}
		return builder;
	}

	///// <inheritdoc/>
	public override string ToString()
	{
		return "[" + GetType().Name + " " + ToHtml() + "]";
	}

	/// <summary>
	/// Validates that the sections are sorted correctly, and that they are not overlapping.
	/// </summary>
	/// <seealso cref="Sections" />
	public void ValidateInvariants()
	{
		var line = this;
		var lineStartOffset = line.DocumentLine.StartIndex;
		var lineEndOffset = line.DocumentLine.EndIndex;
		for (var i = 0; i < line.Sections.Count; i++)
		{
			var s1 = line.Sections[i];
			if ((s1.StartIndex < lineStartOffset) || (s1.Length < 0) || ((s1.StartIndex + s1.Length) > lineEndOffset))
			{
				throw new InvalidOperationException("Section is outside line bounds");
			}
			for (var j = i + 1; j < line.Sections.Count; j++)
			{
				var s2 = line.Sections[j];
				if (s2.StartIndex >= (s1.StartIndex + s1.Length))
				{
					// s2 is after s1
				}
				else if ((s2.StartIndex >= s1.StartIndex) && ((s2.StartIndex + s2.Length) <= (s1.StartIndex + s1.Length)))
				{
					// s2 is nested within s1
				}
				else
				{
					throw new InvalidOperationException("Sections are overlapping or incorrectly sorted.");
				}
			}
		}
	}

	/// <summary>
	/// Writes the highlighted line to the RichTextWriter.
	/// </summary>
	internal void WriteTo(RichTextWriter writer)
	{
		var startOffset = DocumentLine.StartIndex;
		WriteTo(writer, startOffset, startOffset + DocumentLine.Length);
	}

	/// <summary>
	/// Writes a part of the highlighted line to the RichTextWriter.
	/// </summary>
	internal void WriteTo(RichTextWriter writer, int startOffset, int endOffset)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		var documentLineStartOffset = DocumentLine.StartIndex;
		var documentLineEndOffset = documentLineStartOffset + DocumentLine.Length;
		if ((startOffset < documentLineStartOffset) || (startOffset > documentLineEndOffset))
		{
			throw new ArgumentOutOfRangeException("startOffset", startOffset, "Value must be between " + documentLineStartOffset + " and " + documentLineEndOffset);
		}
		if ((endOffset < startOffset) || (endOffset > documentLineEndOffset))
		{
			throw new ArgumentOutOfRangeException("endOffset", endOffset, "Value must be between startOffset and " + documentLineEndOffset);
		}
		IRange requestedRange = new SimpleRange(startOffset, endOffset - startOffset);

		var elements = new List<HtmlElement>();
		for (var i = 0; i < Sections.Count; i++)
		{
			var s = Sections[i];
			if (s.GetOverlap(requestedRange).Length > 0)
			{
				elements.Add(new HtmlElement(s.StartIndex, i, false, s.Color));
				elements.Add(new HtmlElement(s.StartIndex + s.Length, i, true, s.Color));
			}
		}
		elements.Sort();

		var document = Document;
		var textOffset = startOffset;
		foreach (var e in elements)
		{
			var newOffset = Math.Min(e.Offset, endOffset);
			if (newOffset > startOffset)
			{
				document.WriteTextTo(writer, textOffset, newOffset - textOffset);
			}
			textOffset = Math.Max(textOffset, newOffset);
			if (e.IsEnd)
			{
				writer.EndSpan();
			}
			else
			{
				writer.BeginSpan(e.Color);
			}
		}
		document.WriteTextTo(writer, textOffset, endOffset - textOffset);
	}

	private void Insert(ref int pos, ref int newSectionStart, int insertionEndPos, HighlightingColor color, Stack<int> insertionStack)
	{
		if (newSectionStart >= insertionEndPos)
		{
			// nothing to insert here
			return;
		}

		while (insertionStack.Peek() <= newSectionStart)
		{
			insertionStack.Pop();
		}
		while (insertionStack.Peek() < insertionEndPos)
		{
			var end = insertionStack.Pop();
			// insert the portion from newSectionStart to end
			if (end > newSectionStart)
			{
				Sections.Insert(pos++, new HighlightedSection
				{
					StartIndex = newSectionStart,
					Length = end - newSectionStart,
					Color = color
				});
				newSectionStart = end;
			}
		}
		if (insertionEndPos > newSectionStart)
		{
			Sections.Insert(pos++, new HighlightedSection
			{
				StartIndex = newSectionStart,
				Length = insertionEndPos - newSectionStart,
				Color = color
			});
			newSectionStart = insertionEndPos;
		}
	}

	#endregion

	#region Classes

	private sealed class HtmlElement : IComparable<HtmlElement>
	{
		#region Fields

		internal readonly HighlightingColor Color;
		internal readonly bool IsEnd;
		internal readonly int Nesting;
		internal readonly int Offset;

		#endregion

		#region Constructors

		public HtmlElement(int offset, int nesting, bool isEnd, HighlightingColor color)
		{
			Offset = offset;
			Nesting = nesting;
			IsEnd = isEnd;
			Color = color;
		}

		#endregion

		#region Methods

		[SuppressMessage("ReSharper", "ImpureMethodCallOnReadonlyValueField")]
		public int CompareTo(HtmlElement other)
		{
			var r = Offset.CompareTo(other.Offset);
			if (r != 0)
			{
				return r;
			}
			if (IsEnd != other.IsEnd)
			{
				if (IsEnd)
				{
					return -1;
				}
				return 1;
			}
			return IsEnd ? other.Nesting.CompareTo(Nesting) : Nesting.CompareTo(other.Nesting);
		}

		#endregion
	}

	#endregion
}