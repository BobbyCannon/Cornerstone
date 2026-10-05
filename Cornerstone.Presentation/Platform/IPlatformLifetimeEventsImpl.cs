using System;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Platform
{
    [Unstable]
    public interface IPlatformLifetimeEventsImpl
    {
        /// <summary>
        /// Raised by the platform when a shutdown is requested.
        /// </summary>
        /// <remarks>
        /// Raised on OSX via the Quit menu or right-clicking on the application icon and selecting Quit.
        /// </remarks>
        event EventHandler<ShutdownRequestedEventArgs>? ShutdownRequested;
    }
}
