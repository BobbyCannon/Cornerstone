using Cornerstone.Presentation.Wayland.Server.Interop;
using Cornerstone.Presentation.Wayland.Server.Transient;

namespace Cornerstone.Presentation.Wayland.Server.Persistent;

interface IPersistentWaylandObject
{
    void OnConnected(WaylandConnection connection, WaylandGlobals globals);
    void OnDisconnected();
}