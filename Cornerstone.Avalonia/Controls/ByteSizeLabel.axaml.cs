#region References

using System;
using Avalonia;
using Avalonia.Media;
using Cornerstone.Avalonia.Drawing;
using Cornerstone.Avalonia.Text;
using Cornerstone.Data.Bytes;
using Cornerstone.Profiling;

#endregion

namespace Cornerstone.Avalonia.Controls;

public partial class ByteSizeLabel : CornerstoneTemplatedControl
{
	#region Fields

	private readonly DrawingContextHelper _contextHelper;

	#endregion

	#region Constructors

	public ByteSizeLabel()
	{
		_contextHelper = new DrawingContextHelper(this);
	}

	static ByteSizeLabel()
	{
		AffectsRender<TextRenderer>(
			ForegroundProperty
		);

		AffectsMeasure<TextRenderer>(
			FontFamilyProperty,
			FontSizeProperty,
			FontStyleProperty,
			FontWeightProperty
		);
	}

	#endregion

	#region Properties

	[StyledProperty]
	public partial string Title { get; set; }

	[StyledProperty]
	public partial decimal Value { get; set; }

	#endregion

	#region Methods

	public override void Render(DrawingContext context)
	{
		using var start = ProfilerExtensions.Start(Profiler, "Render");
		var borderThickness = CornerstoneExtensions.GetBestSingle(BorderThickness);
		var cornerRadius = (float) CornerstoneExtensions.GetBestSingle(CornerRadius);
		var backgroundArea = new Rect(Bounds.Size);

		if ((BorderBrush != null) && (borderThickness > 0))
		{
			backgroundArea = backgroundArea.Deflate(borderThickness * 0.5);
			var roundedRect = new RoundedRect(backgroundArea, CornerRadius.TopLeft, CornerRadius.TopRight, CornerRadius.BottomRight, CornerRadius.BottomLeft);
			context.DrawRectangle(Background, new Pen(BorderBrush, borderThickness), roundedRect);
			backgroundArea = backgroundArea.Deflate(borderThickness * 0.5);
		}
		else
		{
			context.DrawRectangle(Background, null, backgroundArea);
		}

		var clippedRect = new RoundedRect(backgroundArea, cornerRadius);
		using var _ = context.PushClip(clippedRect);

		var visualX = Padding.Left;
		var visualY = Padding.Top;
		var byteSize = new ByteSize(Value);

		if (Title != null)
		{
			_contextHelper.Draw(context, Title, ref visualX, ref visualY);
			visualX += 10;
		}

		_contextHelper.Draw(context, byteSize.LargestWholeNumberValue, ref visualX, ref visualY);
		visualX += 6;
		_contextHelper.Draw(context, byteSize.GetLargestByteUnit(), ref visualX, ref visualY);

		base.Render(context);
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		using var _ = ProfilerExtensions.Start(Profiler, "Measure");

		var w = Padding.Left + Padding.Right;
		var h = _contextHelper.SpriteHeight + Padding.Top + Padding.Bottom;

		if (Title != null)
		{
			w += _contextHelper.Measure(Title) + 10;
		}

		var byteSize = new ByteSize(Value);
		w += _contextHelper.Measure(byteSize.LargestWholeNumberValue);
		w += 6;
		w += _contextHelper.Measure(byteSize.GetLargestByteUnit());

		return new Size(Math.Max(w, 80), h);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if (change.Property == ValueProperty)
		{
			InvalidateMeasure();
		}
		base.OnPropertyChanged(change);
	}

	#endregion
}