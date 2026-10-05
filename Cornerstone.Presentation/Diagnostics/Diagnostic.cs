using System;

namespace Cornerstone.Presentation.Diagnostics;

internal static partial class Diagnostic
{
    public static bool IsEnabled { get; }

    private static bool InitializeIsEnabled() => AppContext.TryGetSwitch("Cornerstone.Presentation.Diagnostics.Diagnostic.IsEnabled", out var isEnabled) && isEnabled;

    static Diagnostic()
    {
        IsEnabled = InitializeIsEnabled();
        if (!IsEnabled)
        {
            return;
        }

        InitActivitySource();
        InitMetrics();
    }
}
