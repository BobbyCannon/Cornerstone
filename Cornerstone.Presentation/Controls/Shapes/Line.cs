using Cornerstone.Presentation.Media;

namespace Cornerstone.Presentation.Controls.Shapes
{
    public class Line : Shape
    {
        public static readonly StyledProperty<Point> StartPointProperty =
            PresentationProperty.Register<Line, Point>(nameof(StartPoint));

        public static readonly StyledProperty<Point> EndPointProperty =
            PresentationProperty.Register<Line, Point>(nameof(EndPoint));

        static Line()
        {
            StrokeThicknessProperty.OverrideDefaultValue<Line>(1);
            AffectsGeometry<Line>(StartPointProperty, EndPointProperty);
        }

        public Point StartPoint
        {
            get => GetValue(StartPointProperty);
            set => SetValue(StartPointProperty, value);
        }

        public Point EndPoint
        {
            get => GetValue(EndPointProperty);
            set => SetValue(EndPointProperty, value);
        }

        protected override Geometry CreateDefiningGeometry()
        {
            return new LineGeometry(StartPoint, EndPoint);
        }
    }
}
