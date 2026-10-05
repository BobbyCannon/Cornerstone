using System;
using Cornerstone.Presentation.Animation.Animators;

namespace Cornerstone.Presentation.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="PresentationProperty"/> with <see cref="int"/> types.
    /// </summary>  
    public class IntegerTransition : Transition<int>
    {
        internal override IObservable<int> DoTransition(IObservable<double> progress, int oldValue, int newValue) => 
            AnimatorDrivenTransition<int, Int32Animator>.Transition(Easing, progress, oldValue, newValue);
    }
}
