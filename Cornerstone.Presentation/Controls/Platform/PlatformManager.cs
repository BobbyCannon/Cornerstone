using System;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Controls.Platform
{
    [Unstable]
    public static partial class PlatformManager
    {
        static bool s_designerMode;

        public static IDisposable DesignerMode()
        {
            s_designerMode = true;
            return Disposable.Create(() => s_designerMode = false);
        }

        public static void SetDesignerScalingFactor(double factor)
        {
        }

        public static ITrayIconImpl? CreateTrayIcon() =>
            s_designerMode ? null : PresentationLocator.Current.GetService<IWindowingPlatform>()?.CreateTrayIcon();


        public static IWindowImpl CreateWindow()
        {
            var platform = PresentationLocator.Current.GetRequiredService<IWindowingPlatform>();

            return s_designerMode ? platform.CreateEmbeddableWindow() : platform.CreateWindow();
        }

        public static IWindowImpl CreateEmbeddableWindow()
        {
            var platform = PresentationLocator.Current.GetRequiredService<IWindowingPlatform>();
            return platform.CreateEmbeddableWindow();
        }
        
        public static ITopLevelImpl CreateEmbeddableTopLevel()
        {
            var platform = PresentationLocator.Current.GetRequiredService<IWindowingPlatform>();
            return platform.CreateEmbeddableTopLevel();
        }
    }
}
