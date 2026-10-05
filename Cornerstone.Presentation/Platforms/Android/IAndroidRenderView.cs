using System;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Android
{
    internal interface IAndroidRenderView : INativePlatformHandleSurface
    {
        event EventHandler SurfaceWindowCreated;
        event EventHandler SurfaceWindowDestroyed;
    }
}