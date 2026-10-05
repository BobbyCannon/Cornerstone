using System.Diagnostics;

// ReSharper disable ExplicitCallerInfoArgument

namespace Cornerstone.Presentation.Diagnostics;

internal static partial class Diagnostic
{
    private static ActivitySource? s_activitySource;

    public static void InitActivitySource()
    {
        s_activitySource = new("Cornerstone.Presentation.Diagnostic.Source");
    }

    private static Activity? StartActivity(string name) => s_activitySource?.StartActivity(name);

    public static Activity? AttachingStyle() => StartActivity("Cornerstone.Presentation.AttachingStyle");
    public static Activity? FindingResource() => StartActivity("Cornerstone.Presentation.FindingResource");
    public static Activity? EvaluatingStyle() => StartActivity("Cornerstone.Presentation.EvaluatingStyle");
    public static Activity? MeasuringLayoutable() => StartActivity("Cornerstone.Presentation.MeasuringLayoutable");
    public static Activity? ArrangingLayoutable() => StartActivity("Cornerstone.Presentation.ArrangingLayoutable");
    public static Activity? PerformingHitTest() => StartActivity("Cornerstone.Presentation.PerformingHitTest");
    public static Activity? RaisingRoutedEvent() => StartActivity("Cornerstone.Presentation.RaisingRoutedEvent");
}
