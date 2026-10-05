using System;
using Android.OS;
using Cornerstone.Presentation.Android.Platform;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Platform;

namespace Cornerstone.Presentation.Android;

public class CornerstoneMainActivity : CornerstoneActivity
{
    private protected override void InitializeCornerstoneView(object? initialContent)
    {
        if (Application is IAndroidApplication application && application.Lifetime is { } lifetime)
        {
            initialContent ??= lifetime.MainViewFactory?.Invoke();

            _view = new CornerstoneView(this);

            Content = initialContent;
        }

        if (_view is null)
            throw new InvalidOperationException("Unknown error: CornerstoneView initialization has failed.");
    }

    protected override void OnResume()
    {
        base.OnResume();

        if (Cornerstone.Presentation.Application.Current?.TryGetFeature<IActivatableLifetime>()
            is AndroidActivatableLifetime activatableLifetime)
        {
            activatableLifetime.CurrentMainActivity = this;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (Cornerstone.Presentation.Application.Current?.TryGetFeature<IActivatableLifetime>()
            is AndroidActivatableLifetime activatableLifetime && activatableLifetime.CurrentMainActivity == this)
        {
            activatableLifetime.CurrentMainActivity = null;
        }
    }
}
