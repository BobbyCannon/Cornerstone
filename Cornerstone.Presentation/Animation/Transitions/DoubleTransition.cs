using System;
using Cornerstone.Presentation.Animation.Animators;

namespace Cornerstone.Presentation.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="PresentationProperty"/> with <see cref="double"/> types.
    /// </summary>  
    public class DoubleTransition : Transition<double>
    {
        internal override IObservable<double> DoTransition(IObservable<double> progress, double oldValue, double newValue) => 
            AnimatorDrivenTransition<double, DoubleAnimator>.Transition(Easing, progress, oldValue, newValue);
    }
}
