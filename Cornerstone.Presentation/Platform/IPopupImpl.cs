using Cornerstone.Presentation.Controls.Primitives.PopupPositioning;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Platform
{
    /// <summary>
    /// Defines a platform-specific popup window implementation.
    /// </summary>
    [Unstable]
    public interface IPopupImpl : IWindowBaseImpl
    {
        IPopupPositioner? PopupPositioner { get; }

        void SetWindowManagerAddShadowHint(bool enabled);
        void TakeFocus();

        /// <summary>
        /// Sets whether the popup window takes part in pointer hit testing. When false, the
        /// native window is made input-transparent so that pointer input passes through to
        /// whatever is behind it.
        /// </summary>
        void SetHitTestVisible(bool isHitTestVisible);
    }
}
