using System;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Animation
{
    /// <summary>
    /// Interface for Transition objects.
    /// </summary>
    [NotClientImplementable, PrivateApi]
    public interface ITransition
    {
        /// <summary>
        /// Applies the transition to the specified <see cref="Animatable"/>.
        /// </summary>
        internal IDisposable Apply(Animatable control, IClock clock, object? oldValue, object? newValue);

        /// <summary>
        /// Gets the property to be animated.
        /// </summary>
        PresentationProperty Property { get; set; }
    
    }
}
