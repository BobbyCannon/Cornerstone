using Cornerstone.Presentation.SourceGenerator;
using Cornerstone.Presentation.Wayland.Server;

namespace Cornerstone.Presentation.Wayland.Server.Persistent;

/// <summary>
/// UI→worker proxy for a native-host <c>wl_subsurface</c> under an xdg_toplevel.
/// </summary>
[GenerateCrossThreadProxy(
    typeof(WaylandDispatchPriority),
    "Cornerstone.Presentation.Wayland.Server.WaylandDispatchPriority.Normal",
    GeneratedClassName = "WNativeControlSubsurfaceProxy")]
internal interface IWNativeControlSubsurface
{
    void Disconnect();
    /// <summary>
    /// x/y/width/height are parent surface-local (logical). scale is used for the SHM buffer.
    /// </summary>
    void ShowInBounds(int x, int y, int width, int height, double scale);
    void Hide();
    void SetBehindComposition(bool behind);
    void SetUsesExternalBuffers(bool value);
    void AttachBgra(byte[] pixels, int width, int height);
}
