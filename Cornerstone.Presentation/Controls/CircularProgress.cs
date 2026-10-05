using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Controls.Automation.Peers;
using Cornerstone.Presentation.Controls.Metadata;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Controls.StyleClasses;

namespace Cornerstone.Presentation.Controls
{
	/// <summary>
	/// A circular indicator for the progress of an operation.
	/// </summary>
	[PseudoClasses(":indeterminate")]
	public class CircularProgress : RangeBase
	{
		/// <summary>
		/// Defines the <see cref="Content"/> property.
		/// </summary>
		public static readonly StyledProperty<object> ContentProperty =
			ContentControl.ContentProperty.AddOwner<CircularProgress>();

		/// <summary>
		/// Defines the <see cref="IsIndeterminate"/> property.
		/// </summary>
		public static readonly StyledProperty<bool> IsIndeterminateProperty =
			PresentationProperty.Register<CircularProgress, bool>(nameof(IsIndeterminate));

		/// <summary>
		/// Defines the <see cref="ShowProgressText"/> property.
		/// </summary>
		public static readonly StyledProperty<bool> ShowProgressTextProperty =
			PresentationProperty.Register<CircularProgress, bool>(nameof(ShowProgressText));

		/// <summary>
		/// Defines the <see cref="ProgressTextFormat"/> property.
		/// </summary>
		public static readonly StyledProperty<string> ProgressTextFormatProperty =
			PresentationProperty.Register<CircularProgress, string>(nameof(ProgressTextFormat), "{1:0}%");

		/// <summary>
		/// Defines the <see cref="Stroke"/> property.
		/// </summary>
		public static readonly StyledProperty<IBrush> StrokeProperty =
			PresentationProperty.Register<CircularProgress, IBrush>(nameof(Stroke));

		/// <summary>
		/// Defines the <see cref="StrokeLineCap"/> property.
		/// </summary>
		public static readonly StyledProperty<PenLineCap> StrokeLineCapProperty =
			PresentationProperty.Register<CircularProgress, PenLineCap>(nameof(StrokeLineCap), PenLineCap.Round);

		/// <summary>
		/// Defines the <see cref="StrokeThickness"/> property.
		/// </summary>
		public static readonly StyledProperty<int> StrokeThicknessProperty =
			PresentationProperty.Register<CircularProgress, int>(nameof(StrokeThickness), 8);

		/// <summary>
		/// Defines the <see cref="Percentage"/> property.
		/// </summary>
		public static readonly DirectProperty<CircularProgress, double> PercentageProperty =
			PresentationProperty.RegisterDirect<CircularProgress, double>(
				nameof(Percentage),
				o => o.Percentage);

		/// <summary>
		/// Defines the <see cref="SweepAngle"/> property.
		/// </summary>
		public static readonly DirectProperty<CircularProgress, double> SweepAngleProperty =
			PresentationProperty.RegisterDirect<CircularProgress, double>(
				nameof(SweepAngle),
				o => o.SweepAngle);

		private double _percentage;
		private double _radius;
		private double _sweepAngle;

		static CircularProgress()
		{
			ValueProperty.OverrideMetadata<CircularProgress>(new(defaultBindingMode: BindingMode.OneWay));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CircularProgress"/> class.
		/// </summary>
		public CircularProgress()
		{
			_percentage = 0;
			_radius = 0;
			_sweepAngle = 0;
			UpdatePseudoClasses(IsIndeterminate);
		}

		/// <summary>
		/// Gets or sets the content displayed in the center of the control.
		/// </summary>
		public object Content
		{
			get => GetValue(ContentProperty);
			set => SetValue(ContentProperty, value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether the control shows a generic continues indicator.
		/// </summary>
		public bool IsIndeterminate
		{
			get => GetValue(IsIndeterminateProperty);
			set => SetValue(IsIndeterminateProperty, value);
		}

		/// <summary>
		/// Gets the overall percentage complete of the progress.
		/// </summary>
		public double Percentage
		{
			get => _percentage;
			private set => SetAndRaise(PercentageProperty, ref _percentage, value);
		}

		/// <summary>
		/// Gets or sets the format string applied to the internally calculated progress text.
		/// </summary>
		public string ProgressTextFormat
		{
			get => GetValue(ProgressTextFormatProperty);
			set => SetValue(ProgressTextFormatProperty, value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether progress text will be shown.
		/// </summary>
		public bool ShowProgressText
		{
			get => GetValue(ShowProgressTextProperty);
			set => SetValue(ShowProgressTextProperty, value);
		}

		/// <summary>
		/// Gets or sets the brush used to paint the progress arc.
		/// </summary>
		public IBrush Stroke
		{
			get => GetValue(StrokeProperty);
			set => SetValue(StrokeProperty, value);
		}

		/// <summary>
		/// Gets or sets the line cap of the progress arc.
		/// </summary>
		public PenLineCap StrokeLineCap
		{
			get => GetValue(StrokeLineCapProperty);
			set => SetValue(StrokeLineCapProperty, value);
		}

		/// <summary>
		/// Gets or sets the thickness of the progress arc.
		/// </summary>
		public int StrokeThickness
		{
			get => GetValue(StrokeThicknessProperty);
			set => SetValue(StrokeThicknessProperty, value);
		}

		/// <summary>
		/// Gets the sweep angle of the progress arc in degrees.
		/// </summary>
		public double SweepAngle
		{
			get => _sweepAngle;
			private set => SetAndRaise(SweepAngleProperty, ref _sweepAngle, value);
		}

		/// <inheritdoc/>
		protected override Size MeasureOverride(Size availableSize)
		{
			_radius = availableSize.Height / 2;
			_radius -= StrokeThickness;
			RenderArc();
			return new Size(_radius * 2, _radius * 2);
		}

		/// <inheritdoc/>
		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new CircularProgressAutomationPeer(this);
		}

		/// <inheritdoc/>
		protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
		{
			base.OnPropertyChanged(change);

			if ((change.Property == ValueProperty)
				|| (change.Property == MinimumProperty)
				|| (change.Property == MaximumProperty)
				|| (change.Property == StrokeThicknessProperty))
			{
				RenderArc();
			}

			if (change.Property == IsIndeterminateProperty)
			{
				UpdatePseudoClasses(change.GetNewValue<bool>());
			}
		}

		private void RenderArc()
		{
			var total = Maximum - Minimum;
			var value = Value - Minimum;
			double percentage;

			if (value <= 0)
			{
				percentage = 0;
			}
			else if (value > total)
			{
				percentage = 1;
			}
			else
			{
				percentage = total > 0 ? value / total : 0;
			}

			SweepAngle = percentage * 360;
			Percentage = percentage * 100;
		}

		private void UpdatePseudoClasses(bool isIndeterminate)
		{
			PseudoClasses.Set(":indeterminate", isIndeterminate);
		}
	}
}
