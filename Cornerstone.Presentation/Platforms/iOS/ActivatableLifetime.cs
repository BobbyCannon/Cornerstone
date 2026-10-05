using System;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;

namespace Cornerstone.Presentation.iOS;

internal class ActivatableLifetime : ActivatableLifetimeBase
{
    public ActivatableLifetime(ICornerstoneAppDelegate cornerstoneAppDelegate)
    {
        cornerstoneAppDelegate.Activated += (_, args) => OnActivated(args);
        cornerstoneAppDelegate.Deactivated += (_, args) => OnDeactivated(args);
    }
}
