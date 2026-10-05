using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation.Browser;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.ApplicationLifetimes;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation;

internal class BrowserSingleViewLifetime : ISingleViewApplicationLifetime, ISingleTopLevelApplicationLifetime
{
    public CornerstoneView? View;

    public Control? MainView
    {
        get
        {
            EnsureView();
            return View.Content;
        }
        set
        {
            EnsureView();
            View.Content = value;
        }
    }

    [MemberNotNull(nameof(View))]
    private void EnsureView()
    {
        if (View is null)
        {
            throw new InvalidOperationException(
                "Browser lifetime was not initialized. Make sure AppBuilder.StartBrowserAppAsync was called.");
        }
    }

    public TopLevel? TopLevel => View?.TopLevel;
}
