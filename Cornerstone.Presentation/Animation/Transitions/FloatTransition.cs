using System;
using Cornerstone.Presentation.Animation.Animators;

namespace Cornerstone.Presentation.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="PresentationProperty"/> with <see cref="float"/> types.
    /// </summary>  
    public class FloatTransition : Transition<float>
    {
        internal override IObservable<float> DoTransition(IObservable<double> progress, float oldValue, float newValue) => 
            AnimatorDrivenTransition<float, FloatAnimator>.Transition(Easing, progress, oldValue, newValue);
    }
}
