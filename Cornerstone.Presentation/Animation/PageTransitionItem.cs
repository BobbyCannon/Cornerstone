using Cornerstone.Presentation.VisualTree;

namespace Cornerstone.Presentation.Animation
{
    /// <summary>
    /// Describes a single visible page within a carousel viewport.
    /// </summary>
    public readonly record struct PageTransitionItem(
        int Index,
        Visual Visual,
        double ViewportCenterOffset);
}
