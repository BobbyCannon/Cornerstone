using System;
using Cornerstone.Presentation.Threading;

namespace Cornerstone.Presentation.Wayland.Server;

/// <summary>
/// Marshallers used by generated cross-thread proxies.
/// </summary>
/// <remarks>
/// The UI-thread marshaller is stateless and exposed as a singleton. The
/// worker-thread marshaller is per-worker and exposed via
/// <see cref="WaylandWorkerClient.Marshaller"/> — there is no global worker.
/// </remarks>
internal static class WaylandMarshallers
{
    public static Action<Action, DispatcherPriority> UIThread { get; } = Dispatcher.UIThread.Post;
}
