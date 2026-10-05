using Cornerstone.Presentation.SourceGenerator;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.Wayland.Server;

namespace Cornerstone.Presentation.Wayland.Screens;

/// <summary>
/// Worker → UI sink delivering a full <see cref="WaylandOutputsSnapshot"/>
/// whenever the compositor adds, removes, or finishes updating an output.
/// Implemented on the UI thread by <see cref="SnapshotScreensImpl"/>; the
/// worker holds the generated <c>WaylandOutputsSinkProxy</c> which marshals
/// every call onto the UI dispatcher.
/// </summary>
[GenerateCrossThreadProxy(
    typeof(DispatcherPriority),
    "default",
    GeneratedClassName = "WaylandOutputsSinkProxy")]
internal interface IWaylandOutputsSink
{
    void OnOutputsChanged(WaylandOutputsSnapshot snapshot);
}
