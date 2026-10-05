using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Controls.Platform;

namespace Cornerstone.Presentation.Platforms.MacOS;

internal class MacOSActivatableLifetime : ActivatableLifetimeBase
{
    public override bool TryLeaveBackground()
    {
        var nativeApplicationCommands = PresentationLocator.Current.GetService<INativeApplicationCommands>();
        nativeApplicationCommands?.ShowApp();

        return true;
    }

    public override bool TryEnterBackground()
    {
        var nativeApplicationCommands = PresentationLocator.Current.GetService<INativeApplicationCommands>();
        nativeApplicationCommands?.HideApp();

        return true;
    }
}
