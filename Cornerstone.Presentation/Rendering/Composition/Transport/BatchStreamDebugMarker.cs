using System;

namespace Cornerstone.Presentation.Rendering.Composition.Transport;

internal class BatchStreamDebugMarkers
{
    public static object ObjectEndMarker = new object();
    public static Guid ObjectEndMagic = Guid.NewGuid();
}
