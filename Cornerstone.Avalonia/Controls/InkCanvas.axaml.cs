#region References

using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Cornerstone.Avalonia.Resources;
using Cornerstone.Collections;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Avalonia.Controls;

public sealed partial class InkCanvas : CornerstoneControl
{
	#region Fields

	private ImmutablePen _currentPen;
	private InkCanvasStroke _currentStroke;
	private bool _isErasing;
	private IBrush _lastBrush;

	#endregion

	#region Constructors

	public InkCanvas()
	{
		History = [];
		Stroke ??= ResourceService.GetColorAsBrush("Foreground00");
		Background ??= Brushes.Transparent;
	}

	#endregion

	#region Properties

	[StyledProperty]
	public partial IBrush Background { get; set; }

	public SpeedyList<InkCanvasStroke> History { get; }

	[StyledProperty]
	public partial IBrush Stroke { get; set; }

	#endregion

	#region Methods

	public void Clear()
	{
		History.Clear();
		InvalidateVisual();
	}

	public override void Render(DrawingContext context)
	{
		base.Render(context);

		var rect = new Rect(0, 0, Bounds.Width, Bounds.Height);
		context.FillRectangle(Background, rect);

		foreach (var stroke in History)
		{
			if (stroke.Points.Count < 2)
			{
				continue;
			}

			var brush = stroke.Brush?.ToImmutable();

			if (!ReferenceEquals(brush, _lastBrush))
			{
				_currentPen = new ImmutablePen(brush, 2, null, PenLineCap.Round);
				_lastBrush = brush;
			}

			var pen = _currentPen!;

			for (var i = 1; i < stroke.Points.Count; i++)
			{
				context.DrawLine(pen, stroke.Points[i - 1], stroke.Points[i]);
			}
		}
	}

	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		History.ListUpdated += HistoryOnListUpdated;
		base.OnAttachedToVisualTree(e);
	}

	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		History.ListUpdated -= HistoryOnListUpdated;
		base.OnDetachedFromVisualTree(e);
	}

	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);

		if (_currentStroke != null)
		{
			var point = e.GetCurrentPoint(this).Position;
			_currentStroke.Points.Add(point);
			InvalidateVisual();
		}
		else if (_isErasing)
		{
			EraseAt(e.GetCurrentPoint(this).Position);
		}

		e.Handled = true;
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);

		var point = e.GetCurrentPoint(this).Position;

		if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed
			|| e.KeyModifiers.HasAllFlags(KeyModifiers.Control))
		{
			_isErasing = true;

			EraseAt(point);
		}
		else
		{
			_isErasing = false;
			_currentStroke = new InkCanvasStroke
			{
				Brush = Stroke.ToImmutable(),
				Id = ShortGuid.NewGuid().ToString(),
				Points = [point]
			};

			History.Add(_currentStroke);
		}

		e.Pointer.Capture(this);
		e.Handled = true;
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		base.OnPointerReleased(e);

		_currentStroke = null;
		_isErasing = false;
		e.Pointer.Capture(null);
		e.Handled = true;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == StrokeProperty)
		{
			_currentStroke = null;
		}

		base.OnPropertyChanged(change);
	}

	private void EraseAt(Point point)
	{
		for (var i = History.Count - 1; i >= 0; i--)
		{
			var stroke = History[i];
			foreach (var p in stroke.Points)
			{
				if (!(Point.Distance(p, point) < 4))
				{
					continue;
				}

				History.RemoveAt(i);
				return;
			}
		}
	}

	private void HistoryOnListUpdated(object sender, SpeedyListUpdatedEventArg<InkCanvasStroke> e)
	{
		InvalidateVisual();
	}

	#endregion
}