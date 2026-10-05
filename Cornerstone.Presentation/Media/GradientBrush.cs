using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

using Cornerstone.Presentation.Animation.Animators;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Media.Immutable;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.Rendering.Composition;
using Cornerstone.Presentation.Rendering.Composition.Transport;

namespace Cornerstone.Presentation.Media
{
    /// <summary>
    /// Base class for brushes that draw with a gradient.
    /// </summary>
    public abstract class GradientBrush : Brush, IGradientBrush, IMutableBrush
    {
        /// <summary>
        /// Defines the <see cref="SpreadMethod"/> property.
        /// </summary>
        public static readonly StyledProperty<GradientSpreadMethod> SpreadMethodProperty =
            PresentationProperty.Register<GradientBrush, GradientSpreadMethod>(nameof(SpreadMethod));

        /// <summary>
        /// Defines the <see cref="GradientStops"/> property.
        /// </summary>
        public static readonly StyledProperty<GradientStops> GradientStopsProperty =
            PresentationProperty.Register<GradientBrush, GradientStops>(nameof(GradientStops));

        private IDisposable? _gradientStopsSubscription;

        /// <summary>
        /// Initializes a new instance of the <see cref="GradientBrush"/> class.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("PresentationProperty", "AVP1012", 
            Justification = "Collection properties shouldn't be set with SetCurrentValue.")]
        internal GradientBrush()
        {
            this.GradientStops = new GradientStops();
        }

        /// <inheritdoc/>
        public GradientSpreadMethod SpreadMethod
        {
            get { return GetValue(SpreadMethodProperty); }
            set { SetValue(SpreadMethodProperty, value); }
        }

        /// <inheritdoc cref="IGradientBrush.GradientStops"/>
        [Content]
        public GradientStops GradientStops
        {
            get { return GetValue(GradientStopsProperty); }
            set { SetValue(GradientStopsProperty, value); }
        }

        /// <inheritdoc/>
        IReadOnlyList<IGradientStop> IGradientBrush.GradientStops => GradientStops;


        protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
        {
            if (change.Property == GradientStopsProperty)
            {
                var (oldValue, newValue) = change.GetOldAndNewValue<GradientStops?>();

                if (oldValue != null)
                {
                    oldValue.CollectionChanged -= GradientStopsChanged;
                    _gradientStopsSubscription?.Dispose();
                }

                if (newValue != null)
                {
                    newValue.CollectionChanged += GradientStopsChanged;
                    _gradientStopsSubscription = newValue.TrackItemPropertyChanged(GradientStopChanged);
                }
            }
            base.OnPropertyChanged(change);
        }

        private void GradientStopsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RegisterForSerialization();
        }

        private void GradientStopChanged(Tuple<object?, PropertyChangedEventArgs> e)
        {
            RegisterForSerialization();
        }

        private protected override void SerializeChanges(Compositor c, BatchStreamWriter writer)
        {
            base.SerializeChanges(c, writer);
            writer.Write(SpreadMethod);
            writer.Write(GradientStops.Count);
            foreach (var stop in GradientStops) 
                // TODO: Technically it allocates, so it would be better to sync stops individually
                writer.WriteObject(new ImmutableGradientStop(stop.Offset, stop.Color));
        }

        public abstract IImmutableBrush ToImmutable();
    }
}
