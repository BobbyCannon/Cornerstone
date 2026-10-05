using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Rendering.Composition.Transport;

namespace Cornerstone.Presentation.Rendering.Composition.Animations
{
    public class CompositionAnimationGroup : CompositionObject, ICompositionAnimationBase
    {
        internal List<CompositionAnimation> Animations { get; } = new List<CompositionAnimation>();
        void ICompositionAnimationBase.InternalOnly()
        {
            
        }

        public void Add(CompositionAnimation value) => Animations.Add(value);
        public void Remove(CompositionAnimation value) => Animations.Remove(value);
        public void RemoveAll() => Animations.Clear();

        public CompositionAnimationGroup(Compositor compositor) : base(compositor, null)
        {
        }
    }
}
