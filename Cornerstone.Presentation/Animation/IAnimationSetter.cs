using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Animation
{
    [NotClientImplementable, PrivateApi]
    public interface IAnimationSetter
    {
        PresentationProperty? Property { get; set; }
        object? Value { get; set; }
    }
}
