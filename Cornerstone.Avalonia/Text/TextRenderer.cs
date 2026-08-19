#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Cornerstone.Avalonia.Extensions;
using Cornerstone.Avalonia.Text.Models;
using Cornerstone.Avalonia.Text.Rendering;
using Cornerstone.Avalonia.Themes;
using Cornerstone.Parsers.Markdown;
using Cornerstone.Presentation;
using Cornerstone.Profiling;
using Cornerstone.Reflection;
using DispatcherPriority = Avalonia.Threading.DispatcherPriority;
using IRenderer = Cornerstone.Avalonia.Text.Rendering.IRenderer;

#endregion

namespace Cornerstone.Avalonia.Text;

[SourceReflection]
public partial class TextRenderer : CornerstoneControl<TextEditorViewModel>, ILogicalScrollable
{
	#region Fields

	public readonly PresentationList<IRenderer> BackgroundRenderers;
	private readonly CurrentLineRenderer _currentLineRenderer;
	private readonly DispatcherTimer _dispatchTimer;
	private bool _eventsAttached;
	private readonly SelectionRenderer _selectionRenderer;
	private Typeface? _typefaceBold;
	private Typeface? _typefaceBoldItalic;
	private Typeface? _typefaceItalic;
	private Typeface? _typefaceNormal;

	#endregion

	#region Constructors

