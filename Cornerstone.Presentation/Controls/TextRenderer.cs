#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Controls.Text;
using Cornerstone.Presentation.Controls.Text.Models;
using Cornerstone.Presentation.Controls.Text.Rendering;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.TextFormatting;
using Cornerstone.Presentation.Threading;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using Cornerstone.Text.Parsing;
using Cornerstone.Text.Parsing.Markdown;
using IRenderer = Cornerstone.Presentation.Controls.Text.Rendering.IRenderer;

#endregion

namespace Cornerstone.Presentation.Controls;

[SourceReflection]
public partial class TextRenderer : Control<TextEditorViewModel>, ILogicalScrollable
{
	#region Constants

	private const double MutedOpacity = 0.4;

	#endregion

	#region Fields

	public readonly PresentationList<IRenderer> BackgroundRenderers;
	private readonly CurrentLineRenderer _currentLineRenderer;
	private readonly DiagnosticRenderer _diagnosticRenderer;
	private readonly DispatcherTimer _dispatchTimer;
	private TextLayout _ellipsisLayout;
	private bool _eventsAttached;
	private Size _lastRaisedExtent;
	private Vector _lastRaisedOffset;
	private Size _lastRaisedViewport;
	private readonly Dictionary<int, TextLayout> _lineNumberLayouts = new();
	private int _paintCacheGeneration;
	private readonly Dictionary<PaintLayoutKey, CachedPaintLayout> _paintLayouts = new();
	private TextLayout _sampleGlyphLayout;
	private readonly SelectionRenderer _selectionRenderer;
	private Typeface? _typefaceBold;
	private Typeface? _typefaceBoldItalic;
	private Typeface? _typefaceItalic;
	private Typeface? _typefaceNormal;
	private bool _writingOffsetFromViewModel;

	#endregion

	#region Constructors

	public TextRenderer()
	{
		_currentLineRenderer = new CurrentLineRenderer(this);
		_diagnosticRenderer = new DiagnosticRenderer(this);
		_selectionRenderer = new SelectionRenderer(this);
		_dispatchTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Background, DispatchTimerCallback);

		BackgroundRenderers = [_currentLineRenderer, _selectionRenderer];
		CaretVisual = new CaretVisual(this);
		CanVerticallyScroll = true;
		Focusable = true;
		FontSize = 16;
		ViewModel = new TextEditorViewModel();

		VisualChildren.Add(CaretVisual);

