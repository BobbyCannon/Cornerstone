using System;
using Cornerstone.Presentation.Animation.Animators;

namespace Cornerstone.Presentation.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="PresentationProperty"/> with <see cref="Vector"/> type.
    /// </summary>  
    public class VectorTransition : Transition<Vector>
    {
        internal override IObservable<Vector> DoTransition(IObservable<double> progress, Vector oldValue, Vector newValue) => 
            AnimatorDrivenTransition<Vector, VectorAnimator>.Transition(Easing, progress, oldValue, newValue);
    }
}
