using System;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Browser;

internal class BrowserSystemNavigationManagerImpl : ISystemNavigationManagerImpl
{
    public event EventHandler<RoutedEventArgs>? BackRequested;

    public bool OnBackRequested()
    {
        var routedEventArgs = new RoutedEventArgs();

        BackRequested?.Invoke(this, routedEventArgs);

        return routedEventArgs.Handled;
    }
}
