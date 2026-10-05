using System;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Data.Core
{
    [NotClientImplementable]
    public interface IPropertyInfo
    {
        string Name { get; }
        object? Get(object target);
        void Set(object target, object? value);
        bool CanSet { get; }
        bool CanGet { get; }
        Type PropertyType { get; }
    }
}
