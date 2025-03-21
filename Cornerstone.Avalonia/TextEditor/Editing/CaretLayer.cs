#region References

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Cornerstone.Avalonia.TextEditor.Rendering;
using Cornerstone.Avalonia.TextEditor.Utils;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Editing;

internal sealed class CaretLayer : Layer
{
	#region Fields

	internal IBrush CaretBrush;
	private bool _blink;

	private readonly DispatcherTimer _caretBlinkTimer;
	private Rect _caretRectangle;

	private bool _isVisible;
	private readonly TextArea _textArea;

	#endregion

	#region Constructors

	public CaretLayer(TextArea textArea) : base(textArea.TextView, KnownLayer.Caret)
	{
		_caretBlinkTimer = new();
		_textArea = textArea;
		IsHitTestVisible = false;
		_caretBlinkTimer.Tick += CaretBlinkTimer_Tick;
	}

	#endregion

	#region Methods

	public void Hide()
	{
		if (_isVisible)
		{
			_isVisible = false;
			StopBlinkAnimation();
			InvalidateVisual();
		}
	}

	public override void Render(DrawingContext drawingContext)
	{
		base.Render(drawingContext);

		if (_isVisible && _blink)
		{
			var caretBrush = CaretBrush ?? TextView.GetValue(TextBlock.ForegroundProperty);

			if (_textArea.OverstrikeMode)
			{
				if (caretBrush is ISolidColorBrush scBrush)
				{
					var brushColor = scBrush.Color;
					var newColor = Color.FromArgb(100, brushColor.R, brushColor.G, brushColor.B);
					caretBrush = new SolidColorBrush(newColor);
				}
			}

			var r = new Rect(_caretRectangle.X - TextView.HorizontalOffset,
				_caretRectangle.Y - TextView.VerticalOffset,
				_caretRectangle.Width,
				_caretRectangle.Height);

			drawingContext.FillRectangle(caretBrush, PixelSnapHelpers.Round(r, PixelSnapHelpers.GetPixelSize(this)));
		}
	}

	public void Show(Rect caretRectangle)
	{
		_caretRectangle = caretRectangle;
		_isVisible = true;
		StartBlinkAnimation();
		InvalidateVisual();
	}

	private void CaretBlinkTimer_Tick(object sender, EventArgs e)
	{
		_blink = !_blink;
		InvalidateVisual();
	}

	private void StartBlinkAnimation()
	{
		// TODO
		var blinkTime = TimeSpan.FromMilliseconds(500); //Win32.CaretBlinkTime;
		_blink = true; // the caret should visible initially
		// This is important if blinking is disabled (system reports a negative blinkTime)
		if (blinkTime.TotalMilliseconds > 0)
		{
			_caretBlinkTimer.Interval = blinkTime;
			_caretBlinkTimer.Start();
		}
	}

	private void StopBlinkAnimation()
	{
		_caretBlinkTimer.Stop();
	}

	#endregion
}