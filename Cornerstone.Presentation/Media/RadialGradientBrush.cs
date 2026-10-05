using System;
using Cornerstone.Presentation.Media.Immutable;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Rendering.Composition;
using Cornerstone.Presentation.Rendering.Composition.Server;
using Cornerstone.Presentation.Rendering.Composition.Transport;

namespace Cornerstone.Presentation.Media
{
    /// <summary>
    /// Paints an area with a radial gradient.
    /// </summary>
    public sealed class RadialGradientBrush : GradientBrush, IRadialGradientBrush
    {
        /// <summary>
        /// Defines the <see cref="Center"/> property.
        /// </summary>
        public static readonly StyledProperty<RelativePoint> CenterProperty =
            PresentationProperty.Register<RadialGradientBrush, RelativePoint>(
                nameof(Center),
                RelativePoint.Center);

        /// <summary>
        /// Defines the <see cref="GradientOrigin"/> property.
        /// </summary>
        public static readonly StyledProperty<RelativePoint> GradientOriginProperty =
            PresentationProperty.Register<RadialGradientBrush, RelativePoint>(
                nameof(GradientOrigin), 
                RelativePoint.Center);
        
        /// <summary>
        /// Defines the <see cref="RadiusX"/> property.
        /// </summary>
        public static readonly StyledProperty<RelativeScalar> RadiusXProperty =
            PresentationProperty.Register<RadialGradientBrush, RelativeScalar>(
                nameof(RadiusX), RelativeScalar.Middle);
        
        /// <summary>
        /// Defines the <see cref="RadiusX"/> property.
        /// </summary>
        public static readonly StyledProperty<RelativeScalar> RadiusYProperty =
            PresentationProperty.Register<RadialGradientBrush, RelativeScalar>(
                nameof(RadiusY), RelativeScalar.Middle);
        
        /// <summary>
        /// Gets or sets the start point for the gradient.
        /// </summary>
        public RelativePoint Center
        {
            get { return GetValue(CenterProperty); }
            set { SetValue(CenterProperty, value); }
        }

        /// <summary>
        /// Gets or sets the location of the two-dimensional focal point that defines the beginning
        /// of the gradient.
        /// </summary>
        public RelativePoint GradientOrigin
        {
            get { return GetValue(GradientOriginProperty); }
            set { SetValue(GradientOriginProperty, value); }
        }

        /// <summary>
        /// Gets or sets the horizontal radius of the outermost circle of the radial
        /// gradient.
        /// </summary>
        public RelativeScalar RadiusX
        {
            get { return GetValue(RadiusXProperty); }
            set { SetValue(RadiusXProperty, value); }
        }
        
        /// <summary>
        /// Gets or sets the vertical radius of the outermost circle of the radial
        /// gradient.
        /// </summary>
        public RelativeScalar RadiusY
        {
            get { return GetValue(RadiusYProperty); }
            set { SetValue(RadiusYProperty, value); }
        }

        /// <inheritdoc/>
        public override IImmutableBrush ToImmutable()
        {
            return new ImmutableRadialGradientBrush(this);
        }

        internal override Func<Compositor, ServerCompositionSimpleBrush> Factory =>
            static c => new ServerCompositionSimpleRadialGradientBrush(c.Server);

        private protected override void SerializeChanges(Compositor c, BatchStreamWriter writer)
        {
            base.SerializeChanges(c, writer);
            ServerCompositionSimpleRadialGradientBrush.SerializeAllChanges(writer, Center, GradientOrigin, RadiusX, RadiusY);
        }
    }
}
