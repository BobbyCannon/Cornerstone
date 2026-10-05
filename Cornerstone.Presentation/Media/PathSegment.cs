namespace Cornerstone.Presentation.Media
{
    public abstract class PathSegment : PresentationObject
    {
        internal abstract void ApplyTo(StreamGeometryContext ctx);

        public static readonly StyledProperty<bool> IsStrokedProperty =
            PresentationProperty.Register<PathSegment, bool>(nameof(IsStroked), true);

        public bool IsStroked
        {
            get => GetValue(IsStrokedProperty);
            set => SetValue(IsStrokedProperty, value);
        }
    }
}
