#region References

using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;

internal sealed class CurrentLineHighlightRenderer : IBackgroundRenderer
{
	#region Fields

	public static readonly Color DefaultBackground = Color.FromArgb(0x55, 0, 0, 0);
	public static readonly Color DefaultBorder = Color.FromArgb(0x34, 0xCC, 0xCC, 0xCC);

	private int _line;
	private readonly TextView _textView;

	#endregion

	#region Constructors

	public CurrentLineHighlightRenderer(TextView textView)
	{
		BorderPen = new ImmutablePen(new ImmutableSolidColorBrush(DefaultBorder));
		BackgroundBrush = new ImmutableSolidColorBrush(DefaultBackground);

		_textView = textView ?? throw new ArgumentNullException(nameof(textView));
		_textView.BackgroundRenderers.Add(this);

		_line = 0;
	}

	#endregion

	#region Properties

	public IBrush BackgroundBrush { get; set; }

	public IPen BorderPen { get; set; }

	public KnownLayer Layer => KnownLayer.Background;

	public int Line
	{
		get => _line;
		set
		{
			if (_line != value)
			{
				_line = value;
				_textView.InvalidateLayer(Layer);
			}
		}
	}

	#endregion

	#region Methods

	public void Draw(TextView textView, DrawingContext drawingContext)
	{
		if (!_textView.Settings.HighlightCurrentLine)
		{
			return;
		}

		var builder = new BackgroundGeometryBuilder();

		var visualLine = _textView.GetVisualLine(_line);
		if (visualLine == null)
		{
			return;
		}

		var linePosY = visualLine.VisualTop - _textView.ScrollOffset.Y;
		builder.AddRectangle(textView, new Rect(0, linePosY, textView.Bounds.Width, visualLine.Height));

		var geometry = builder.CreateGeometry();
		if (geometry != null)
		{
			drawingContext.DrawGeometry(BackgroundBrush, BorderPen, geometry);
		}
	}

	#endregion
}