using System;
using System.Threading;
using Cornerstone.Presentation.Animation;
using Cornerstone.Presentation.Animation.Easings;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Controls.Automation.Peers;
using Cornerstone.Presentation.Controls.Metadata;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.StyleClasses;

namespace Cornerstone.Presentation.Controls
{
    /// <summary>
    /// A control used to indicate the progress of an operation.
    /// </summary>
    [TemplatePart("PART_Indicator", typeof(Border), IsRequired = true)]
    [PseudoClasses(":vertical", ":horizontal", ":indeterminate")]
    public class ProgressBar : RangeBase
    {
        /// <summary>
        /// Provides calculated values for use with the <see cref="ProgressBar"/>'s control theme or template.
        /// </summary>
        /// <remarks>
        /// This class is NOT intended for general use outside of control templates.
        /// </remarks>
        public class ProgressBarTemplateSettings : PresentationObject
        {
            private double _container2Width;
            private double _containerWidth;
            private double _containerAnimationStartPosition;
            private double _containerAnimationEndPosition;
            private double _container2AnimationStartPosition;
            private double _container2AnimationEndPosition;
            private double _indeterminateStartingOffset;
            private double _indeterminateEndingOffset;

            /// <summary>
            /// Defines the <see cref="ContainerAnimationStartPosition"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, double>
                ContainerAnimationStartPositionProperty =
                    PresentationProperty.RegisterDirect<ProgressBarTemplateSettings, double>(
                        nameof(ContainerAnimationStartPosition),
                        p => p.ContainerAnimationStartPosition,
                        (p, o) => p.ContainerAnimationStartPosition = o);

            /// <summary>
            /// Defines the <see cref="ContainerAnimationEndPosition"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, double>
                ContainerAnimationEndPositionProperty =
                    PresentationProperty.RegisterDirect<ProgressBarTemplateSettings, double>(
                        nameof(ContainerAnimationEndPosition),
                        p => p.ContainerAnimationEndPosition,
                        (p, o) => p.ContainerAnimationEndPosition = o);

            /// <summary>
            /// Defines the <see cref="Container2AnimationStartPosition"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, double>
                Container2AnimationStartPositionProperty =
                    PresentationProperty.RegisterDirect<ProgressBarTemplateSettings, double>(
                        nameof(Container2AnimationStartPosition),
                        p => p.Container2AnimationStartPosition,
                        (p, o) => p.Container2AnimationStartPosition = o);

            /// <summary>
            /// Defines the <see cref="Container2AnimationEndPosition"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, double>
                Container2AnimationEndPositionProperty =
                    PresentationProperty.RegisterDirect<ProgressBarTemplateSettings, double>(
                        nameof(Container2AnimationEndPosition),
                        p => p.Container2AnimationEndPosition,
                        (p, o) => p.Container2AnimationEndPosition = o);

            /// <summary>
            /// Defines the <see cref="Container2Width"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, double> Container2WidthProperty =
                PresentationProperty.RegisterDirect<ProgressBarTemplateSettings, double>(
                    nameof(Container2Width),
                    p => p.Container2Width,
                    (p, o) => p.Container2Width = o);

            /// <summary>
            /// Defines the <see cref="ContainerWidth"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, double> ContainerWidthProperty =
                PresentationProperty.RegisterDirect<ProgressBarTemplateSettings, double>(
                    nameof(ContainerWidth),
                    p => p.ContainerWidth,
                    (p, o) => p.ContainerWidth = o);

            /// <summary>
            /// Defines the <see cref="IndeterminateStartingOffset"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, double> IndeterminateStartingOffsetProperty =
                PresentationProperty.RegisterDirect<ProgressBarTemplateSettings, double>(
                    nameof(IndeterminateStartingOffset),
                    p => p.IndeterminateStartingOffset,
                    (p, o) => p.IndeterminateStartingOffset = o);
            
            /// <summary>
            /// Defines the <see cref="IndeterminateEndingOffset"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, double> IndeterminateEndingOffsetProperty =
                PresentationProperty.RegisterDirect<ProgressBarTemplateSettings, double>(
                    nameof(IndeterminateEndingOffset),
                    p => p.IndeterminateEndingOffset,
                    (p, o) => p.IndeterminateEndingOffset = o);

            /// <summary>
            /// Used by Cornerstone.Presentation.Themes.Fluent to define the first indeterminate indicator's width.
            /// </summary>
            public double ContainerWidth
            {
                get => _containerWidth;
                set => SetAndRaise(ContainerWidthProperty, ref _containerWidth, value);
            }
            
