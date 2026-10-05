using System;
using Cornerstone.Presentation.Animation.Animators;
using Cornerstone.Presentation.Media;

namespace Cornerstone.Presentation.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="PresentationProperty"/> with <see cref="BoxShadows"/> type.
    /// </summary>  
    public class BoxShadowsTransition : Transition<BoxShadows>
    {
        internal override IObservable<BoxShadows> DoTransition(IObservable<double> progress, BoxShadows oldValue,
            BoxShadows newValue) =>
            AnimatorDrivenTransition<BoxShadows, BoxShadowsAnimator>.Transition(Easing, progress, oldValue, newValue);
    }
}
