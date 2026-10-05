using System;
using Cornerstone.Presentation.Animation.Animators;

namespace Cornerstone.Presentation.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="PresentationProperty"/> with <see cref="RelativePoint"/> type.
    /// </summary>  
    public class RelativePointTransition : Transition<RelativePoint>
    {
        internal override IObservable<RelativePoint> DoTransition(IObservable<double> progress, RelativePoint oldValue, RelativePoint newValue) =>
            AnimatorDrivenTransition<RelativePoint, RelativePointAnimator>.Transition(Easing, progress, oldValue, newValue);
    }
}