            /// <summary>
            /// Used by Cornerstone.Presentation.Themes.Fluent to define the second indeterminate indicator's width.
            /// </summary>
            public double Container2Width
            {
                get => _container2Width;
                set => SetAndRaise(Container2WidthProperty, ref _container2Width, value);
            }

            /// <summary>
            /// Used by Cornerstone.Presentation.Themes.Fluent to define the first indeterminate indicator's start position when animated.
            /// </summary>
            public double ContainerAnimationStartPosition
            {
                get => _containerAnimationStartPosition;
                set => SetAndRaise(ContainerAnimationStartPositionProperty, ref _containerAnimationStartPosition,
                    value);
            }

            /// <summary>
            /// Used by Cornerstone.Presentation.Themes.Fluent to define the first indeterminate indicator's end position when animated.
            /// </summary>
            public double ContainerAnimationEndPosition
            {
                get => _containerAnimationEndPosition;
                set => SetAndRaise(ContainerAnimationEndPositionProperty, ref _containerAnimationEndPosition, value);
            }

            /// <summary>
            /// Used by Cornerstone.Presentation.Themes.Fluent to define the second indeterminate indicator's start position when animated.
            /// </summary>
            public double Container2AnimationStartPosition
            {
                get => _container2AnimationStartPosition;
                set => SetAndRaise(Container2AnimationStartPositionProperty, ref _container2AnimationStartPosition,
                    value);
            }
            
            /// <summary>
            /// Used by Cornerstone.Presentation.Themes.Fluent to define the second indeterminate indicator's end position when animated.
            /// </summary>
            public double Container2AnimationEndPosition
            {
                get => _container2AnimationEndPosition;
                set => SetAndRaise(Container2AnimationEndPositionProperty, ref _container2AnimationEndPosition, value);
            }

            /// <summary>
            /// Used by Cornerstone.Presentation.Themes.Simple  to define the starting point of its indeterminate animation.
            /// </summary>
            public double IndeterminateStartingOffset
            {
                get => _indeterminateStartingOffset;
                set => SetAndRaise(IndeterminateStartingOffsetProperty, ref _indeterminateStartingOffset, value);
            }
            
            /// <summary>
            /// Used by Cornerstone.Presentation.Themes.Simple to define the ending point of its indeterminate animation.
            /// </summary>
            public double IndeterminateEndingOffset
            {
                get => _indeterminateEndingOffset;
                set => SetAndRaise(IndeterminateEndingOffsetProperty, ref _indeterminateEndingOffset, value);
            }
        }

        private double _percentage;
        private Border? _indicator;
        private IDisposable? _trackSizeChangedListener;
        private global::Cornerstone.Presentation.Animation.Animation _animation;
        private CancellationTokenSource _animationCancellationTokenSource;

        /// <summary>
        /// Defines the <see cref="IsIndeterminate"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsIndeterminateProperty =
            PresentationProperty.Register<ProgressBar, bool>(nameof(IsIndeterminate));

        /// <summary>
        /// Defines the <see cref="ShowProgressText"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowProgressTextProperty =
            PresentationProperty.Register<ProgressBar, bool>(nameof(ShowProgressText));

        /// <summary>
        /// Defines the <see cref="ProgressTextFormat"/> property.
        /// </summary>
        public static readonly StyledProperty<string> ProgressTextFormatProperty =
            PresentationProperty.Register<ProgressBar, string>(nameof(ProgressTextFormat), "{1:0}%");

        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly StyledProperty<Orientation> OrientationProperty =
            PresentationProperty.Register<ProgressBar, Orientation>(nameof(Orientation));

        /// <summary>
        /// Defines the <see cref="IndeterminateDuration"/> property.
        /// </summary>
        public static readonly StyledProperty<TimeSpan> IndeterminateDurationProperty =
            PresentationProperty.Register<ProgressBar, TimeSpan>(nameof(IndeterminateDuration), TimeSpan.FromSeconds(1.5));

        /// <summary>
        /// Defines the <see cref="Percentage"/> property.
        /// </summary>
        public static readonly DirectProperty<ProgressBar, double> PercentageProperty =
            PresentationProperty.RegisterDirect<ProgressBar, double>(
                nameof(Percentage),
                o => o.Percentage);

        /// <summary>
        /// Gets the overall percentage complete of the progress 
        /// </summary>
        /// <remarks>
        /// This read-only property is automatically calculated using the current <see cref="RangeBase.Value"/> and
        /// the effective range (<see cref="RangeBase.Maximum"/> - <see cref="RangeBase.Minimum"/>).
        /// </remarks>
        public double Percentage
        {
            get => _percentage;
            private set => SetAndRaise(PercentageProperty, ref _percentage, value);
        }

