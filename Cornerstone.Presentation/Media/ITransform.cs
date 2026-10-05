using System.ComponentModel;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Media
{
    [TypeConverter(typeof(TransformConverter))]
    [NotClientImplementable]
    public interface ITransform
    {
        Matrix Value { get; }
    }
}
