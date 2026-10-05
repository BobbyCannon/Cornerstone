using System;
using System.Collections.Specialized;
using Cornerstone.Presentation.Collections;

namespace Cornerstone.Presentation.Diagnostics
{
    /// <summary>
    /// Provides a debug interface into <see cref="INotifyCollectionChanged"/> subscribers on
    /// <see cref="OldPresentationList{T}"/>
    /// </summary>
    internal interface INotifyCollectionChangedDebug
    {
        /// <summary>
        /// Gets the subscriber list for the <see cref="INotifyCollectionChanged.CollectionChanged"/>
        /// event.
        /// </summary>
        /// <returns>
        /// The subscribers or null if no subscribers.
        /// </returns>
        Delegate[]? GetCollectionChangedSubscribers();
    }
}