        static ProgressBar()
        {
            ValueProperty.OverrideMetadata<ProgressBar>(new(defaultBindingMode: BindingMode.OneWay));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressBar"/> class.
        /// </summary>
        public ProgressBar()
        {
            _animation = null;
            _animationCancellationTokenSource = null;
            UpdatePseudoClasses(IsIndeterminate, Orientation);
        }

        /// <summary>
        /// Gets or sets the TemplateSettings for the <see cref="ProgressBar"/>.
        /// </summary>
        public ProgressBarTemplateSettings TemplateSettings { get; } = new ProgressBarTemplateSettings();

        /// <summary>
        /// Gets or sets a value indicating whether the progress bar shows the actual value or a generic,
        /// continues progress indicator (indeterminate state).
        /// </summary>
        public bool IsIndeterminate
        {
            get => GetValue(IsIndeterminateProperty);
            set => SetValue(IsIndeterminateProperty, value);
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
        /// Gets or sets the format string applied to the internally calculated progress text before it is shown.
        /// </summary>
        public string ProgressTextFormat
        {
            get => GetValue(ProgressTextFormatProperty);
            set => SetValue(ProgressTextFormatProperty, value);
        }

        /// <summary>
        /// Gets or sets the orientation of the <see cref="ProgressBar"/>.
        /// </summary>
        public Orientation Orientation
        {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <summary>
        /// Gets or sets how long one indeterminate sweep takes.
        /// </summary>
        public TimeSpan IndeterminateDuration
        {
            get => GetValue(IndeterminateDurationProperty);
            set => SetValue(IndeterminateDurationProperty, value);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var result = base.ArrangeOverride(finalSize);
            UpdateIndicator();
            return result;
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ValueProperty ||
                change.Property == MinimumProperty ||
                change.Property == MaximumProperty ||
                change.Property == IsIndeterminateProperty ||
                change.Property == OrientationProperty)
            {
                UpdateIndicator();
            }

            if ((change.Property == IsIndeterminateProperty)
                || (change.Property == IsVisibleProperty)
                || (change.Property == BoundsProperty)
                || (change.Property == IndeterminateDurationProperty))
            {
                UpdateIndeterminateAnimation();
            }

            if (change.Property == IsIndeterminateProperty)
            {
                UpdatePseudoClasses(change.GetNewValue<bool>(), null);
            }
            else if (change.Property == OrientationProperty)
            {
                UpdatePseudoClasses(null, change.GetNewValue<Orientation>());
            }
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            LayoutUpdated += OnLayoutUpdated;
            base.OnAttachedToVisualTree(e);
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            LayoutUpdated -= OnLayoutUpdated;
            StopAnimation();
            base.OnDetachedFromVisualTree(e);
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            // dispose any previous track size listener
            _trackSizeChangedListener?.Dispose();

            _indicator = e.NameScope.Get<Border>("PART_Indicator");

            // listen to size changes of the indicators track (parent) and update the indicator there. 
            _trackSizeChangedListener = _indicator.Parent?.GetPropertyChangedObservable(BoundsProperty)
                .Subscribe(_ => UpdateIndicator());

            UpdateIndicator();
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ProgressBarAutomationPeer(this);
        }

        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            if (_animation == null)
            {
                UpdateIndeterminateAnimation();
            }
        }

        private void CancelAnimation()
        {
            if (_animationCancellationTokenSource != null)
            {
                _animationCancellationTokenSource.Cancel();
                _animationCancellationTokenSource.Dispose();
                _animationCancellationTokenSource = null;
            }

            _animation = null;
        }

        private void ResetIndicatorTransform()
        {
            if (_indicator != null)
            {
                _indicator.RenderTransform = new TranslateTransform { X = 0, Y = 0 };
            }
        }

        private void StopAnimation()
        {
            CancelAnimation();
            ResetIndicatorTransform();
        }

        private void UpdateIndeterminateAnimation()
        {
            if (_indicator == null)
            {
                return;
            }

            CancelAnimation();

            if (!IsIndeterminate || !IsVisible)
            {
                ResetIndicatorTransform();
                return;
            }

            var isHorizontal = Orientation == Orientation.Horizontal;
            var barSize = _indicator.VisualParent?.Bounds.Size ?? Bounds.Size;
            var track = isHorizontal ? barSize.Width : barSize.Height;
            var bar = track * 0.4;

            if ((track <= 0) || (bar <= 0))
            {
                return;
            }

            // Vertical indicator is bottom-aligned, so a shift of -bar only slides it up by its own height
            // and leaves it about 20% down a track whose indicator is 40% of the track.
            var start = isHorizontal ? 0.0 - bar : 0.0 - track;
            var end = isHorizontal ? track : bar;
            var property = isHorizontal ? TranslateTransform.XProperty : TranslateTransform.YProperty;
            _indicator.RenderTransform = isHorizontal
                ? new TranslateTransform { X = start, Y = 0 }
                : new TranslateTransform { X = 0, Y = start };

            var duration = IndeterminateDuration;
            if (duration.TotalMilliseconds < 1000)
            {
                duration = TimeSpan.FromMilliseconds(1000);
            }

            _animation = new global::Cornerstone.Presentation.Animation.Animation
            {
                Duration = duration,
                IterationCount = IterationCount.Infinite,
                PlaybackDirection = PlaybackDirection.Normal,
                Easing = new ExponentialEaseInOut()
            };

            var startFrame = new KeyFrame { Cue = new Cue(0.0) };
            startFrame.Setters.Add(new Setter { Property = property, Value = start });

            var endFrame = new KeyFrame { Cue = new Cue(1.0) };
            endFrame.Setters.Add(new Setter { Property = property, Value = end });

            _animation.Children.Add(startFrame);
            _animation.Children.Add(endFrame);

            _animationCancellationTokenSource = new CancellationTokenSource();
            _animation.RunAsync(_indicator, _animationCancellationTokenSource.Token);
        }

        private void UpdateIndicator()
        {
            // Gets the size of the parent indicator container
            var barSize = _indicator?.VisualParent?.Bounds.Size ?? Bounds.Size;

            if (_indicator == null)
            {
                return;
            }

            if (IsIndeterminate)
            {
                // Pulled from ModernWPF.

                var dim = Orientation == Orientation.Horizontal ? barSize.Width : barSize.Height;
                var barIndicatorWidth = dim * 0.4; // Indicator width at 40% of ProgressBar
                var barIndicatorWidth2 = dim * 0.6; // Indicator width at 60% of ProgressBar

                TemplateSettings.ContainerWidth = barIndicatorWidth;
                TemplateSettings.Container2Width = barIndicatorWidth2;

                TemplateSettings.ContainerAnimationStartPosition = barIndicatorWidth * -1.8; // Position at -180%
                TemplateSettings.ContainerAnimationEndPosition = barIndicatorWidth * 3.0; // Position at 300%

                TemplateSettings.Container2AnimationStartPosition = barIndicatorWidth2 * -1.5; // Position at -150%
                TemplateSettings.Container2AnimationEndPosition = barIndicatorWidth2 * 1.66; // Position at 166%

                TemplateSettings.IndeterminateStartingOffset = -dim;
                TemplateSettings.IndeterminateEndingOffset = dim;

                if (Orientation == Orientation.Horizontal)
                {
                    _indicator.Width = barIndicatorWidth > 0 ? barIndicatorWidth : 0;
                    _indicator.Height = double.NaN;
                    if (_animation == null)
                    {
                        _indicator.RenderTransform = new TranslateTransform { X = -barIndicatorWidth, Y = 0 };
                    }
                }
                else
                {
                    _indicator.Width = double.NaN;
                    _indicator.Height = barIndicatorWidth > 0 ? barIndicatorWidth : 0;
                    if (_animation == null)
                    {
                        _indicator.RenderTransform = new TranslateTransform { X = 0, Y = -dim };
                    }
                }
            }
            else
            {
                StopAnimation();

                var percent = Math.Abs(Maximum - Minimum) < double.Epsilon ?
                    1.0 :
                    (Value - Minimum) / (Maximum - Minimum);

                if (percent < 0)
                {
                    percent = 0;
                }
                else if (percent > 1)
                {
                    percent = 1;
                }

                // When the Orientation changed, the indicator's Width or Height should set to double.NaN.
                // Indicator size calculation should consider the ProgressBar's Padding property setting
                if (Orientation == Orientation.Horizontal)
                {
                    var width = (barSize.Width - _indicator.Margin.Left - _indicator.Margin.Right) * percent;
                    _indicator.Width = width > 0 ? width : 0;
                    _indicator.Height = double.NaN;
                }
                else
                {
                    _indicator.Width = double.NaN;
                    var height = (barSize.Height - _indicator.Margin.Top - _indicator.Margin.Bottom) * percent;
                    _indicator.Height = height > 0 ? height : 0;
                }

                Percentage = percent * 100;
            }
        }

        private void UpdatePseudoClasses(
            bool? isIndeterminate,
            Orientation? o)
        {
            if (isIndeterminate.HasValue)
            {
                PseudoClasses.Set(":indeterminate", isIndeterminate.Value);
            }

            if (!o.HasValue) return;
            PseudoClasses.Set(":vertical", o == Orientation.Vertical);
            PseudoClasses.Set(":horizontal", o == Orientation.Horizontal);
        }
    }
}
