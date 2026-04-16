#region References

using System;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Cornerstone.Avalonia.Text.Models;
using Cornerstone.Avalonia.Text.Rendering;
using Cornerstone.Avalonia.Themes;
using Cornerstone.Collections;
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

	private readonly PresentationList<IRenderer> _backgroundRenderers;
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
		_backgroundRenderers = [_currentLineRenderer, _selectionRenderer];
		_dispatchTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Background, DispatchTimerCallback);

		CaretVisual = new CaretVisual(this);
		CanVerticallyScroll = true;
		Focusable = true;
		FontSize = 16;
		ViewModel = new TextEditorViewModel();

		VisualChildren.Add(CaretVisual);
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
			FontWeightProperty
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

	public Size Extent => ViewModel.ViewMetrics.DocumentSize;

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

	public Size PageScrollSize => new(ViewModel.ViewMetrics.CharacterWidth * 10, ViewModel.ViewMetrics.CharacterWidth * 10);

	public Size ScrollSize => new(ViewModel.ViewMetrics.CharacterWidth, ViewModel.ViewMetrics.CharacterHeight);

	[DirectProperty]
	public string Text
	{
		get => ViewModel.Buffer.ToString();
		set => ViewModel.Load(value);
	}

	public Size Viewport => ViewModel.ViewMetrics.Viewport;

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

		return new TextLayout(
			lineText,
			typeface,
			FontSize,
			foreground ?? Foreground ?? Brushes.White,
			textWrapping: wrap
				? TextWrapping.Wrap
				: TextWrapping.NoWrap,
			maxWidth: wrap
				? maxWidth
				: 999999,
			flowDirection: FlowDirection.LeftToRight,
			textDecorations: textDecorations
		);
	}

	public void RaiseScrollInvalidated(EventArgs e)
	{
		OnScrollInvalidated();
	}

	public override void Render(DrawingContext drawingContext)
	{
		using var _ = ProfilerExtensions.Start(Profiler, nameof(Render));
		drawingContext.FillRectangle(Brushes.Transparent, Bounds.Inflate(Margin));

		// Uncomment to see the calculated extent area
		//drawingContext.DrawRectangle(new Pen(Brushes.Red), new Rect(0, 0, Extent.Width, Extent.Height));

		foreach (var renderer in _backgroundRenderers)
		{
			renderer.Draw(this, drawingContext);
		}

		var leftX = Offset.X;
		var topY = Offset.Y;
		var bottomY = Offset.Y + Bounds.Bottom;

		foreach (var line in ViewModel.Lines)
		{
			if (line.VisualLayout.Bottom <= topY)
			{
				continue;
			}

			if (line.VisualLayout.Bottom >= bottomY)
			{
				break;
			}

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
			var tokens = ViewModel.TokenManager
				.GetTokens(start, lineEnd)
				.ToArray();

			if (tokens.Length == 0)
			{
				var text = ViewModel.Buffer.Substring(start, length);
				using var layout = GetTextLayout(text, Width, false, Foreground);
				layout.Draw(drawingContext, new Point(-leftX, visualY - topY));
				return;
			}

			var currentX = -leftX;
			var currentPos = start;

			foreach (var token in tokens)
			{
				// Print the gap before the token
				if (token.StartOffset > currentPos)
				{
					var gapLen = Math.Min(token.StartOffset, lineEnd) - currentPos;
					if (gapLen > 0)
					{
						var gapText = ViewModel.Buffer.Substring(currentPos, gapLen);
						using var tl = GetTextLayout(gapText, Width, false, Foreground);
						tl.Draw(drawingContext, new Point(currentX, visualY - topY));
						currentX += tl.WidthIncludingTrailingWhitespace;
					}
					currentPos = token.StartOffset;
				}

				// The formatted part, clipped to current line
				var runStart = Math.Max(token.StartOffset, currentPos);
				var runEnd = Math.Min(token.EndOffset, lineEnd);

				if (runStart < runEnd)
				{
					var brush = SyntaxBrushes.TryGetValue(token.Color, out var b) ? b : Foreground;
					var runText = ViewModel.Buffer.Substring(runStart, runEnd - runStart);
					using var tl = GetTextLayout(runText, Width, false, brush, token.Bold, token.Italic,
						token.Strikethrough ? TextDecorations.Strikethrough : null);
					tl.Draw(drawingContext, new Point(currentX, visualY - topY));
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
				tl.Draw(drawingContext, new Point(currentX, visualY - topY));
			}
		}
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		OnScrollInvalidated();
		return base.ArrangeOverride(finalSize);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		using var _ = ProfilerExtensions.Start(Profiler, nameof(MeasureOverride));
		using var line = GetTextLayout("X", availableSize.Width);
		ViewModel.Measure(line, availableSize);
		OnScrollInvalidated();
		return ViewModel.ViewMetrics.DocumentSize;
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
		_dispatchTimer.IsEnabled = true;
		ViewModel.Caret.IsVisible = true;
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

	protected virtual void OnScrollInvalidated()
	{
		OnPropertyChanged(nameof(Extent));
		OnPropertyChanged(nameof(Offset));
		OnPropertyChanged(nameof(Viewport));
		ScrollInvalidated?.Invoke(this, EventArgs.Empty);
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

	private Typeface GetTypeface(bool bold, bool italic)
	{
		// Lazy initialization + caching based on exact combination
		if (bold)
		{
			if (italic)
			{
				return _typefaceBoldItalic ??= this.CreateTypeface(FontWeight.SemiBold, FontStyle.Italic);
			}

			return _typefaceBold ??= this.CreateTypeface(FontWeight.SemiBold, FontStyle.Normal);
		}

		if (italic)
		{
			return _typefaceItalic ??= this.CreateTypeface(FontWeight.Normal, FontStyle.Italic);
		}

		return _typefaceNormal ??= this.CreateTypeface(FontWeight.Normal, FontStyle.Normal);
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

		InvalidateMeasure();
	}

	private void SelectionOnUpdated(object sender, EventArgs e)
	{
		InvalidateVisual();
	}

	private void UpdateCaret()
	{
		CaretVisual.InvalidateVisual();

		if (ViewModel.Caret.Line != _currentLineRenderer.CurrentLine)
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