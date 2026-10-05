using System;
using Cornerstone.Presentation.Animation.Animators;

namespace Cornerstone.Presentation.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="PresentationProperty"/> with <see cref="bool"/> types.
    /// </summary>  
    public class BoolTransition : Transition<bool>
    {
        internal override IObservable<bool> DoTransition(IObservable<double> progress, bool oldValue, bool newValue) => 
            AnimatorDrivenTransition<bool, BoolAnimator>.Transition(Easing, progress, oldValue, newValue);
    }
}
