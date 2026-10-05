using System;
using Cornerstone.Data.Bytes;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Drawing;
using Cornerstone.Presentation.Media;
using Cornerstone.Profiling;

namespace Cornerstone.Presentation.Controls
{
	public class ByteSizeLabel : TemplatedControl
	{
		public static readonly StyledProperty<Profiler> ProfilerProperty;
		public static readonly StyledProperty<string> TitleProperty;
		public static readonly StyledProperty<decimal> ValueProperty;

		private readonly DrawingContextHelper _contextHelper;

		static ByteSizeLabel()
		{
			ProfilerProperty = PresentationProperty.Register<ByteSizeLabel, Profiler>(nameof(Profiler));
			TitleProperty = PresentationProperty.Register<ByteSizeLabel, string>(nameof(Title));
			ValueProperty = PresentationProperty.Register<ByteSizeLabel, decimal>(nameof(Value));

			AffectsRender<ByteSizeLabel>(ForegroundProperty);
			AffectsMeasure<ByteSizeLabel>(
				FontFamilyProperty,
				FontSizeProperty,
				FontStyleProperty,
				FontWeightProperty);
		}

		public ByteSizeLabel()
		{
			_contextHelper = new DrawingContextHelper(this);
		}

		public Profiler Profiler
		{
			get => GetValue(ProfilerProperty);
			set => SetValue(ProfilerProperty, value);
		}

		public string Title
		{
			get => GetValue(TitleProperty);
			set => SetValue(TitleProperty, value);
		}

		public decimal Value
		{
			get => GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		public override void Render(DrawingContext context)
		{
			using var start = ProfilerExtensions.Start(Profiler, "Render");
			var border = BorderThickness;
			var borderThickness = border.IsUniform
				? border.Left
				: (border.Left + border.Top + border.Right + border.Bottom) / 4.0;
			var radius = CornerRadius;
			var cornerRadius = (float) (radius.IsUniform
				? radius.TopLeft
				: (radius.TopLeft + radius.TopRight + radius.BottomLeft + radius.BottomRight) / 4.0);
			var backgroundArea = new Rect(Bounds.Size);

			if ((BorderBrush != null) && (borderThickness > 0))
			{
				backgroundArea = backgroundArea.Deflate(borderThickness * 0.5);
				var roundedRect = new RoundedRect(backgroundArea, CornerRadius.TopLeft, CornerRadius.TopRight, CornerRadius.BottomRight, CornerRadius.BottomLeft);
				context.DrawRectangle(Background, new global::Cornerstone.Presentation.Media.Pen(BorderBrush, borderThickness), roundedRect);
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

		protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
		{
			if ((change.Property == ValueProperty) || (change.Property == TitleProperty))
			{
				InvalidateMeasure();
			}
			base.OnPropertyChanged(change);
		}
	}
}
