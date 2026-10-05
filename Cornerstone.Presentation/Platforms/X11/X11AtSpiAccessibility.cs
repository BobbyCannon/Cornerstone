using System.Threading.Tasks;

namespace Cornerstone.Presentation.X11
{
    /// <summary>
    /// AT-SPI is not copied yet. Track/untrack stay no-ops so X11 windowing can compile (pause path).
    /// </summary>
    internal sealed class X11AtSpiAccessibility
    {
        internal X11AtSpiAccessibility(X11Platform platform)
        {
        }

        internal object Server => null;

        internal void Initialize()
        {
        }

        internal void TrackWindow(X11Window window)
        {
        }

        internal void UntrackWindow(X11Window window)
        {
        }
    }
}