	public TextRenderer()
	{
		_currentLineRenderer = new CurrentLineRenderer(this);
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

			return new Size(ViewModel.ViewMetrics.CharacterWidth * 10, ViewModel.ViewMetrics.CharacterWidth * 10);
		}
	}

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

		return new TextLayout(
			lineText,
			typeface,
			FontSize,
			foreground ?? Foreground ?? Brushes.White,
			textWrapping: wrap && double.IsFinite(maxWidth) && (maxWidth > 0)
				? TextWrapping.Wrap
				: TextWrapping.NoWrap,
			maxWidth: layoutMaxWidth,
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
		var bottomY = Offset.Y + Bounds.Bottom;
		foreach (var line in ViewModel.Lines.GetVisibleLines(topY, bottomY))
		{
			yield return line;
		}
	}

	/// <summary>
	/// Maps a pointer position (control-local) to a document offset using the same
	/// styled <see cref="TextLayout"/> run widths as <see cref="Render"/>.
	/// Prefer this over <see cref="Line.GetNearestOffsetAtVisual"/> when the surface
	/// paints with proportional fonts / bold-italic runs (markdown links, etc.).
	/// </summary>
	public bool TryGetDocumentOffsetAtPoint(Point localPoint, out int offset)
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

		foreach (var token in viewModel.TokenManager.GetTokens(start, endExclusive))
		{
			if (token.StartOffset > currentPos)
			{
				var gapLen = Math.Min(token.StartOffset, endExclusive) - currentPos;
				if ((gapLen > 0)
					&& TryHitTestPaintRun(currentPos, gapLen, false, false, layoutWidth, visualX, ref currentX, out offset))
				{
					return true;
				}

				currentPos = Math.Max(currentPos, token.StartOffset);
			}

			var runStart = Math.Max(token.StartOffset, currentPos);
			var runEnd = Math.Min(token.EndOffset, endExclusive);
			if ((runStart < runEnd)
				&& TryHitTestPaintRun(runStart, runEnd - runStart, token.Bold, token.Italic, layoutWidth, visualX, ref currentX, out offset))
			{
				return true;
			}

			currentPos = Math.Max(currentPos, token.EndOffset);
		}

		if (currentPos < endExclusive)
		{
			var trailingLen = endExclusive - currentPos;
			if (TryHitTestPaintRun(currentPos, trailingLen, false, false, layoutWidth, visualX, ref currentX, out offset))
			{
				return true;
			}
		}

		// Past the painted end of this visual row — clamp to last character when present.
		offset = endExclusive > start ? endExclusive - 1 : start;
		return true;
	}

	public void RaiseScrollInvalidated(EventArgs e)
	{
		OnScrollInvalidated();
	}

	/// <summary>
	/// Advances <paramref name="currentX"/> by the painted run width. When
	/// <paramref name="visualX"/> falls inside the run, sets <paramref name="offset"/>
	/// to the character under the pointer (not the trailing caret edge) and returns true.
	/// </summary>
	private bool TryHitTestPaintRun(
		int runStart,
		int runLength,
		bool bold,
		bool italic,
		double layoutWidth,
		double visualX,
		ref double currentX,
		out int offset)
	{
		offset = runStart;
		if (runLength <= 0)
		{
			return false;
		}

		var runText = ViewModel.Buffer.Substring(runStart, runLength);
		using var layout = GetTextLayout(runText, layoutWidth, false, Foreground, bold, italic);
		var runWidth = layout.WidthIncludingTrailingWhitespace;

		if (visualX > (currentX + runWidth))
		{
			currentX += runWidth;
			return false;
		}

		var hit = layout.HitTestPoint(new Point(visualX - currentX, 0));
		var indexInRun = hit.CharacterHit.FirstCharacterIndex;
		if (indexInRun < 0)
		{
			indexInRun = 0;
		}
		else if (indexInRun >= runLength)
		{
			indexInRun = runLength - 1;
		}

		offset = runStart + indexInRun;
		currentX += runWidth;
		return true;
	}

	public override void Render(DrawingContext drawingContext)
	{
		using var _ = ProfilerExtensions.Start(Profiler, nameof(Render));
		drawingContext.FillRectangle(Brushes.Transparent, Bounds.Inflate(Margin));

		// Uncomment to see the calculated extent area
		//drawingContext.DrawRectangle(new Pen(Brushes.Red), new Rect(0, 0, Extent.Width, Extent.Height));

		foreach (var renderer in BackgroundRenderers)
		{
			renderer.Draw(this, drawingContext);
		}

		var leftX = Offset.X;
		var topY = Offset.Y;

		foreach (var line in GetVisualLines())
		{
			if (line.WrappedStartOffsets.Count == 0)
			{
				Process(line.VisualLayout.Top, line.StartOffset, line.Length);
				continue;
			}

			var subLineCount = line.WrappedStartOffsets.Count + 1;
			var lineY = 0.0;

			for (var sub = 0; sub < subLineCount; sub++)
			{
				var start = sub == 0 ? line.StartOffset : line.WrappedStartOffsets[sub - 1];
				var endExclusive = sub < line.WrappedStartOffsets.Count
					? line.WrappedStartOffsets[sub]
					: line.EndOffset;

				Process(line.VisualLayout.Top + lineY, start, endExclusive - start);
				lineY += ViewModel.ViewMetrics.CharacterHeight;
			}

			// Uncomment to see the calculated visual layout
			//drawingContext.DrawRectangle(new Pen(Brushes.Blue), line.VisualLayout);
		}

		return;

		void Process(double visualY, int start, int length)
		{
			if (length <= 0)
			{
				return;
			}

			var lineEnd = start + length;
			var currentX = -leftX;
			var currentPos = start;

			foreach (var token in ViewModel.TokenManager.GetTokens(start, lineEnd))
			{
				// Print the gap before the token
				if (token.StartOffset > currentPos)
				{
					var gapLen = Math.Min(token.StartOffset, lineEnd) - currentPos;
					if (gapLen > 0)
					{
						var gapText = ViewModel.Buffer.Substring(currentPos, gapLen);
						using var tl = GetTextLayout(gapText, Width, false, Foreground);
						tl.Draw(drawingContext, new Point(Math.Round(currentX), Math.Round(visualY - topY)));
						currentX += tl.WidthIncludingTrailingWhitespace;
					}
					currentPos = token.StartOffset;
				}

				// The formatted part, clipped to current line
				var runStart = Math.Max(token.StartOffset, currentPos);
				var runEnd = Math.Min(token.EndOffset, lineEnd);

				if (runStart < runEnd)
				{
					var brush = token.Type == MarkdownTokenizer.TokenTypeLink
						? global::Cornerstone.Avalonia.Themes.Theme.GetAccentBrush()
						: token.Foreground?.GetBrush() ?? Foreground;
					if ((token.Type != MarkdownTokenizer.TokenTypeLink)
						&& SyntaxBrushes.TryGetValue(token.SyntaxKind, out var b))
					{
						brush = b;
					}
					var runText = ViewModel.Buffer.Substring(runStart, runEnd - runStart);
					var decorations = token.Strikethrough
						? TextDecorations.Strikethrough
						: token.Type == MarkdownTokenizer.TokenTypeLink
							? TextDecorations.Underline
							: null;
					using var tl = GetTextLayout(runText, Width, false, brush, token.Bold, token.Italic, decorations);

					var backgroundBrush = token.Background?.GetBrush();
					if (backgroundBrush != null)
					{
						var width = runText.Length * ViewModel.ViewMetrics.CharacterWidth;
						var backgroundBounds = new Rect(currentX, visualY - topY, width, tl.Height);
						var geometry = new RectangleGeometry(backgroundBounds);
						drawingContext.DrawGeometry(backgroundBrush, null, geometry);
					}

					tl.Draw(drawingContext, new Point(Math.Round(currentX), Math.Round(visualY - topY)));
					currentX += tl.WidthIncludingTrailingWhitespace;
				}

				currentPos = Math.Max(currentPos, token.EndOffset);
			}

			// Trailing unpainted gap after this token
			if (currentPos < lineEnd)
			{
				var trailingLen = lineEnd - currentPos;
				var trailingText = ViewModel.Buffer.Substring(currentPos, trailingLen);
				using var tl = GetTextLayout(trailingText, Width, false, Foreground);
				tl.Draw(drawingContext, new Point(Math.Round(currentX), Math.Round(visualY - topY)));
			}
		}
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
		OnScrollInvalidated();
		return base.ArrangeOverride(finalSize);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		using var _ = ProfilerExtensions.Start(Profiler, nameof(MeasureOverride));
		if (ViewModel == null)
		{
			return default;
		}

		// TextLayout maxWidth must be finite; unconstrained measure uses a large
		// stand-in so glyph metrics still resolve.
		var layoutWidth = double.IsFinite(availableSize.Width) ? availableSize.Width : 999999;
		using var line = GetTextLayout("X", layoutWidth);
		ViewModel.Measure(line, availableSize);
		OnScrollInvalidated();

		// Avalonia throws InvalidOperationException if Measure returns NaN/Infinity
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

		// Force a full refresh after reattach
		InvalidateMeasure();
		InvalidateVisual();

		// Re-raise scroll info so parent ScrollViewer knows the extent/viewport
		RaiseScrollInvalidated(EventArgs.Empty);
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnDetachedFromVisualTree(e);
		DetachEvents(ViewModel);
		_dispatchTimer.Stop();
		CaretVisual?.InvalidateVisual();
	}

	protected override void OnGotFocus(FocusChangedEventArgs e)
	{
		// Selection still works without a caret; skip blink timer when caret is hidden.
		if ((ViewModel != null) && ViewModel.ShowCaret)
		{
			_dispatchTimer.IsEnabled = true;
			ViewModel.Caret.IsVisible = true;
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
		ViewModel.Caret.Selection.StopSelection();
		CaretVisual.InvalidateVisual();
		base.OnLostFocus(e);
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		if (ViewModel.Caret.Selection.IsSelectingUsingMouse
			&& e.Properties.IsLeftButtonPressed)
		{
			var point = e.GetPosition(this);
			var visualX = point.X + Offset.X;
			var visualY = point.Y + Offset.Y;

			if (!ViewModel.Lines.TryGetLineForOffset(visualY, visualY, out var line))
			{
				return;
			}

			var offset = line.GetNearestOffsetAtVisual(visualX, visualY, false);
			if (ViewModel.Caret.Selection.EndOffset != offset)
			{
				ViewModel.Caret.Selection.EndOffset = offset;
				ViewModel.Caret.Move(offset);

				InvalidateVisual();
			}
		}

		base.OnPointerMoved(e);
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		var viewModel = ViewModel;
		if ((viewModel == null)
			|| !e.Properties.IsLeftButtonPressed)
		{
			base.OnPointerPressed(e);
			return;
		}

		var point = e.GetPosition(this);
		var visualX = point.X + Offset.X;
		var visualY = point.Y + Offset.Y;

		if (!ViewModel.Lines.TryGetLineForOffset(visualY, visualY, out var line))
		{
			base.OnPointerPressed(e);
			return;
		}

		var caretOffset = line.GetNearestOffsetAtVisual(visualX, visualY, false);

		if (e.ClickCount >= 2)
		{
			ViewModel.SelectWord(caretOffset);
			base.OnPointerPressed(e);
			return;
		}

		if (caretOffset != ViewModel.Caret.Offset)
		{
			ViewModel.Caret.Move(caretOffset);
		}

		if (ViewModel.Caret.Selection.IsSelecting)
		{
			ViewModel.Caret.Selection.EndOffset = caretOffset;
			InvalidateVisual();
		}
		else
		{
			ViewModel.Caret.Selection.Reset(caretOffset);
		}

		ViewModel.Caret.Selection.StartMouseSelection();

		base.OnPointerPressed(e);
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
		{
			ViewModel.Caret.Selection.StopMouseSelection();
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

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if ((change.Property == CanHorizontallyScrollProperty)
			&& change.NewValue is bool canHorizontallyScroll)
		{
			ViewModel.WordWrap = !canHorizontallyScroll;
		}
		if ((change.Property == OffsetProperty)
			&& change.NewValue is Vector offset)
		{
			ViewModel.ViewMetrics.Offset = offset;
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
			InvalidateVisual();
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
		viewModel.Caret.CaretMoved += OnCaretMoved;
		viewModel.Caret.Selection.Updated += SelectionOnUpdated;
		viewModel.DocumentChanged += OnDocumentChanged;
	}

	private void DetachEvents(TextEditorViewModel viewModel)
	{
		if (viewModel == null)
		{
			return;
		}

		viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
		viewModel.Caret.CaretMoved -= OnCaretMoved;
		viewModel.Caret.Selection.Updated -= SelectionOnUpdated;
		viewModel.DocumentChanged -= OnDocumentChanged;
		_eventsAttached = false;
	}

	private void DispatchTimerCallback(object sender, EventArgs e)
	{
		UpdateCaret();
	}

	private void EnsureCaretVisible(Caret caret)
	{
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

		if (!ViewModel.WordWrap)
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

	private void OnCaretMoved(object sender, EventArgs e)
	{
		var caret = (Caret) sender;
		if (caret.Selection.IsSelectingUsingKeyboard)
		{
			caret.Selection.EndOffset = caret.Offset;
			InvalidateVisual();
		}

		EnsureCaretVisible(caret);
		UpdateCaret();
	}

	private void OnDocumentChanged(object sender, TextDocumentChangedArgs e)
	{
		if (e.Type == TextDocumentChangeType.Reset)
		{
			Offset = new Vector(0, 0);
		}

		if (ViewModel.Lines.LastEditNeedsPaintOnly)
		{
			OnScrollInvalidated();
			InvalidateVisual();
			return;
		}

		InvalidateMeasure();
	}

	private void SelectionOnUpdated(object sender, EventArgs e)
	{
		InvalidateVisual();
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
		}
	}

	#endregion

	#region Events

	public event EventHandler ScrollInvalidated;

	#endregion
}