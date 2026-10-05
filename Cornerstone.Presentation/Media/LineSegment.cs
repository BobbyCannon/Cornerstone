using System;

namespace Cornerstone.Presentation.Media
{
    public sealed class LineSegment : PathSegment
    {
        /// <summary>
        /// Defines the <see cref="Point"/> property.
        /// </summary>
        public static readonly StyledProperty<Point> PointProperty
                        = PresentationProperty.Register<LineSegment, Point>(nameof(Point));

        /// <summary>
        /// Gets or sets the point.
        /// </summary>
        /// <value>
        /// The point.
        /// </value>
        public Point Point
        {
            get { return GetValue(PointProperty); }
            set { SetValue(PointProperty, value); }
        }

        internal override void ApplyTo(StreamGeometryContext ctx)
        {
            ctx.LineTo(Point, IsStroked);
        }

        public override string ToString()
            => FormattableString.Invariant($"L {Point}");
    }
}
