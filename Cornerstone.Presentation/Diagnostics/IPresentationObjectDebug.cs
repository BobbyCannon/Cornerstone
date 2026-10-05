using System;

namespace Cornerstone.Presentation.Diagnostics
{
    /// <summary>
    /// Provides a debug interface into <see cref="PresentationObject"/>.
    /// </summary>
    internal interface IPresentationObjectDebug
    {
        /// <summary>
        /// Gets the subscriber list for the <see cref="PresentationObject.PropertyChanged"/>
        /// event.
        /// </summary>
        /// <returns>
        /// The subscribers or null if no subscribers.
        /// </returns>
        Delegate[]? GetPropertyChangedSubscribers();
    }
}
