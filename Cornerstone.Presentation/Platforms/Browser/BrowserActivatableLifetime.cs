using Cornerstone.Presentation.Browser.Interop;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;

namespace Cornerstone.Presentation.Browser;

internal class BrowserActivatableLifetime : ActivatableLifetimeBase
{
    public void OnVisibilityStateChanged(string visibilityState)
    {
        var visible = visibilityState == "visible";
        if (visible)
        {
            OnActivated(ActivationKind.Background);
        }
        else
        {
            OnDeactivated(ActivationKind.Background);
        }
    }
}
