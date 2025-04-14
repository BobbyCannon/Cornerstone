#region References

using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Cornerstone.Avalonia.TextEditor.Document;
using Cornerstone.Avalonia.TextEditor.Rendering;
using Cornerstone.Avalonia.TextEditor.Utils;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Editing;

/// <summary>
/// Margin showing line numbers.
/// </summary>
public class LineNumberMargin : AbstractMargin
{
	#region Fields

	/// <summary>
	/// Defines the <see cref="FontSize" /> property.
	/// </summary>
	public static readonly AttachedProperty<double> FontSizeProperty =
		AvaloniaProperty.RegisterAttached<LineNumberMargin, LineNumberMargin, double>(nameof(FontSize), 12, true, coerce: Coerce);

	/// <summary>
	/// Maximum length of a line number, in characters
	/// </summary>
	protected int MaxLineNumberLength;

	private bool _selecting;
	private AnchorRange _selectionStart;

	#endregion

	#region Constructors

	public LineNumberMargin()
	{
		using var stream = AssetLoader.Open(new Uri("avares://Cornerstone.Avalonia/Resources/RightArrow.cur"));
		using var bitmap = new Bitmap(stream);

		Cursor = new Cursor(bitmap, new PixelPoint(12,0));
		MaxLineNumberLength = 1;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets or sets the font size.
	/// </summary>
	public double FontSize
	{
		get => GetValue(FontSizeProperty);
		set => SetValue(FontSizeProperty, value);
	}

	/// <summary>
	/// The typeface used for rendering the line number margin.
	/// This field is calculated in MeasureOverride() based on the FontFamily etc. properties.
	/// </summary>
	protected Typeface Typeface { get; set; }

	#endregion

	#region Methods

	public override void Render(DrawingContext drawingContext)
	{
		var textView = TextView;
		var renderSize = Bounds.Size;
		var foreground = GetValue(TextElement.ForegroundProperty);

		if (textView is { VisualLinesValid: true })
		{
			// this is necessary so hit-testing works properly and events get tunneled to the TextView.
			drawingContext.FillRectangle(Brushes.Transparent, Bounds);

			foreach (var line in textView.VisualLines)
			{
				if (line.FirstDocumentLine.IsDeleted)
				{
					continue;
				}

				var lineNumber = line.FirstDocumentLine.LineNumber;
				var text = TextFormatterFactory.CreateFormattedText(
					this,
					lineNumber.ToString(CultureInfo.CurrentCulture),
					Typeface,
					FontSize,
					foreground
				);
				var y = line.GetTextLineVisualYPosition(line.TextLines[0], VisualYPosition.TextTop);
				drawingContext.DrawText(text, new Point(renderSize.Width - text.Width, y - textView.VerticalOffset));
			}
		}
	}

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
	{
		Typeface = this.CreateTypeface();

		var text = TextFormatterFactory.CreateFormattedText(
			this,
			new string('9', MaxLineNumberLength + 1),
			Typeface,
			FontSize,
			GetValue(TextElement.ForegroundProperty)
		);

		return new Size(text.Width, 0);
	}

	/// <inheritdoc />
	protected override void OnDocumentChanged(TextEditorDocument oldDocument, TextEditorDocument newDocument)
	{
		if (oldDocument != null)
		{
			TextDocumentWeakEventManager.LineCountChanged.RemoveHandler(oldDocument, OnDocumentLineCountChanged);
		}
		base.OnDocumentChanged(oldDocument, newDocument);
		if (newDocument != null)
		{
			TextDocumentWeakEventManager.LineCountChanged.AddHandler(newDocument, OnDocumentLineCountChanged);
		}
		OnDocumentLineCountChanged();
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		if (_selecting && (TextArea != null) && (TextView != null))
		{
			e.Handled = true;
			var currentSeg = GetTextLineSegment(e);
			if (currentSeg == SegmentExtensions.Invalid)
			{
				return;
			}
			ExtendSelection(currentSeg);
			TextArea.Caret.BringCaretToView(0);
		}
		base.OnPointerMoved(e);
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);

		if (!e.Handled && (TextView != null) && (TextArea != null))
		{
			e.Handled = true;
			TextArea.Focus();

			var currentSeg = GetTextLineSegment(e);
			if (currentSeg == SegmentExtensions.Invalid)
			{
				return;
			}
			TextArea.Caret.Offset = currentSeg.Offset + currentSeg.Length;
			e.Pointer.Capture(this);
			if (e.Pointer.Captured == this)
			{
				_selecting = true;
				_selectionStart = new AnchorRange(Document, currentSeg.Offset, currentSeg.Length);
				if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
				{
					if (TextArea.Selection is SimpleSelection simpleSelection)
					{
						_selectionStart = new AnchorRange(Document, simpleSelection.SurroundingRange);
					}
				}
				TextArea.Selection = Selection.Create(TextArea, _selectionStart);
				if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
				{
					ExtendSelection(currentSeg);
				}
				TextArea.Caret.BringCaretToView(0);
			}
		}
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		if (_selecting)
		{
			_selecting = false;
			_selectionStart = null;
			e.Pointer.Capture(null);
			e.Handled = true;
		}
		base.OnPointerReleased(e);
	}

