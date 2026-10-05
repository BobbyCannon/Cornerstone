using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Immutable;
using Cornerstone.Presentation.Rendering.Composition.Drawing;
using Cornerstone.Presentation.Rendering.Composition.Transport;

// ReSharper disable CheckNamespace

namespace Cornerstone.Presentation.Rendering.Composition.Server
{
    internal partial class ServerCompositionSimpleBrush : IBrush
    {
        ITransform? IBrush.Transform => Transform;
        ITransform? IBrush.RelativeTransform => RelativeTransform;
    }

    internal class ServerCompositionSimpleGradientBrush : ServerCompositionSimpleBrush, IGradientBrush
    {
        
        internal ServerCompositionSimpleGradientBrush(ServerCompositor compositor) : base(compositor)
        {
            
        }

        private readonly List<IGradientStop> _gradientStops = new();
        public IReadOnlyList<IGradientStop> GradientStops => _gradientStops;
        public GradientSpreadMethod SpreadMethod { get; private set; }

        protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
        {
            base.DeserializeChangesCore(reader, committedAt);
            SpreadMethod = reader.Read<GradientSpreadMethod>();
            _gradientStops.Clear();
            var count = reader.Read<int>();
            for (var c = 0; c < count; c++)
                _gradientStops.Add(reader.ReadObject<ImmutableGradientStop>());
        }
    }

    partial class ServerCompositionSimpleConicGradientBrush : IConicGradientBrush
    {
        
    }
    
    partial class ServerCompositionSimpleLinearGradientBrush : ILinearGradientBrush
    {
        
    }
    
    partial class ServerCompositionSimpleRadialGradientBrush : IRadialGradientBrush
    {

    }
    
    partial class ServerCompositionSimpleSolidColorBrush : ISolidColorBrush
    {
        
    }
    
    
}
