using System.Collections.Generic;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Data;

namespace Cornerstone.Presentation.Controls.Shapes
{
    public class Polygon : Shape
    {
        public static readonly StyledProperty<IList<Point>> PointsProperty =
            PresentationProperty.Register<Polygon, IList<Point>>("Points");

        public static readonly StyledProperty<FillRule> FillRuleProperty =
            PresentationProperty.Register<Polygon, FillRule>(nameof(FillRule));

        static Polygon()
        {
            AffectsGeometry<Polygon>(PointsProperty, FillRuleProperty);
        }

        public Polygon()
        {
            SetValue(PointsProperty, new Points(), BindingPriority.Template);
        }

        public IList<Point> Points
        {
            get => GetValue(PointsProperty);
            set => SetValue(PointsProperty, value);
        }

        /// <summary>
        /// Gets or sets how the interior of the polygon is determined when a <see cref="Shape.Fill"/> is applied.
        /// </summary>
        public FillRule FillRule
        {
            get => GetValue(FillRuleProperty);
            set => SetValue(FillRuleProperty, value);
        }

        protected override Geometry CreateDefiningGeometry()
        {
            return new PolylineGeometry(Points, isFilled: true, fillRule: FillRule);
        }
    }
}
