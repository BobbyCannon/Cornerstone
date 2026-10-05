using System;
using Cornerstone.Presentation.Media;

namespace Cornerstone.Presentation.Controls.Shapes
{
    public class Path : Shape
    {
        public static readonly StyledProperty<Geometry?> DataProperty =
            PresentationProperty.Register<Path, Geometry?>(nameof(Data));

        static Path()
        {
            AffectsGeometry<Path>(DataProperty);
        }

        public Geometry? Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        protected override Geometry? CreateDefiningGeometry() => Data;
    }
}
