using System;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Utilities;

namespace Cornerstone.Presentation.Rendering.Composition.Server
{
    internal abstract partial class ServerCompositionSurface : ServerObject
    {
        protected ServerCompositionSurface(ServerCompositor compositor) : base(compositor)
        {
        }
        
        public abstract IRef<IBitmapImpl>? Bitmap { get; }
        public Action? Changed { get; set; }
    }
}
