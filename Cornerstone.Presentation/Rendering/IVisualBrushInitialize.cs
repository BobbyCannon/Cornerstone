using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Rendering
{
    /// <summary>
    /// Internal interface for initializing controls that are to be used as the visual in a
    /// <see cref="VisualBrush"/>.
    /// </summary>
    [Unstable]
    internal interface IVisualBrushInitialize
    {
        /// <summary>
        /// Ensures that the control is ready to use as the visual in a visual brush.
        /// </summary>
        void EnsureInitialized();
    }
}
