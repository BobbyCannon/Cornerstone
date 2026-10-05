using System;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls;

namespace Cornerstone.Presentation.Dialogs
{
    public class AboutCornerstoneDialog : Window
    {
        private static readonly Version s_version = typeof(AboutCornerstoneDialog).Assembly.GetName().Version!;

        public static string Version { get; } = $@"v{s_version.ToString(2)}";

        public static bool IsDevelopmentBuild { get; } = s_version.Revision == 999;

        public static string Copyright { get; } = $"© {DateTime.Now.Year} The Cornerstone Project";

        public AboutCornerstoneDialog()
        {
            CornerstoneXamlLoader.Load(this);
            DataContext = this;
        }

        private async void Button_OnClick(object sender, RoutedEventArgs e)
        {
            var url = new Uri("https://github.com/BobbyCannon/Cornerstone");
            await Launcher.LaunchUriAsync(url);
        }
    }
}
