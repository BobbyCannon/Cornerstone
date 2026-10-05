using System;
using Cornerstone.Presentation.Animation.Animators;

namespace Cornerstone.Presentation.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="PresentationProperty"/> with <see cref="CornerRadius"/> type.
    /// </summary>  
    public class CornerRadiusTransition : Transition<CornerRadius>
    {
        internal override IObservable<CornerRadius> DoTransition(IObservable<double> progress, CornerRadius oldValue,
            CornerRadius newValue) =>
            AnimatorDrivenTransition<CornerRadius, CornerRadiusAnimator>.Transition(Easing, progress, oldValue,
                newValue);
    }
}
