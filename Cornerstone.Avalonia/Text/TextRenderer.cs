#region References

using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Cornerstone.Avalonia.Text.Models;
using Cornerstone.Avalonia.Text.Rendering;
using Cornerstone.Collections;
using Cornerstone.Reflection;
using IRenderer = Cornerstone.Avalonia.Text.Rendering.IRenderer;
using Line = Cornerstone.Avalonia.Text.Models.Line;
using TextMetrics = Cornerstone.Avalonia.Text.Rendering.TextMetrics;

#endregion

namespace Cornerstone.Avalonia.Text;

[SourceReflection]
public partial class TextRenderer : CornerstoneControl<TextEditorViewModel>, ILogicalScrollable
{
	#region Fields

	private readonly SpeedyList<IRenderer> _backgroundRenderers;
	private readonly CurrentLineRenderer _currentLineRenderer;
	private readonly DispatcherTimer _dispatchTimer;
	private readonly SelectionRenderer _selectionRenderer;

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
		Focusable = false;
		TextMetrics = new TextMetrics();

		VisualChildren.Add(CaretVisual);
	}

	static TextRenderer()
	{
		AffectsRender<TextRenderer>(
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

	[StyledProperty]
	public partial bool CanHorizontallyScroll { get; set; }

	[StyledProperty]
	public partial bool CanVerticallyScroll { get; set; }

	public Size Extent { get; private set; }

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

	public Size PageScrollSize => new(TextMetrics.CharacterWidth * 10, TextMetrics.CharacterWidth * 10);

	public Size ScrollSize => new(TextMetrics.CharacterWidth, TextMetrics.CharacterHeight);

	public Size Viewport { get; private set; }

	internal CaretVisual CaretVisual { get; }

	internal TextMetrics TextMetrics { get; }

	internal Typeface Typeface { get; private set; }

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
		return new TextLayout(
			lineText,
			Typeface,
			FontSize,
			Foreground ?? Brushes.White,
			textWrapping: ViewModel.WordWrap
				? TextWrapping.Wrap
				: TextWrapping.NoWrap,
			maxWidth: ViewModel.WordWrap
				? Extent.Width
				: 999999,
			flowDirection: FlowDirection.LeftToRight
		);
	}

	public void RaiseScrollInvalidated(EventArgs e)
	{
		OnScrollInvalidated();
	}

	public override void Render(DrawingContext drawingContext)
	{
		drawingContext.FillRectangle(Brushes.Transparent, Bounds.Inflate(Margin));

		// Uncomment to see the calculated extent area
		//drawingContext.DrawRectangle(new Pen(Brushes.Red), new Rect(0, 0, Extent.Width, Extent.Height));

		foreach (var renderer in _backgroundRenderers)
		{
			renderer.Draw(this, drawingContext);
		}

		var topY = Offset.Y;
		var bottomY = Offset.Y + Bounds.Bottom;
		var leftX = Offset.X;

		foreach (var line in ViewModel.Document.Lines)
		{
			if (line.VisualLayout.Bottom < topY)
			{
				continue;
			}

			if (line.VisualLayout.Top > bottomY)
			{
				break;
			}

			var lineText = line.ToString();
			var textLayout = GetTextLayout(lineText);
			textLayout.Draw(drawingContext, new(-leftX, line.VisualLayout.Top - topY));
		}
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		Typeface = CornerstoneExtensions.CreateTypeface(this);
		InvalidateDefaultTextMetrics();
		Extent = ViewModel.Document.Lines.Measure(availableSize, !CanHorizontallyScroll, TextMetrics);
		OnScrollInvalidated();
		return Extent;
	}

	protected override void OnGotFocus(GotFocusEventArgs e)
	{
		_dispatchTimer.Start();
		base.OnGotFocus(e);
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		e.Handled = ViewModel.ProcessKeyDownEvent(e);
		base.OnKeyDown(e);
	}

	protected override void OnKeyUp(KeyEventArgs e)
	{
		e.Handled = ViewModel.ProcessKeyUpEvent(e);
		base.OnKeyUp(e);
	}

	protected override void OnLostFocus(RoutedEventArgs e)
	{
		ViewModel.Selection.EndSelection();
		_dispatchTimer.Stop();
		base.OnLostFocus(e);
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		if (ViewModel.Selection.IsSelectingUsingMouse)
		{
			var point = e.GetPosition(this);
			var offset = ScreenPointToDocumentOffset(point);

			if (ViewModel.Selection.EndOffset != offset)
			{
				ViewModel.Selection.EndOffset = offset;
				ViewModel.Caret.Move(offset);

				InvalidateVisual();
			}
		}

		// Auto-scroll when dragging near edge
		//AutoScrollIfNeeded(currentPoint);

		base.OnPointerMoved(e);
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		var viewModel = ViewModel;
		if ((viewModel == null)
			|| !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
		{
			base.OnPointerPressed(e);
			return;
		}

		var point = e.GetPosition(this);
		var caretOffset = ScreenPointToDocumentOffset(point);
		ViewModel.Caret.Move(caretOffset);

		if (ViewModel.Selection.IsSelecting)
		{
			ViewModel.Selection.EndOffset = caretOffset;
			InvalidateVisual();
		}
		else
		{
			ViewModel.Selection.Reset(caretOffset);
		}

		ViewModel.Selection.StartMouseSelection();

		base.OnPointerPressed(e);
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
		{
			ViewModel.Selection.EndMouseSelection();
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

		if (change.Property == ViewModelProperty)
		{
			if (change.OldValue is TextEditorViewModel oldValue)
			{
				oldValue.PropertyChanged -= ViewModelOnPropertyChanged;
				oldValue.Caret.PropertyChanged -= CaretOnPropertyChanged;
				oldValue.Document.DocumentChanged -= OnDocumentChanged;
			}
			if (change.NewValue is TextEditorViewModel newValue)
			{
				newValue.PropertyChanged += ViewModelOnPropertyChanged;
				newValue.Caret.PropertyChanged += CaretOnPropertyChanged;
				newValue.Document.DocumentChanged += OnDocumentChanged;
			}
		}

		base.OnPropertyChanged(change);
	}

	protected virtual void OnScrollInvalidated()
	{
		OnPropertyChanged(nameof(Extent));
		OnPropertyChanged(nameof(Offset));
		OnPropertyChanged(nameof(Viewport));
		ScrollInvalidated?.Invoke(this, EventArgs.Empty);
	}

	protected override void OnSizeChanged(SizeChangedEventArgs e)
	{
		Viewport = e.NewSize;
		OnScrollInvalidated();
		base.OnSizeChanged(e);
	}

	private void CaretOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(ViewModel.Caret.Offset):
			{
				// Only scroll when caret is out of view
				var caret = (Caret) sender;
				//var caretDocY = (caret.Line.LineNumber - 1) * TextMetrics.CharacterHeight;
				//var caretDocX = (caret.Offset - caret.Line.StartOffset) * TextMetrics.CharacterWidth;
				//var visibleRect = new Rect(Offset.X, Offset.Y, Viewport.Width, Viewport.Height);

				

				//if (!visibleRect.Contains(new Point(caretDocX, caretDocY)))
				//{
				//	// biased a bit left, show caret in upper third
				//	var newX = Math.Max(0, caretDocX - (Viewport.Width / 4));
				//	var newY = Math.Max(0, caretDocY - (Viewport.Height / 3));

				//	Offset = new Vector(newX, newY);
				//	RaiseScrollInvalidated(EventArgs.Empty);
				//}

				InvalidateVisual();
				break;
			}
		}

		UpdateCaret();
	}

	private void DispatchTimerCallback(object sender, EventArgs e)
	{
		UpdateCaret();
	}

	private TextHitTestResult HitTestLine(string lineText, Point pointInDocumentSpace)
	{
		if (string.IsNullOrEmpty(lineText))
		{
			return new TextHitTestResult();
		}

		var textLayout = GetTextLayout(lineText);

		// pointInDocumentSpace.X should already be relative to the start of THIS line
		// (i.e. documentX - any line indent if you have indentation later)
		return textLayout.HitTestPoint(pointInDocumentSpace);
	}

	private void InvalidateDefaultTextMetrics()
	{
		var line = GetTextLayout("X");
		TextMetrics.CharacterHeight = line.Height;
		TextMetrics.CharacterWidth = Math.Max(1, line.WidthIncludingTrailingWhitespace);
	}

	private void OnDocumentChanged(object sender, TextDocumentChangedArgs e)
	{
		InvalidateMeasure();
	}

	private Line ScreenPointToDocumentLine(Point screenPoint)
	{
		var documentY = screenPoint.Y + Offset.Y;
		Line line = null;
		for (var index = 0; index < ViewModel.Document.Lines.Count; index++)
		{
			line = ViewModel.Document.Lines[index];
			if ((documentY >= line.VisualLayout.Top)
				&& (documentY <= line.VisualLayout.Bottom))
			{
				break;
			}
		}
		return line!;
	}

	private int ScreenPointToDocumentOffset(Point screenPoint)
	{
		var documentX = screenPoint.X + Offset.X;
		var documentY = screenPoint.Y + Offset.Y;
		Line line = null;
		for (var index = 0; index < ViewModel.Document.Lines.Count; index++)
		{
			line = ViewModel.Document.Lines[index];
			if ((documentY >= line.VisualLayout.Top)
				&& (documentY <= line.VisualLayout.Bottom))
			{
				break;
			}
		}
		var hit = HitTestLine(line!.ToString(), new Point(documentX, documentY - line.VisualLayout.Top));
		var offset = line.StartOffset + hit.TextPosition;
		return offset;
	}

	private void UpdateCaret()
	{
		CaretVisual.InvalidateVisual();

		if (ViewModel.Caret.Line != _currentLineRenderer.CurrentLine)
		{
			InvalidateMeasure();
		}
	}

	private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(ViewModel.WordWrap):
			{
				CanHorizontallyScroll = !ViewModel.WordWrap;
				break;
			}
		}
	}

	#endregion

	#region Events

	public event EventHandler ScrollInvalidated;

	#endregion
}