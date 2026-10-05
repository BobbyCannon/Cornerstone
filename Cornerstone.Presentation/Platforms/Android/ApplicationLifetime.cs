using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Android;

internal class ApplicationLifetime : IActivityApplicationLifetime, ISingleViewApplicationLifetime
{
    private Control? _mainView;

    public Func<Control>? MainViewFactory { get; set; }

    public Control? MainView
    {
        get => _mainView; set
        {
            _mainView = value;

            Logger.TryGet(LogEventLevel.Warning, LogArea.AndroidPlatform)?.Log(this, "ISingleViewApplicationLifetime.MainView is not fully supported on Android." +
                " Consider setting IActivityApplicationLifetime.MainViewFactory.");
            if (_mainView != null)
                MainViewFactory = () => _mainView;
            else
                MainViewFactory = null;
        }
    }
}
