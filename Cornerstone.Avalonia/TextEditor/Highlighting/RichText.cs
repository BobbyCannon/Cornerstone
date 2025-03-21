#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Highlighting;

/// <summary>
/// Represents a immutable piece text with highlighting information.
/// </summary>
public class RichText
{
	#region Fields

	/// <summary>
	/// The empty string without any formatting information.
	/// </summary>
	public static readonly RichText Empty = new(string.Empty);

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a RichText instance with the given text and RichTextModel.
	/// </summary>
	/// <param name="text">
	/// The text to use in this RichText instance.
	/// </param>
	/// <param name="model">
	/// The model that contains the formatting to use for this RichText instance.
	/// <c> model.DocumentLength </c> should correspond to <c> text.Length </c>.
	/// This parameter may be null, in which case the RichText instance just holds plain text.
	/// </param>
	public RichText(string text, RichTextModel model = null)
	{
		Text = text ?? throw new ArgumentNullException(nameof(text));
		if (model != null)
		{
			var sections = model.GetHighlightedSections(0, text.Length).ToArray();
			StateChangeOffsets = new int[sections.Length];
			StateChanges = new HighlightingColor[sections.Length];
			for (var i = 0; i < sections.Length; i++)
			{
				StateChangeOffsets[i] = sections[i].StartIndex;
				StateChanges[i] = sections[i].Color;
			}
		}
		else
		{
			StateChangeOffsets = [0];
			StateChanges = [HighlightingColor.Empty];
		}
	}

	internal RichText(string text, int[] offsets, HighlightingColor[] states)
	{
		Text = text;
		Debug.Assert(offsets[0] == 0);
		Debug.Assert(offsets.Last() <= text.Length);
		StateChangeOffsets = offsets;
		StateChanges = states;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the text length.
	/// </summary>
	public int Length => Text.Length;

	/// <summary>
	/// Gets the text.
	/// </summary>
	public string Text { get; }

	internal int[] StateChangeOffsets { get; }

	internal HighlightingColor[] StateChanges { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Concatenates the specified rich texts.
	/// </summary>
	public static RichText Concat(params RichText[] texts)
	{
		if ((texts == null) || (texts.Length == 0))
		{
			return Empty;
		}
		if (texts.Length == 1)
		{
			return texts[0];
		}
		var newText = string.Concat(texts.Select(txt => txt.Text));
		var model = texts[0].ToRichTextModel();
		var offset = texts[0].Length;
		for (var i = 1; i < texts.Length; i++)
		{
			model.Append(offset, texts[i].StateChangeOffsets, texts[i].StateChanges);
			offset += texts[i].Length;
		}
		return new RichText(newText, model);
	}

	/// <summary>
	/// Retrieves the highlighted sections in the specified range.
	/// The highlighted sections will be sorted by offset, and there will not be any nested or overlapping sections.
	/// </summary>
	public IEnumerable<HighlightedSection> GetHighlightedSections(int offset, int length)
	{
		var index = GetIndexForOffset(offset);
		var pos = offset;
		var endOffset = offset + length;
		while (pos < endOffset)
		{
			var endPos = Math.Min(endOffset, GetEnd(index));
			yield return new HighlightedSection
			{
				StartIndex = pos,
				Length = endPos - pos,
				Color = StateChanges[index]
			};
			pos = endPos;
			index++;
		}
	}

	/// <summary>
	/// Gets the HighlightingColor for the specified offset.
	/// </summary>
	public HighlightingColor GetHighlightingAt(int offset)
	{
		return StateChanges[GetIndexForOffset(offset)];
	}

	/// <summary>
	/// Concatenates the specified rich texts.
	/// </summary>
	public static RichText operator +(RichText a, RichText b)
	{
		return Concat(a, b);
	}

	/// <summary>
	/// Implicit conversion from string to RichText.
	/// </summary>
	public static implicit operator RichText(string text)
	{
		if (text != null)
		{
			return new RichText(text);
		}
		return null;
	}

	///// <summary>
	///// Creates Run instances that can be used for TextBlock.Inlines.
	///// </summary>
	//public Run[] CreateRuns()
	//{
	//	Run[] runs = new Run[stateChanges.Length];
	//	for (int i = 0; i < runs.Length; i++) {
	//		int startOffset = stateChangeOffsets[i];
	//		int endOffset = i + 1 < stateChangeOffsets.Length ? stateChangeOffsets[i + 1] : text.Length;
	//		Run r = new Run(text.Substring(startOffset, endOffset - startOffset));
	//		HighlightingColor state = stateChanges[i];
	//		ApplyColorToTextElement(r, state);
	//		runs[i] = r;
	//	}
	//	return runs;
	//}

	//internal static void ApplyColorToTextElement(TextElement r, HighlightingColor state)
	//{
	//	if (state.Foreground != null)
	//		r.Foreground = state.Foreground.GetBrush(null);
	//	if (state.Background != null)
	//		r.Background = state.Background.GetBrush(null);
	//	if (state.FontWeight != null)
	//		r.FontWeight = state.FontWeight.Value;
	//	if (state.FontStyle != null)
	//		r.FontStyle = state.FontStyle.Value;
	//}

	///// <summary>
	///// Produces HTML code for the line, with &lt;span style="..."&gt; tags.
	///// </summary>
	//public string ToHtml(HtmlOptions options = null)
	//{
	//	StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
	//	using (var htmlWriter = new HtmlRichTextWriter(stringWriter, options)) {
	//		htmlWriter.Write(this);
	//	}
	//	return stringWriter.ToString();
	//}

	///// <summary>
	///// Produces HTML code for a section of the line, with &lt;span style="..."&gt; tags.
	///// </summary>
	//public string ToHtml(int offset, int length, HtmlOptions options = null)
	//{
	//	StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
	//	using (var htmlWriter = new HtmlRichTextWriter(stringWriter, options)) {
	//		htmlWriter.Write(this, offset, length);
	//	}
	//	return stringWriter.ToString();
	//}

	/// <summary>
	/// Creates a substring of this rich text.
	/// </summary>
	public RichText Substring(int offset, int length)
	{
		if ((offset == 0) && (length == Length))
		{
			return this;
		}
		var newText = Text.Substring(offset, length);
		var model = ToRichTextModel();
		var map = new OffsetChangeMap(2)
		{
			new(offset + length, Text.Length - offset - length, 0),
			new(0, offset, 0)
		};
		model.UpdateOffsets(map);
		return new RichText(newText, model);
	}

	/// <summary>
	/// Creates a new RichTextModel with the formatting from this RichText.
	/// </summary>
	public RichTextModel ToRichTextModel()
	{
		return new RichTextModel(StateChangeOffsets, StateChanges.Select(ch => ch.Clone()).ToArray());
	}

	/// <summary>
	/// Gets the text.
	/// </summary>
	public override string ToString()
	{
		return Text;
	}

	private int GetEnd(int index)
	{
		// Gets the end of the color segment no. index.
		if ((index + 1) < StateChangeOffsets.Length)
		{
			return StateChangeOffsets[index + 1];
		}
		return Text.Length;
	}

	private int GetIndexForOffset(int offset)
	{
		if ((offset < 0) || (offset > Text.Length))
		{
			throw new ArgumentOutOfRangeException(nameof(offset));
		}
		var index = Array.BinarySearch(StateChangeOffsets, offset);
		if (index < 0)
		{
			// If no color change exists directly at offset,
			// return the index of the color segment that contains offset.
			index = ~index - 1;
		}
		return index;
	}

	#endregion
}