		TextOptions.SetTextOptions(this, new TextOptions
		{
			TextRenderingMode = TextRenderingMode.SubpixelAntialias,
			TextHintingMode = TextHintingMode.Strong,
			BaselinePixelAlignment = BaselinePixelAlignment.Aligned
		});
	}

	static TextRenderer()
	{
		AffectsRender<TextRenderer>(
			CurrentLineBackgroundProperty,
			ForegroundProperty,
			OffsetProperty
		);

		AffectsMeasure<TextRenderer>(
			CanHorizontallyScrollProperty,
			FontFamilyProperty,
			FontSizeProperty,
			FontStyleProperty,
			FontWeightProperty,
			ViewModelProperty
		);
	}

	#endregion

	#region Properties

	[DirectProperty]
	public bool CanHorizontallyScroll
	{
		get => !ViewModel.WordWrap;
		set => ViewModel.WordWrap = !value;
	}

	[StyledProperty]
	public partial bool CanVerticallyScroll { get; set; }

	[StyledProperty]
	public partial IBrush CurrentLineBackground { get; set; }

	public Size Extent => ViewModel == null ? default : ViewModel.ViewMetrics.DocumentSize;

	[StyledProperty]
	public partial FontFamily FontFamily { get; set; }

	[StyledProperty]
	public partial double FontSize { get; set; }

	[StyledProperty]
	public partial FontStyle FontStyle { get; set; }

	[StyledProperty]
	public partial FontWeight FontWeight { get; set; }

	[StyledProperty]
	public partial IBrush Foreground { get; set; }

	public bool IsLogicalScrollEnabled => true;

	[StyledProperty]
	public partial Vector Offset { get; set; }

	public Size PageScrollSize
	{
		get
		{
			if (ViewModel == null)
			{
				return default;
			}

			return new Size(ViewModel.ViewMetrics.CharacterWidth * 10, ViewModel.ViewMetrics.CharacterHeight * 10);
		}
	}

	[StyledProperty]
	public partial Profiler Profiler { get; set; }

	public Size ScrollSize
	{
		get
		{
			if (ViewModel == null)
			{
				return default;
			}

			return new Size(ViewModel.ViewMetrics.CharacterWidth * 3, ViewModel.ViewMetrics.CharacterHeight * 3);
		}
	}

	[DirectProperty]
	public string Text
	{
		get => ViewModel.Buffer.ToString();
		set => ViewModel.Load(value);
	}

	public Size Viewport => ViewModel == null ? default : ViewModel.ViewMetrics.Viewport;

	internal CaretVisual CaretVisual { get; }

	/// <summary>
	/// Count of Cornerstone <see cref="TextLayout" /> instances created. Tests use this to
	/// assert paint-run cache hits.
	/// </summary>
	internal int TextLayoutCreateCount { get; private set; }

	#endregion

	#region Methods

	public bool BringIntoView(Control target, Rect targetRect)
	{
		return false;
	}

	public Control GetControlInDirection(NavigationDirection direction, Control from)
	{
		return this;
	}

	public TextLayout GetTextLayout(string lineText)
	{
		return GetTextLayout(lineText, Bounds.Width);
	}

	public TextLayout GetTextLayout(string lineText, double maxWidth)
	{
		return GetTextLayout(lineText, maxWidth, ViewModel.WordWrap, Foreground);
	}

	public TextLayout GetTextLayout(string lineText, double maxWidth, bool wrap, IBrush foreground,
		bool bold = false, bool italic = false, TextDecorationCollection textDecorations = null)
	{
		var typeface = GetTypeface(bold, italic);

		// TextLayout requires a finite maxWidth; unconstrained measure can pass Infinity.
		var layoutMaxWidth = wrap && double.IsFinite(maxWidth) && (maxWidth > 0)
			? maxWidth
			: 999999;

		TextLayoutCreateCount++;
		return new TextLayout(
			lineText,
			typeface,
			FontSize,
			foreground ?? Foreground ?? Brushes.White,
			textWrapping: wrap && double.IsFinite(maxWidth) && (maxWidth > 0)
				? TextWrapping.Wrap
				: TextWrapping.NoWrap,
			maxWidth: layoutMaxWidth,
			lineHeight: ViewMetrics.GetLineHeight(FontSize),
			flowDirection: FlowDirection.LeftToRight,
			textDecorations: textDecorations
		);
	}

	public Typeface GetTypeface(bool bold, bool italic)
	{
		// Use this control's FontFamily, not TextElement.FontFamily (we are not a TemplatedControl).
		var family = FontFamily ?? FontFamily.Default;
		if (bold)
		{
			if (italic)
			{
				return _typefaceBoldItalic ??= new Typeface(family, FontStyle.Italic, FontWeight.Bold);
			}

			return _typefaceBold ??= new Typeface(family, FontStyle.Normal, FontWeight.Bold);
		}

		if (italic)
		{
			return _typefaceItalic ??= new Typeface(family, FontStyle.Italic, FontWeight.Normal);
		}

		return _typefaceNormal ??= new Typeface(family, FontStyle.Normal, FontWeight.Normal);
	}

	public IEnumerable<Line> GetVisualLines()
	{
		if (ViewModel?.Lines == null)
		{
			yield break;
		}

		var topY = Offset.Y;
		var height = Bounds.Height > 1 ? Bounds.Height : ViewModel.ViewMetrics.Viewport.Height;
		var bottomY = Offset.Y + height;
		foreach (var line in ViewModel.Lines.GetVisibleLines(topY, bottomY))
		{
			yield return line;
		}
	}

	public void RaiseScrollInvalidated(EventArgs e)
	{
		OnScrollInvalidated();
	}

	public override void Render(DrawingContext drawingContext)
	{
		using var _ = ProfilerExtensions.Start(Profiler, nameof(Render));
		_paintCacheGeneration++;
		drawingContext.FillRectangle(Brushes.Transparent, Bounds.Inflate(Margin));

		// Uncomment to see the calculated extent area
		//drawingContext.DrawRectangle(new Pen(Brushes.Red), new Rect(0, 0, Extent.Width, Extent.Height));

		foreach (var renderer in BackgroundRenderers)
		{
			try
			{
				renderer.Draw(this, drawingContext);
			}
			catch
			{
				// Selection/current-line paint must not abort the frame.
			}
		}

		if (ViewModel == null)
		{
			return;
		}

		var leftX = Offset.X;
		var topY = Offset.Y;
		var height = Bounds.Height > 1 ? Bounds.Height : ViewModel.ViewMetrics.Viewport.Height;
		var bottomY = Offset.Y + height;
		var layoutWidth = Width > 1 ? Width : 999999;
		IBrush accentBrush;

		try
		{
			accentBrush = Presentation.Theme.Theming.Theme.GetAccentBrush() ?? Foreground;
		}
		catch
		{
			accentBrush = Foreground;
		}

		var tokens = ViewModel.TokenManager;
		var lines = ViewModel.Lines;
		int firstVisible;
		try
		{
			firstVisible = lines.GetFirstVisibleLineIndex(topY);
		}
		catch
		{
			firstVisible = 0;
		}

		for (var i = firstVisible; i < lines.Count; i++)
		{
			var line = lines[i];
			if (line == null)
			{
				continue;
			}

			if (line.VisualLayout.Top >= bottomY)
			{
				break;
			}

			if ((line.VisualLayout.Height <= 0) || (line.VisualLayout.Bottom <= topY))
			{
				continue;
			}

			try
			{
				var folding = ViewModel.FoldingManager.GetFoldingStartingOnLine(line);
				var folded = folding is { IsFolded: true };
				if (folded)
				{
					using (drawingContext.PushOpacity(MutedOpacity))
					{
						DrawVisualLine(line, true);
					}

					DrawFoldEllipsis(drawingContext, line, topY);
					continue;
				}

				DrawVisualLine(line, false);
			}
			catch
			{
				// One bad line or token range must not abort the frame.
			}
		}

		try
		{
			EvictUnusedPaintLayouts();
		}
		catch
		{
		}

		_diagnosticRenderer.Draw(this, drawingContext);
		return;

		void DrawVisualLine(Line line, bool muteSyntax)
		{
			if (line.WrappedStartOffsets.Count == 0)
			{
				Process(line.VisualLayout.Top, line.StartOffset, line.Length, muteSyntax);
				return;
			}

			var subLineCount = line.WrappedStartOffsets.Count + 1;
			var lineY = 0.0;

			for (var sub = 0; sub < subLineCount; sub++)
			{
				var start = sub == 0 ? line.StartOffset : line.WrappedStartOffsets[sub - 1];
				var endExclusive = sub < line.WrappedStartOffsets.Count
					? line.WrappedStartOffsets[sub]
					: line.EndOffset;

				Process(line.VisualLayout.Top + lineY, start, endExclusive - start, muteSyntax);
				lineY += ViewModel.ViewMetrics.CharacterHeight;
			}
		}

		void DrawFoldEllipsis(DrawingContext context, Line line, double viewTopY)
		{
			var layout = GetEllipsisLayout();
			var x = line.VisualLayout.Width + (ViewModel.ViewMetrics.CharacterWidth / 2);
			var y = line.VisualLayout.Top - viewTopY;
			layout.Draw(context, new Point(Math.Round(x - Offset.X), Math.Round(y)));
		}

		void Process(double visualY, int start, int length, bool muteSyntax)
		{
			if (length <= 0)
			{
				return;
			}

			var lineEnd = start + length;
			var currentX = -leftX;
			var currentPos = start;

			foreach (var token in tokens.GetOverlappingTokens(start, lineEnd))
			{
				if (token.StartOffset > currentPos)
				{
					var gapLen = Math.Min(token.StartOffset, lineEnd) - currentPos;
					DrawRun(currentPos, gapLen, false, false, null, Foreground, ref currentX, visualY);
					currentPos = token.StartOffset;
				}

				var runStart = Math.Max(token.StartOffset, currentPos);
				var runEnd = Math.Min(token.EndOffset, lineEnd);
				if (runStart < runEnd)
				{
					var brush = Foreground;
					if (!muteSyntax)
					{
						brush = token.Type == MarkdownTokenizer.TokenTypeLink
							? accentBrush
							: token.Foreground?.GetBrush() ?? Foreground;
						if ((token.Type != MarkdownTokenizer.TokenTypeLink)
							&& SyntaxBrushes.TryGetValue(token.SyntaxKind, out var syntaxBrush))
						{
							brush = syntaxBrush;
						}
					}

					var decorations = muteSyntax
						? null
						: token.Strikethrough
							? TextDecorations.Strikethrough
							: token.Type == MarkdownTokenizer.TokenTypeLink
								? TextDecorations.Underline
								: null;
					var runLength = runEnd - runStart;
					var backgroundX = currentX;
					if (!muteSyntax)
					{
						DrawTokenBackground(token, runStart, runEnd, backgroundX, visualY, ViewModel.ViewMetrics.CharacterHeight);
					}

					var tl = DrawRun(runStart, runLength, token.Bold, token.Italic, decorations, brush, ref currentX, visualY);
				}

				currentPos = Math.Max(currentPos, token.EndOffset);
			}

			if (currentPos < lineEnd)
			{
				DrawRun(currentPos, lineEnd - currentPos, false, false, null, Foreground, ref currentX, visualY);
			}
		}

		TextLayout DrawRun(int runStart, int runLength, bool bold, bool italic, TextDecorationCollection decorations, IBrush brush, ref double currentX, double visualY)
		{
			if (runLength <= 0)
			{
				return null;
			}

			var tl = GetOrCreatePaintLayout(runStart, runLength, bold, italic, decorations, brush, layoutWidth);
			if (tl == null)
			{
				return null;
			}

			tl.Draw(drawingContext, new Point(Math.Round(currentX), Math.Round(visualY - topY)));
			currentX += tl.WidthIncludingTrailingWhitespace;
			return tl;
		}

		void DrawTokenBackground(Token token, int runStart, int runEnd, double x, double visualY, double height)
		{
			var backgroundBrush = token.Background?.GetBrush();
			if (backgroundBrush == null)
			{
				return;
			}

			var bgChars = CountNonNewlineChars(runStart, runEnd - runStart);
			if (bgChars <= 0)
			{
				return;
			}

			var width = bgChars * ViewModel.ViewMetrics.CharacterWidth;
			drawingContext.FillRectangle(backgroundBrush, new Rect(x, visualY - topY, width, height));
		}
	}

	/// <summary>
	/// Maps a pointer position (control-local) to a document offset using the same
	/// styled <see cref="TextLayout" /> run widths as <see cref="Render" />.
	/// Prefer this over <see cref="Text.Models.Line.GetNearestOffsetAtVisual" /> when the surface
	/// paints with proportional fonts / bold-italic runs (markdown links, etc.).
	/// </summary>
	public bool TryGetDocumentOffsetAtPoint(Point localPoint, out int offset)
	{
		return TryGetDocumentOffsetAtPoint(localPoint, false, out offset);
	}

	/// <summary>
	/// Maps a pointer position (control-local) to a document offset using the same
	/// styled <see cref="TextLayout" /> run widths as <see cref="Render" />.
	/// Prefer this over <see cref="Text.Models.Line.GetNearestOffsetAtVisual" /> when the surface
	/// paints with proportional fonts / bold-italic runs (markdown links, etc.).
	/// </summary>
	/// <param name="caretEdge">
	/// When true, returns a caret offset (may sit after the last character in a run)
	/// so drag-selection EndOffset is exclusive and copy includes the last glyph.
	/// When false, returns the character under the pointer (for link hit-testing).
	/// </param>
	public bool TryGetDocumentOffsetAtPoint(Point localPoint, bool caretEdge, out int offset)
	{
		offset = 0;
		var viewModel = ViewModel;
		if (viewModel?.Lines is null || (viewModel.ViewMetrics.CharacterHeight <= 0))
		{
			return false;
		}

		var visualX = localPoint.X + Offset.X;
		var visualY = localPoint.Y + Offset.Y;

		if (!viewModel.Lines.TryGetLineForOffset(visualY, visualY, out var line))
		{
			return false;
		}

		var relativeY = Math.Clamp(visualY - line.VisualLayout.Y, 0, Math.Max(0, line.VisualLayout.Height - 0.001));
		var subLineIndex = (int) (relativeY / viewModel.ViewMetrics.CharacterHeight);
		if (subLineIndex > line.WrappedStartOffsets.Count)
		{
			subLineIndex = line.WrappedStartOffsets.Count;
		}

		var start = subLineIndex == 0
			? line.StartOffset
			: line.WrappedStartOffsets[subLineIndex - 1];
		var endExclusive = subLineIndex < line.WrappedStartOffsets.Count
			? line.WrappedStartOffsets[subLineIndex]
			: line.EndOffset;

		if (start >= endExclusive)
		{
			offset = start;
			return true;
		}

		var currentX = 0.0;
		var currentPos = start;
		var layoutWidth = Bounds.Width > 1 ? Bounds.Width : 999999;

		foreach (var token in viewModel.TokenManager.GetOverlappingTokens(start, endExclusive))
		{
			if (token.StartOffset > currentPos)
			{
				var gapLen = Math.Min(token.StartOffset, endExclusive) - currentPos;
				if ((gapLen > 0)
					&& TryHitTestPaintRun(currentPos, gapLen, false, false, layoutWidth, visualX, caretEdge, ref currentX, out offset))
				{
					return true;
				}

				currentPos = Math.Max(currentPos, token.StartOffset);
			}

			var runStart = Math.Max(token.StartOffset, currentPos);
			var runEnd = Math.Min(token.EndOffset, endExclusive);
			if ((runStart < runEnd)
				&& TryHitTestPaintRun(runStart, runEnd - runStart, token.Bold, token.Italic, layoutWidth, visualX, caretEdge, ref currentX, out offset))
			{
				return true;
			}

			currentPos = Math.Max(currentPos, token.EndOffset);
		}

		if (currentPos < endExclusive)
		{
			var trailingLen = endExclusive - currentPos;
			if (TryHitTestPaintRun(currentPos, trailingLen, false, false, layoutWidth, visualX, caretEdge, ref currentX, out offset))
			{
				return true;
			}
		}

		// Past the painted end of this visual row.
		if (caretEdge)
		{
			offset = endExclusive;
			if ((line.LineEndingLength > 0) && (offset > (line.EndOffset - line.LineEndingLength)))
			{
				offset = line.EndOffset - line.LineEndingLength;
			}
		}
		else
		{
			offset = endExclusive > start ? endExclusive - 1 : start;
		}

		return true;
	}

	protected internal virtual void OnScrollInvalidated()
	{
		OnPropertyChanged(nameof(Extent));
		OnPropertyChanged(nameof(Offset));
		OnPropertyChanged(nameof(Viewport));
		ScrollInvalidated?.Invoke(this, EventArgs.Empty);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		if (ViewModel == null)
		{
			return default;
		}

		// ScrollViewer measures with infinite constraints in scroll directions;
		// the arranged size is the true viewport.
		ViewModel.ViewMetrics.Viewport = finalSize;
		if ((finalSize.Height <= 1) || (finalSize.Width <= 1))
		{
			ViewModel.IsRestoringViewport = true;
		}
		else
		{
			ApplyOwnedScrollOffset();
		}

		RaiseScrollInvalidatedIfChanged();
		var wasEmpty = Bounds.Height <= 1;
		var arranged = base.ArrangeOverride(finalSize);

		// First layout can paint while Bounds is still 0; schedule a real paint
		// once the viewport exists.
		if (wasEmpty && (finalSize.Height > 1))
		{
			InvalidateVisual();
		}

		return arranged;
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		using var _ = ProfilerExtensions.Start(Profiler, nameof(MeasureOverride));
		if (ViewModel == null)
		{
			return default;
		}

		var sample = GetSampleGlyphLayout();
		ViewModel.Measure(sample, availableSize);
		if (ViewModel.Lines.LastMeasureChangedLayout)
		{
			CaretVisual.InvalidateVisual();
		}

		RaiseScrollInvalidatedIfChanged();

		// Cornerstone throws InvalidOperationException if Measure returns NaN/Infinity
		// (e.g. TextEditor in a parent with unconstrained height/width).
		var size = ViewModel.ViewMetrics.DocumentSize;
		return new Size(
			double.IsFinite(size.Width) && (size.Width >= 0) ? size.Width : 0,
			double.IsFinite(size.Height) && (size.Height >= 0) ? size.Height : 0
		);
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);

		AttachEvents(ViewModel);
		if (ViewModel != null)
		{
			ViewModel.IsRestoringViewport = true;
		}

		// Force a full refresh after reattach. Do not RaiseScrollInvalidated here —
		// Offset may still be 0 from a collapsed tab; arrange applies ViewMetrics.
		InvalidateMeasure();
		InvalidateVisual();
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		if (ViewModel != null)
		{
			ViewModel.IsRestoringViewport = true;
		}

		base.OnDetachedFromVisualTree(e);
		DetachEvents(ViewModel);
		_dispatchTimer.Stop();
		InvalidatePaintLayouts();
		InvalidateSampleGlyphLayout();
		CaretVisual?.InvalidateVisual();
	}

	protected override void OnGotFocus(FocusChangedEventArgs e)
	{
		// Selection still works without a caret; skip blink timer when caret is hidden.
		if ((ViewModel != null) && ViewModel.ShowCaret)
		{
			_dispatchTimer.IsEnabled = true;
			ViewModel.Caret.IsVisible = true;
			ViewModel.Caret.ResetBlink();
			CaretVisual.InvalidateVisual();
		}
		else if (ViewModel != null)
		{
			ViewModel.Caret.IsVisible = false;
		}

		base.OnGotFocus(e);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		if (!e.Handled)
		{
			ViewModel.ProcessKeyDownEvent(e);
		}
		base.OnKeyDown(e);
	}

	protected override void OnKeyUp(KeyEventArgs e)
	{
		if (!e.Handled)
		{
			ViewModel.ProcessKeyUpEvent(e);
		}
		base.OnKeyUp(e);
	}

	protected override void OnLostFocus(FocusChangedEventArgs e)
	{
		_dispatchTimer.IsEnabled = false;
		ViewModel.Caret.IsVisible = false;
		foreach (var caret in ViewModel.Carets.All)
		{
			caret.Selection.StopMouseSelection();
			caret.Selection.StopKeyboardSelection();
		}
		CaretVisual.InvalidateVisual();
		base.OnLostFocus(e);
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		if (e.Properties.IsLeftButtonPressed)
		{
			var point = e.GetPosition(this);
			if (TryGetDocumentOffsetAtPoint(point, true, out var offset))
			{
				ViewModel.HandlePointerMoved(offset);
				InvalidateVisual();
			}
		}

		base.OnPointerMoved(e);
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		var viewModel = ViewModel;
		if (e.Handled
			|| (viewModel == null)
			|| !e.Properties.IsLeftButtonPressed)
		{
			base.OnPointerPressed(e);
			return;
		}

		var point = e.GetPosition(this);
		if (!TryGetDocumentOffsetAtPoint(point, true, out var caretOffset))
		{
			base.OnPointerPressed(e);
			return;
		}

		viewModel.HandlePointerPressed(caretOffset, e.KeyModifiers, e.ClickCount);
		base.OnPointerPressed(e);
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
		{
			ViewModel.HandlePointerReleased();
			InvalidateVisual();
		}
		base.OnPointerReleased(e);
	}

	protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
	{
		if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
		{
			if ((e.Delta.Y > 0) && (FontSize < 40))
			{
				FontSize += 1;
				e.Handled = true;
			}

			if ((e.Delta.Y < 0) && (FontSize > 12))
			{
				FontSize -= 1;
				e.Handled = true;
			}
		}
		base.OnPointerWheelChanged(e);
	}

	protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
	{
		if ((change.Property == CanHorizontallyScrollProperty)
			&& change.NewValue is bool canHorizontallyScroll)
		{
			ViewModel.WordWrap = !canHorizontallyScroll;
		}
		if ((change.Property == OffsetProperty)
			&& change.NewValue is Vector offset)
		{
			PersistOffsetToViewModel(offset);
		}

		if (change.Property == ViewModelProperty)
		{
			DetachEvents(change.OldValue as TextEditorViewModel);
			AttachEvents(change.NewValue as TextEditorViewModel);
			if (change.NewValue != null)
			{
				InvalidateMeasure();
				RaiseScrollInvalidated(EventArgs.Empty);
			}
		}

		base.OnPropertyChanged(change);

		if ((change.Property == FontFamilyProperty)
			|| (change.Property == FontSizeProperty)
			|| (change.Property == ForegroundProperty))
		{
			_typefaceNormal = null;
			_typefaceBold = null;
			_typefaceBoldItalic = null;
			_typefaceItalic = null;
			InvalidateSampleGlyphLayout();
			InvalidatePaintLayouts();
			InvalidateVisual();
		}
	}

	internal TextLayout GetLineNumberLayout(int lineNumber)
	{
		if (_lineNumberLayouts.TryGetValue(lineNumber, out var layout))
		{
			return layout;
		}

		if (_lineNumberLayouts.Count >= 256)
		{
			foreach (var cached in _lineNumberLayouts.Values)
			{
				cached.Dispose();
			}

			_lineNumberLayouts.Clear();
		}

		layout = GetTextLayout(lineNumber.ToString(), 999999, false, Foreground);
		_lineNumberLayouts[lineNumber] = layout;
		return layout;
	}

	/// <summary>
	/// Selection rectangles in document space using the same styled
	/// <see cref="TextLayout" /> run widths as <see cref="Render" />.
	/// Uniform <see cref="ViewMetrics.GetAdvance" /> does not match bold/italic/proportional paint
	/// (markdown display has markers stripped, so a monospace grid is the wrong width).
	/// </summary>
	internal IEnumerable<Rect> GetPaintedSelectionRects(Line line, int selectionStart, int selectionEnd)
	{
		if ((line == null) || (selectionEnd <= selectionStart) || (line.VisualLayout.Height <= 0))
		{
			yield break;
		}

		var viewModel = ViewModel;
		if (viewModel?.ViewMetrics is null)
		{
			yield break;
		}

		var height = viewModel.ViewMetrics.CharacterHeight;
		if (height <= 0)
		{
			yield break;
		}

		var lineSelStart = Math.Max(selectionStart, line.StartOffset);
		var lineSelEnd = Math.Min(selectionEnd, line.EndOffset);
		if (lineSelStart >= lineSelEnd)
		{
			yield break;
		}

		var subLineCount = line.VisualSubLineCount;
		for (var sub = 0; sub < subLineCount; sub++)
		{
			line.GetVisualSubLineRange(sub, out var subStart, out var subEnd);
			var selStart = Math.Max(lineSelStart, subStart);
			var selEnd = Math.Min(lineSelEnd, subEnd);
			if (selStart >= selEnd)
			{
				continue;
			}

			var x0 = GetPaintedWidth(subStart, selStart);
			var x1 = GetPaintedWidth(subStart, selEnd);
			var width = Math.Max(0, x1 - x0);
			if (width <= 0)
			{
				width = viewModel.ViewMetrics.CharacterWidth;
			}

			var y = line.VisualLayout.Y + (sub * height);
			yield return new Rect(x0, y, width, height);
		}
	}

	/// <summary>
	/// Painted pixel width of <c> [start, endExclusive) </c> using token styles (bold/italic).
	/// </summary>
	internal double GetPaintedWidth(int start, int endExclusive)
	{
		if ((ViewModel == null) || (endExclusive <= start))
		{
			return 0;
		}

		var currentX = 0.0;
		var currentPos = start;
		var layoutWidth = Bounds.Width > 1 ? Bounds.Width : 999999;

		foreach (var token in ViewModel.TokenManager.GetOverlappingTokens(start, endExclusive))
		{
			if (token.StartOffset > currentPos)
			{
				var gapEnd = Math.Min(token.StartOffset, endExclusive);
				currentX += MeasurePaintRunWidth(currentPos, gapEnd - currentPos, false, false, layoutWidth);
				currentPos = gapEnd;
				if (currentPos >= endExclusive)
				{
					return currentX;
				}
			}

			var runStart = Math.Max(token.StartOffset, currentPos);
			var runEnd = Math.Min(token.EndOffset, endExclusive);
			if (runStart < runEnd)
			{
				currentX += MeasurePaintRunWidth(runStart, runEnd - runStart, token.Bold, token.Italic, layoutWidth);
				currentPos = runEnd;
			}

			if (currentPos >= endExclusive)
			{
				return currentX;
			}

			currentPos = Math.Max(currentPos, token.EndOffset);
		}

		if (currentPos < endExclusive)
		{
			currentX += MeasurePaintRunWidth(currentPos, endExclusive - currentPos, false, false, layoutWidth);
		}

		return currentX;
	}

	/// <summary>
	/// Apply ViewMetrics.Offset to the logical scroller after layout. Does not persist a
	/// collapsed-viewport clamp back into the VM.
	/// </summary>
	private void ApplyOwnedScrollOffset()
	{
		if (ViewModel == null)
		{
			return;
		}

		if ((Viewport.Height <= 1) || (Viewport.Width <= 1))
		{
			return;
		}

		var restoring = ViewModel.IsRestoringViewport;
		if (restoring && (Extent.Height <= 1))
		{
			return;
		}

		var clamped = ViewModel.ViewMetrics.ClampOffset(Extent, Viewport);
		if (Offset != clamped)
		{
			_writingOffsetFromViewModel = true;
			try
			{
				Offset = clamped;
			}
			finally
			{
				_writingOffsetFromViewModel = false;
			}
		}

		if (restoring)
		{
			if (HasStableViewport())
			{
				ViewModel.IsRestoringViewport = false;
			}

			return;
		}

		if (HasStableViewport() && (ViewModel.ViewMetrics.Offset != clamped))
		{
			ViewModel.ViewMetrics.Offset = clamped;
		}
	}

	private void AttachEvents(TextEditorViewModel viewModel)
	{
		if ((viewModel == null) || _eventsAttached)
		{
			return;
		}

		_eventsAttached = true;
		viewModel.PropertyChanged += ViewModelOnPropertyChanged;
		viewModel.Carets.CaretMoved += OnCaretMoved;
		viewModel.Carets.SelectionUpdated += SelectionOnUpdated;
		viewModel.Carets.CaretsChanged += OnCaretsChanged;
		viewModel.DocumentChanged += OnDocumentChanged;
	}

	private int CountNonNewlineChars(int start, int length)
	{
		var count = 0;
		var spans = ViewModel.Buffer.GetReadOnlySpans(start, length);
		CountSpan(spans.BeforeGap);
		CountSpan(spans.AfterGap);
		return count;

		void CountSpan(ReadOnlySpan<char> span)
		{
			for (var i = 0; i < span.Length; i++)
			{
				var c = span[i];
				if ((c != '\r') && (c != '\n'))
				{
					count++;
				}
			}
		}
	}

	private void DetachEvents(TextEditorViewModel viewModel)
	{
		if (viewModel == null)
		{
			return;
		}

		viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
		viewModel.Carets.CaretMoved -= OnCaretMoved;
		viewModel.Carets.SelectionUpdated -= SelectionOnUpdated;
		viewModel.Carets.CaretsChanged -= OnCaretsChanged;
		viewModel.DocumentChanged -= OnDocumentChanged;
		_eventsAttached = false;
	}

	private void DispatchTimerCallback(object sender, EventArgs e)
	{
		ViewModel?.Caret.ToggleBlink();
		UpdateCaret();
	}

	private void EnsureCaretVisible(Caret caret)
	{
		if (ViewModel is { IsRestoringViewport: true })
		{
			return;
		}

		if ((Viewport.Width <= 1) || (Viewport.Height <= 1) || (caret.VisualLayout.Height <= 0))
		{
			return;
		}

		// Only scroll when caret is out of view
		var visibleRect = new Rect(Offset.X, Offset.Y, Viewport.Width, Viewport.Height);

		if (visibleRect.Contains(caret.VisualLayout.TopLeft)
			&& visibleRect.Contains(caret.VisualLayout.TopRight)
			&& visibleRect.Contains(caret.VisualLayout.BottomRight)
			&& visibleRect.Contains(caret.VisualLayout.BottomLeft))
		{
			return;
		}

		// bug: this is processing before caret.VisualLayout is recalculated

		var targetX = Offset.X;
		var targetY = Offset.Y;

		if (caret.VisualLayout.Y < Offset.Y)
		{
			// Scroll Up
			targetY = Math.Max(0, caret.VisualLayout.Y);
		}
		else if (caret.VisualLayout.Bottom > (Offset.Y + Viewport.Height))
		{
			// Scroll Down
			targetY = Math.Max(0, caret.VisualLayout.Bottom - Viewport.Height);
		}

		// AutoScroll means pin to the bottom, not chase the caret horizontally.
		// Terminal output otherwise scrolls X to the end of a long line, then
		// a later vertical pin resets X — visible as a right-then-left jump.
		if (!ViewModel.WordWrap && !ViewModel.AutoScroll)
		{
			if ((caret.VisualLayout.X + caret.VisualLayout.Width) > (Offset.X + Viewport.Width))
			{
				targetX = Math.Max(0, (caret.VisualLayout.X - Viewport.Width) + caret.VisualLayout.Width + 16);
			}
			else if (caret.VisualLayout.X < Offset.X)
			{
				targetX = Math.Max(0, caret.VisualLayout.X - 8);
			}
		}

		Offset = new Vector(targetX, targetY);
		RaiseScrollInvalidated(EventArgs.Empty);
	}

	private void EvictUnusedPaintLayouts()
	{
		List<PaintLayoutKey> remove = null;
		foreach (var pair in _paintLayouts)
		{
			if (pair.Value.Generation == _paintCacheGeneration)
			{
				continue;
			}

			pair.Value.Layout.Dispose();
			remove ??= [];
			remove.Add(pair.Key);
		}

		if (remove == null)
		{
			return;
		}

		foreach (var key in remove)
		{
			_paintLayouts.Remove(key);
		}
	}

	private TextLayout GetEllipsisLayout()
	{
		if (_ellipsisLayout != null)
		{
			return _ellipsisLayout;
		}

		_ellipsisLayout = GetTextLayout("...", 999999, false, Foreground);
		return _ellipsisLayout;
	}

	private TextLayout GetOrCreatePaintLayout(
		int start,
		int length,
		bool bold,
		bool italic,
		TextDecorationCollection decorations,
		IBrush brush,
		double layoutWidth)
	{
		if ((ViewModel == null) || (length <= 0))
		{
			return null;
		}

		var count = ViewModel.Buffer.Count;
		if (start < 0)
		{
			length += start;
			start = 0;
		}

		if ((start >= count) || (length <= 0))
		{
			return null;
		}

		if ((start + length) > count)
		{
			length = count - start;
			if (length <= 0)
			{
				return null;
			}
		}

		var decoration = decorations == TextDecorations.Strikethrough
			? 1
			: decorations == TextDecorations.Underline
				? 2
				: 0;
		var key = new PaintLayoutKey(start, length, bold, italic, decoration, brush);
		if (_paintLayouts.TryGetValue(key, out var cached))
		{
			cached.Generation = _paintCacheGeneration;
			return cached.Layout;
		}

		var runText = ViewModel.Buffer.Substring(start, length);
		var layout = GetTextLayout(runText, layoutWidth, false, brush, bold, italic, decorations);
		_paintLayouts[key] = new CachedPaintLayout
		{
			Layout = layout,
			Generation = _paintCacheGeneration
		};
		return layout;
	}

	private TextLayout GetSampleGlyphLayout()
	{
		if (_sampleGlyphLayout != null)
		{
			return _sampleGlyphLayout;
		}

		_sampleGlyphLayout = GetTextLayout("X", 999999, false, Foreground);
		return _sampleGlyphLayout;
	}

	private bool HasStableViewport()
	{
		return (Viewport.Height > 1) && (Extent.Height > 1);
	}

	private void InvalidatePaintLayouts()
	{
		foreach (var cached in _paintLayouts.Values)
		{
			cached.Layout.Dispose();
		}

		_paintLayouts.Clear();
		_ellipsisLayout?.Dispose();
		_ellipsisLayout = null;
	}

	private void InvalidateSampleGlyphLayout()
	{
		_sampleGlyphLayout?.Dispose();
		_sampleGlyphLayout = null;
		foreach (var layout in _lineNumberLayouts.Values)
		{
			layout.Dispose();
		}

		_lineNumberLayouts.Clear();
	}

	private double MeasurePaintRunWidth(int runStart, int runLength, bool bold, bool italic, double layoutWidth)
	{
		if (runLength <= 0)
		{
			return 0;
		}

		var layout = GetOrCreatePaintLayout(runStart, runLength, bold, italic, null, Foreground, layoutWidth);
		return layout?.WidthIncludingTrailingWhitespace ?? 0;
	}

	private void OnCaretMoved(object sender, EventArgs e)
	{
		var caret = (Caret) sender;
		if (caret.Selection.IsSelectingUsingKeyboard)
		{
			caret.Selection.EndOffset = caret.Offset;
			InvalidateVisual();
		}

		EnsureCaretVisible(caret);
		caret.ResetBlink();
		UpdateCaret();
	}

	private void OnCaretsChanged(object sender, EventArgs e)
	{
		InvalidateVisual();
		CaretVisual?.InvalidateVisual();
	}

	private void OnDocumentChanged(object sender, TextDocumentChangedArgs e)
	{
		InvalidatePaintLayouts();
		if (ViewModel.Lines.LastEditNeedsPaintOnly)
		{
			RaiseScrollInvalidatedIfChanged();
			InvalidateVisual();
			return;
		}

		InvalidateMeasure();
	}

	private void PersistOffsetToViewModel(Vector offset)
	{
		if (_writingOffsetFromViewModel
			|| (ViewModel == null)
			|| ViewModel.IsRestoringViewport
			|| (VisualRoot == null)
			|| !HasStableViewport())
		{
			return;
		}

		// Stick-to-bottom: a coerced 0 from an unready ScrollViewer is not user state.
		if (ViewModel.AutoScroll
			&& (offset.Y <= 1)
			&& (ViewModel.ViewMetrics.Offset.Y > 1))
		{
			return;
		}

		ViewModel.ViewMetrics.Offset = offset;
	}

	private bool RaiseScrollInvalidatedIfChanged()
	{
		var extent = Extent;
		var viewport = Viewport;
		var offset = Offset;
		if ((extent == _lastRaisedExtent)
			&& (viewport == _lastRaisedViewport)
			&& (offset == _lastRaisedOffset))
		{
			return false;
		}

		_lastRaisedExtent = extent;
		_lastRaisedViewport = viewport;
		_lastRaisedOffset = offset;
		OnScrollInvalidated();
		return true;
	}

	private void SelectionOnUpdated(object sender, EventArgs e)
	{
		InvalidateVisual();
	}

	/// <summary>
	/// Advances <paramref name="currentX" /> by the painted run width. When
	/// <paramref name="visualX" /> falls inside the run, sets <paramref name="offset" />
	/// and returns true.
	/// </summary>
	private bool TryHitTestPaintRun(
		int runStart,
		int runLength,
		bool bold,
		bool italic,
		double layoutWidth,
		double visualX,
		bool caretEdge,
		ref double currentX,
		out int offset)
	{
		offset = runStart;
		if (runLength <= 0)
		{
			return false;
		}

		var layout = GetOrCreatePaintLayout(runStart, runLength, bold, italic, null, Foreground, layoutWidth);
		if (layout == null)
		{
			return false;
		}

		var runWidth = layout.WidthIncludingTrailingWhitespace;

		if (visualX > (currentX + runWidth))
		{
			currentX += runWidth;
			return false;
		}

		var hit = layout.HitTestPoint(new Point(visualX - currentX, 0));
		var indexInRun = hit.CharacterHit.FirstCharacterIndex;
		if (caretEdge)
		{
			indexInRun += hit.CharacterHit.TrailingLength;
			if (indexInRun < 0)
			{
				indexInRun = 0;
			}
			else if (indexInRun > runLength)
			{
				indexInRun = runLength;
			}
		}
		else
		{
			if (indexInRun < 0)
			{
				indexInRun = 0;
			}
			else if (indexInRun >= runLength)
			{
				indexInRun = runLength - 1;
			}
		}

		offset = runStart + indexInRun;
		currentX += runWidth;
		return true;
	}

	private void UpdateCaret()
	{
		CaretVisual.InvalidateVisual();

		if (ViewModel.HighlightCurrentLine
			&& (ViewModel.Caret.Line != _currentLineRenderer.CurrentLine))
		{
			InvalidateVisual();
		}
	}

	private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(ViewModel.WordWrap):
			{
				InvalidateMeasure();
				break;
			}
			case nameof(ViewModel.DiagnosticVersion):
			{
				InvalidateVisual();
				break;
			}
		}
	}

	#endregion

	#region Events

	public event EventHandler ScrollInvalidated;

	#endregion

	#region Classes

	private sealed class CachedPaintLayout
	{
		#region Fields

		public int Generation;
		public TextLayout Layout;

		#endregion
	}

	#endregion

	#region Structures

	private readonly struct PaintLayoutKey : IEquatable<PaintLayoutKey>
	{
		#region Constructors

		public PaintLayoutKey(int start, int length, bool bold, bool italic, int decoration, IBrush brush)
		{
			Start = start;
			Length = length;
			Bold = bold;
			Italic = italic;
			Decoration = decoration;
			Brush = brush;
		}

		#endregion

		#region Properties

		public bool Bold { get; }

		public IBrush Brush { get; }

		public int Decoration { get; }

		public bool Italic { get; }

		public int Length { get; }

		public int Start { get; }

		#endregion

		#region Methods

		public bool Equals(PaintLayoutKey other)
		{
			return (Start == other.Start)
				&& (Length == other.Length)
				&& (Bold == other.Bold)
				&& (Italic == other.Italic)
				&& (Decoration == other.Decoration)
				&& ReferenceEquals(Brush, other.Brush);
		}

		public override bool Equals(object obj)
		{
			return obj is PaintLayoutKey other && Equals(other);
		}

		public override int GetHashCode()
		{
			var hash = new HashCode();
			hash.Add(Start);
			hash.Add(Length);
			hash.Add(Bold);
			hash.Add(Italic);
			hash.Add(Decoration);
			hash.Add(RuntimeHelpers.GetHashCode(Brush));
			return hash.ToHashCode();
		}

		#endregion
	}

	#endregion
}