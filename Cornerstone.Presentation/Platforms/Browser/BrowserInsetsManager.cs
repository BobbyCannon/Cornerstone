using Cornerstone.Presentation.Browser.Interop;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Media;

namespace Cornerstone.Presentation.Browser
{
    internal class BrowserInsetsManager : InsetsManagerBase
    {
        public override bool? IsSystemBarVisible
        {
            get
            {
                return DomHelper.IsFullscreen(BrowserWindowingPlatform.GlobalThis);
            }
            set
            {
                _ = DomHelper.SetFullscreen(BrowserWindowingPlatform.GlobalThis, !value ?? false);
            }
        }

        public override bool DisplayEdgeToEdgePreference { get; set; }

        public override Thickness SafeAreaPadding
        {
            get
            {
                var padding = DomHelper.GetSafeAreaPadding(BrowserWindowingPlatform.GlobalThis);

                return new Thickness(padding[0], padding[1], padding[2], padding[3]);
            }
        }

        public override Color? SystemBarColor { get; set; }

        public void NotifySafeAreaPaddingChanged()
        {
            OnSafeAreaChanged(new SafeAreaChangedArgs(SafeAreaPadding));
        }
    }
}
