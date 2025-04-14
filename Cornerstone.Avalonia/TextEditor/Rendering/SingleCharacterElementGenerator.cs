#region References

using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;
using Cornerstone.Avalonia.TextEditor.Document;
using Cornerstone.Avalonia.TextEditor.Utils;
using LogicalDirection = Cornerstone.Avalonia.TextEditor.Document.LogicalDirection;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;
// This class is internal because it does not need to be accessed by the user - it can be configured using TextEditorOptions.

/// <summary>
/// Element generator that displays · for spaces and » for tabs and a box for control characters.
/// </summary>
/// <remarks>
/// This element generator is present in every TextView by default; the enabled features can be configured using the
/// <see cref="TextEditorSettings" />.
/// </remarks>
[SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace")]
internal sealed class SingleCharacterElementGenerator : VisualLineElementGenerator, IBuiltinElementGenerator
{
	#region Constructors

	/// <summary>
	/// Creates a new SingleCharacterElementGenerator instance.
	/// </summary>
	public SingleCharacterElementGenerator()
	{
		ShowSpaces = true;
		ShowTabs = true;
		ShowBoxForControlCharacters = true;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets/Sets whether to show a box with the hex code for control characters.
	/// </summary>
	public bool ShowBoxForControlCharacters { get; set; }

	/// <summary>
	/// Gets/Sets whether to show · for spaces.
	/// </summary>
	public bool ShowSpaces { get; set; }

	/// <summary>
	/// Gets/Sets whether to show » for tabs.
	/// </summary>
	public bool ShowTabs { get; set; }

	#endregion

	#region Methods

	public override VisualLineElement ConstructElement(int offset)
	{
		var c = CurrentContext.Document.GetCharAt(offset);

		if (ShowSpaces && (c == ' '))
		{
			var runProperties = new VisualLineElementTextRunProperties(CurrentContext.GlobalTextRunProperties);
			runProperties.SetForegroundBrush(CurrentContext.TextView.NonPrintableCharacterBrush);
			return new SpaceTextElement(CurrentContext.TextView.CachedElements.GetTextForNonPrintableCharacter(
				CurrentContext.TextView.Settings.ShowSpacesGlyph,
				runProperties));
		}
		if (ShowTabs && (c == '\t'))
		{
			var runProperties = new VisualLineElementTextRunProperties(CurrentContext.GlobalTextRunProperties);
			runProperties.SetForegroundBrush(CurrentContext.TextView.NonPrintableCharacterBrush);
			return new TabTextElement(CurrentContext.TextView.CachedElements.GetTextForNonPrintableCharacter(
				CurrentContext.TextView.Settings.ShowTabsGlyph,
				runProperties));
		}
		if (ShowBoxForControlCharacters && char.IsControl(c))
		{
			var runProperties = new VisualLineElementTextRunProperties(CurrentContext.GlobalTextRunProperties);
			runProperties.SetForegroundBrush(Brushes.White);
			var textFormatter = TextFormatterFactory.Create(CurrentContext.TextView);
			var text = FormattedTextElement.PrepareText(textFormatter, TextUtilities.GetControlCharacterName(c), runProperties);
			return new SpecialCharacterBoxElement(text);
		}

		return null;
	}

	public override int GetFirstInterestedOffset(int startOffset)
	{
		var endLine = CurrentContext.VisualLine.LastDocumentLine;
		var relevantText = CurrentContext.GetText(startOffset, endLine.EndIndex - startOffset);

		for (var i = 0; i < relevantText.Count; i++)
		{
			var c = relevantText.Text[relevantText.Offset + i];
			switch (c)
			{
				case ' ':
					if (ShowSpaces)
					{
						return startOffset + i;
					}
					break;
				case '\t':
					if (ShowTabs)
					{
						return startOffset + i;
					}
					break;
				default:
					if (ShowBoxForControlCharacters && char.IsControl(c))
					{
						return startOffset + i;
					}
					break;
			}
		}
		return -1;
	}

	void IBuiltinElementGenerator.FetchOptions(TextEditorSettings settings)
	{
		ShowSpaces = settings.ShowSpaces;
		ShowTabs = settings.ShowTabs;
		ShowBoxForControlCharacters = settings.ShowBoxForControlCharacters;
	}

	#endregion

	#region Classes

	internal sealed class SpecialCharacterTextRun : FormattedTextRun
	{
		#region Constants

		internal const double BoxMargin = 3;

		#endregion

		#region Fields

		private static readonly ISolidColorBrush DarkGrayBrush;

		#endregion

		#region Constructors

		public SpecialCharacterTextRun(FormattedTextElement element, TextRunProperties properties)
			: base(element, properties)
		{
		}

		static SpecialCharacterTextRun()
		{
			DarkGrayBrush = new ImmutableSolidColorBrush(Color.FromArgb(200, 128, 128, 128));
		}

		#endregion

		#region Properties

		public override Size Size
		{
			get
			{
				var s = base.Size;

				return s.WithWidth(s.Width + BoxMargin);
			}
		}

		#endregion

		#region Methods

		public override void Draw(DrawingContext drawingContext, Point origin)
		{
			var (x, y) = origin;

			var newOrigin = new Point(x + (BoxMargin / 2), y);

			var (width, height) = Size;

			var r = new Rect(x, y, width, height);

			drawingContext.FillRectangle(DarkGrayBrush, r, 2.5f);

			base.Draw(drawingContext, newOrigin);
		}

		#endregion
	}

	private sealed class SpaceTextElement : FormattedTextElement
	{
		#region Constructors

		public SpaceTextElement(TextLine textLine) : base(textLine, 1)
		{
		}

		#endregion

		#region Methods

		public override int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode)
		{
			if ((mode == CaretPositioningMode.Normal) || (mode == CaretPositioningMode.EveryCodepoint))
			{
				return base.GetNextCaretPosition(visualColumn, direction, mode);
			}
			return -1;
		}

		public override bool IsWhitespace(int visualColumn)
		{
			return true;
		}

		#endregion
	}

	private sealed class SpecialCharacterBoxElement : FormattedTextElement
	{
		#region Constructors

		public SpecialCharacterBoxElement(TextLine text) : base(text, 1)
		{
		}

		#endregion

		#region Methods

		public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
		{
			return new SpecialCharacterTextRun(this, TextRunProperties);
		}

		#endregion
	}

	private sealed class TabGlyphRun : DrawableTextRun
	{
		#region Fields

		private readonly TabTextElement _element;

		#endregion

		#region Constructors

		public TabGlyphRun(TabTextElement element, TextRunProperties properties)
		{
			if (properties == null)
			{
				throw new ArgumentNullException(nameof(properties));
			}
			Properties = properties;
			_element = element;
		}

		#endregion

		#region Properties

		public override double Baseline => _element.Text.Baseline;

		public override TextRunProperties Properties { get; }

		public override Size Size => new(_element.Text.WidthIncludingTrailingWhitespace * (_element.TabSize - 1), 0);

		#endregion

		#region Methods

		public override void Draw(DrawingContext drawingContext, Point origin)
		{
			_element.Text.Draw(drawingContext, origin);
		}

		#endregion
	}

	internal sealed class TabTextElement : VisualLineElement
	{
		#region Fields

		internal int TabSize;
		internal readonly TextLine Text;

		#endregion

		#region Constructors

		public TabTextElement(TextLine text) : base(2, 1)
		{
			Text = text;
		}

		#endregion

		#region Methods

		public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
		{
			// the TabTextElement consists of two TextRuns:
			// first a TabGlyphRun, then TextCharacters '\t' to let WPF handle the tab indentation
			if (startVisualColumn == VisualColumn)
			{
				return new TabGlyphRun(this, TextRunProperties);
			}
			if (startVisualColumn == (VisualColumn + 1))
			{
				return new TextCharacters("\t".AsMemory(), TextRunProperties);
			}
			throw new ArgumentOutOfRangeException(nameof(startVisualColumn));
		}

		public override int GetNextCaretPosition(int visualColumn, LogicalDirection direction, CaretPositioningMode mode)
		{
			if ((mode == CaretPositioningMode.Normal) || (mode == CaretPositioningMode.EveryCodepoint))
			{
				return base.GetNextCaretPosition(visualColumn, direction, mode);
			}
			return -1;
		}

		public override bool IsWhitespace(int visualColumn)
		{
			return true;
		}

		#endregion
	}

	#endregion
}