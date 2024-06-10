#region References

using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Cornerstone.Avalonia.AvaloniaEdit.Utils;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Rendering;

/// <summary>
/// Formatted text (not normal document text).
/// This is used as base class for various VisualLineElements that are displayed using a
/// FormattedText, for example newline markers or collapsed folding sections.
/// </summary>
public class FormattedTextElement : VisualLineElement
{
	#region Fields

	internal readonly FormattedText FormattedText;
	internal string Text;
	internal TextLine TextLine;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new FormattedTextElement that displays the specified text
	/// and occupies the specified length in the document.
	/// </summary>
	public FormattedTextElement(string text, int documentLength) : base(1, documentLength)
	{
		Text = text ?? throw new ArgumentNullException(nameof(text));
	}

	/// <summary>
	/// Creates a new FormattedTextElement that displays the specified text
	/// and occupies the specified length in the document.
	/// </summary>
	public FormattedTextElement(TextLine text, int documentLength) : base(1, documentLength)
	{
		TextLine = text ?? throw new ArgumentNullException(nameof(text));
	}

	/// <summary>
	/// Creates a new FormattedTextElement that displays the specified text
	/// and occupies the specified length in the document.
	/// </summary>
	public FormattedTextElement(FormattedText text, int documentLength) : base(1, documentLength)
	{
		FormattedText = text ?? throw new ArgumentNullException(nameof(text));
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
	{
		if (TextLine == null)
		{
			var formatter = TextFormatterFactory.Create(context.TextView);
			TextLine = PrepareText(formatter, Text, TextRunProperties);
			Text = null;
		}
		return new FormattedTextRun(this, TextRunProperties);
	}

	/// <summary>
	/// Constructs a TextLine from a simple text.
	/// </summary>
	public static TextLine PrepareText(TextFormatter formatter, string text, TextRunProperties properties)
	{
		if (formatter == null)
		{
			throw new ArgumentNullException(nameof(formatter));
		}
		if (text == null)
		{
			throw new ArgumentNullException(nameof(text));
		}
		if (properties == null)
		{
			throw new ArgumentNullException(nameof(properties));
		}
		return formatter.FormatLine(
			new SimpleTextSource(text, properties),
			0,
			32000,
			new VisualLineTextParagraphProperties
			{
				defaultTextRunProperties = properties,
				textWrapping = TextWrapping.NoWrap,
				tabSize = 40
			});
	}

	#endregion
}

/// <summary>
/// This is the TextRun implementation used by the <see cref="FormattedTextElement" /> class.
/// </summary>
public class FormattedTextRun : DrawableTextRun
{
	#region Constructors

	/// <summary>
	/// Creates a new FormattedTextRun.
	/// </summary>
	public FormattedTextRun(FormattedTextElement element, TextRunProperties properties)
	{
		if (properties == null)
		{
			throw new ArgumentNullException(nameof(properties));
		}
		Properties = properties;
		Element = element ?? throw new ArgumentNullException(nameof(element));
	}

	#endregion

	#region Properties

	public override double Baseline => Element.FormattedText?.Baseline ?? Element.TextLine.Baseline;

	/// <summary>
	/// Gets the element for which the FormattedTextRun was created.
	/// </summary>
	public FormattedTextElement Element { get; }

	/// <inheritdoc />
	public override TextRunProperties Properties { get; }

	/// <inheritdoc />
	public override Size Size
	{
		get
		{
			var formattedText = Element.FormattedText;

			if (formattedText != null)
			{
				return new Size(formattedText.WidthIncludingTrailingWhitespace, formattedText.Height);
			}

			var text = Element.TextLine;
			return new Size(text.WidthIncludingTrailingWhitespace, text.Height);
		}
	}

	public override ReadOnlyMemory<char> Text { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Draw(DrawingContext drawingContext, Point origin)
	{
		if (Element.FormattedText != null)
		{
			//var y = origin.Y - Element.FormattedText.Baseline;
			drawingContext.DrawText(Element.FormattedText, origin);
		}
		else
		{
			//var y = origin.Y - Element.TextLine.Baseline;
			Element.TextLine.Draw(drawingContext, origin);
		}
	}

	#endregion
}