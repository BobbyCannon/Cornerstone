using System;
using Cornerstone.Presentation.Animation.Animators;

namespace Cornerstone.Presentation.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="PresentationProperty"/> with <see cref="Thickness"/> type.
    /// </summary>  
    public class ThicknessTransition : Transition<Thickness>
    {
        internal override IObservable<Thickness> DoTransition(IObservable<double> progress, Thickness oldValue, Thickness newValue) => 
            AnimatorDrivenTransition<Thickness, ThicknessAnimator>.Transition(Easing, progress, oldValue, newValue);
    }
}
