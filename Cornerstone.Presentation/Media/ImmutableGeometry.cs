using System;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Media;

internal class ImmutableGeometry : Geometry
{
    public ImmutableGeometry(IGeometryImpl? platformImpl)
        : base(platformImpl)
    {
    }

    public override Geometry Clone() => new ImmutableGeometry(PlatformImpl);

    private protected override IGeometryImpl? CreateDefiningGeometry()
    {
        return PlatformImpl;
    }
}
