using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Platform;
using static Cornerstone.Presentation.Controls.Platform.Win32Properties;

namespace Cornerstone.Presentation.Controls.Platform
{
    [PrivateApi]
    public interface IWin32OptionsTopLevelImpl : ITopLevelImpl
    {
        /// <summary>
        /// Gets or sets a callback to set the window styles. 
        /// </summary>
        public CustomWindowStylesCallback? WindowStylesCallback { get; set; }

        /// <summary>
        /// Gets or sets a custom callback for the window's WndProc
        /// </summary>
        public CustomWndProcHookCallback? WndProcHookCallback { get; set; }

        /// <summary>
        /// Sets a window corner preference for the window.
        /// </summary>
        /// <param name="preference">The value to set.</param>
        public void SetWindowCornerPreference(WindowCornerPreference preference);
    }
}
