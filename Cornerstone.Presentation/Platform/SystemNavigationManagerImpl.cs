using System;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Platform
{
    [Unstable]
    public interface ISystemNavigationManagerImpl
    {
        public event EventHandler<RoutedEventArgs>? BackRequested;
    }
}
