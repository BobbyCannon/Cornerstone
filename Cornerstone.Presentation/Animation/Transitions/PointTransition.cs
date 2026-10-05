using System;
using Cornerstone.Presentation.Animation.Animators;

namespace Cornerstone.Presentation.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="PresentationProperty"/> with <see cref="Point"/> type.
    /// </summary>  
    public class PointTransition : Transition<Point>
    {
        internal override IObservable<Point> DoTransition(IObservable<double> progress, Point oldValue, Point newValue) => 
            AnimatorDrivenTransition<Point, PointAnimator>.Transition(Easing, progress, oldValue, newValue);
    }
}
