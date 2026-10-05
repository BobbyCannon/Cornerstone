using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Animation
{
    /// <summary>
    /// Interface for Animator objects
    /// </summary>
    internal interface IAnimator : IList<AnimatorKeyFrame>
    {
        /// <summary>
        /// The target property.
        /// </summary>
        PresentationProperty? Property { get; set; }

        /// <summary>
        /// Applies the current KeyFrame group to the specified control.
        /// </summary>
        IDisposable? Apply(Animation animation, Animatable control, IClock? clock,
            IObservable<bool> match, Action? onComplete, bool shouldPauseOnInvisible);
    }
}
