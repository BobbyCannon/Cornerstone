using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Platforms.MacOS.Interop;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation.Platforms.MacOS
{
    internal class MacOSNativeMenuCommands : INativeApplicationCommands
    {
        private readonly ICsnApplicationCommands _commands;

        public MacOSNativeMenuCommands(ICsnApplicationCommands commands)
        {
            _commands = commands;
        }

        public void ShowApp()
        {
            _commands.UnhideApp();
        }

        public void HideApp()
        {
            _commands.HideApp();
        }

        public void ShowAll()
        {
            _commands.ShowAll();
        }

        public void HideOthers()
        {
            _commands.HideOthers();
        }

        public static readonly AttachedProperty<bool> IsServicesSubmenuProperty =
            PresentationProperty.RegisterAttached<MacOSNativeMenuCommands, NativeMenu, bool>("IsServicesSubmenu", false);
    }
}