	/// <inheritdoc />
	protected override void OnTextViewChanged(TextView oldTextView, TextView newTextView)
	{
		if (oldTextView != null)
		{
			oldTextView.VisualLinesChanged -= TextViewVisualLinesChanged;
		}
		base.OnTextViewChanged(oldTextView, newTextView);
		if (newTextView != null)
		{
			newTextView.VisualLinesChanged += TextViewVisualLinesChanged;
		}
		InvalidateVisual();
	}

	private static double Coerce(AvaloniaObject arg1, double arg2)
	{
		return arg2;
	}

	private void ExtendSelection(SimpleRange currentSeg)
	{
		if (currentSeg.Offset < _selectionStart.StartIndex)
		{
			TextArea.Caret.Offset = currentSeg.Offset;
			TextArea.Selection = Selection.Create(TextArea, currentSeg.Offset, _selectionStart.StartIndex + _selectionStart.Length);
		}
		else
		{
			TextArea.Caret.Offset = currentSeg.Offset + currentSeg.Length;
			TextArea.Selection = Selection.Create(TextArea, _selectionStart.StartIndex, currentSeg.Offset + currentSeg.Length);
		}
	}

	private SimpleRange GetTextLineSegment(PointerEventArgs e)
	{
		var pos = e.GetPosition(TextView);
		pos = new Point(0, pos.Y.CoerceValue(0, TextView.Bounds.Height) + TextView.VerticalOffset);
		var vl = TextView.GetVisualLineFromVisualTop(pos.Y);
		if (vl == null)
		{
			return SegmentExtensions.Invalid;
		}
		var tl = vl.GetTextLineByVisualYPosition(pos.Y);
		var visualStartColumn = vl.GetTextLineVisualStartColumn(tl);
		var visualEndColumn = visualStartColumn + tl.Length;
		var relStart = vl.FirstDocumentLine.StartIndex;
		var startOffset = vl.GetRelativeOffset(visualStartColumn) + relStart;
		var endOffset = vl.GetRelativeOffset(visualEndColumn) + relStart;
		if (endOffset == (vl.LastDocumentLine.StartIndex + vl.LastDocumentLine.Length))
		{
			endOffset += vl.LastDocumentLine.DelimiterLength;
		}
		return new SimpleRange(startOffset, endOffset - startOffset);
	}

	private void OnDocumentLineCountChanged(object sender, EventArgs e)
	{
		OnDocumentLineCountChanged();
	}

	private void OnDocumentLineCountChanged()
	{
		var documentLineCount = Document?.LineCount ?? 1;
		var newLength = documentLineCount.ToString(CultureInfo.CurrentCulture).Length;

		// The margin looks too small when there is only one digit, so always reserve space for
		// at least two digits
		if (newLength < 2)
		{
			newLength = 2;
		}

		if (newLength != MaxLineNumberLength)
		{
			MaxLineNumberLength = newLength;
			InvalidateMeasure();
		}
	}

	private void TextViewVisualLinesChanged(object sender, EventArgs e)
	{
		InvalidateMeasure();
	}

	#endregion
}