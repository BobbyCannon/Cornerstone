using System;
using Cornerstone.Presentation.Animation.Animators;

namespace Cornerstone.Presentation.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="PresentationProperty"/> with <see cref="Size"/> type.
    /// </summary>  
    public class SizeTransition : Transition<Size>
    {
        internal override IObservable<Size> DoTransition(IObservable<double> progress, Size oldValue, Size newValue) => 
            AnimatorDrivenTransition<Size, SizeAnimator>.Transition(Easing, progress, oldValue, newValue);
    }
}
