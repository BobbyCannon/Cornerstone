using System;
using Cornerstone.Presentation.Animation.Animators;
using Cornerstone.Presentation.Media;

namespace Cornerstone.Presentation.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="PresentationProperty"/> with <see cref="Color"/> type.
    /// </summary>
    public class ColorTransition : Transition<Color>
    {
        internal override IObservable<Color> DoTransition(IObservable<double> progress, Color oldValue, Color newValue)
            => AnimatorDrivenTransition<Color, ColorAnimator>.Transition(Easing, progress, oldValue, newValue);
    }
}
