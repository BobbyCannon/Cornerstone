using System;
using System.Threading;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation.Controls.ApplicationLifetimes
{
    public static class DesktopApplicationExtensions
    {

        /// <summary>
        /// On desktop-style platforms runs the application's main loop until closable is closed
        /// </summary>
        /// <remarks>
        /// Consider using StartWithDesktopStyleLifetime instead, see Application-lifetimes for details
        /// </remarks>
        public static void Run(this Application app, ICloseable closable)
        {
            var cts = new CancellationTokenSource();
            closable.Closed += (s, e) => cts.Cancel();

            app.Run(cts.Token);
        }

        /// <summary>
        /// On desktop-style platforms runs the application's main loop until main window is closed
        /// </summary>
        /// <remarks>
        /// Consider using StartWithDesktopStyleLifetime instead, see Application-lifetimes for details
        /// </remarks>
        public static void Run(this Application app, Window mainWindow)
        {
            if (mainWindow == null)
            {
                throw new ArgumentNullException(nameof(mainWindow));
            }
            var cts = new CancellationTokenSource();
            mainWindow.Closed += (_, __) => cts.Cancel();
            if (!mainWindow.IsVisible)
            {
                mainWindow.Show();
            }
            app.Run(cts.Token);
        }
        
        /// <summary>
        /// On desktop-style platforms runs the application's main loop with custom CancellationToken
        /// without setting a lifetime.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="token">The token to track.</param>
        public static void Run(this Application app, CancellationToken token)
        {
            Dispatcher.UIThread.MainLoop(token);
        }

        public static void RunWithMainWindow<TWindow>(this Application app)
            where TWindow : Cornerstone.Presentation.Controls.Window, new()
        {
            var window = new TWindow();
            window.Show();
            app.Run(window);
        }
    }
}
