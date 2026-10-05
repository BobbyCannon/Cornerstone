using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation.Controls.NativeHosts
{
    /// <summary>
    /// Allows native menu support on platforms where a <see cref="NativeMenu"/> can be attached to the dock.
    /// </summary>
    public static class NativeDock
    {
        /// <summary>
        /// Defines the Menu attached property.
        /// </summary>
        public static readonly AttachedProperty<NativeMenu?> MenuProperty =
            PresentationProperty.RegisterAttached<PresentationObject, NativeMenu?>("Menu", typeof(NativeDock));

        /// <summary>
        /// Sets the value of the attached <see cref="MenuProperty"/>.
        /// </summary>
        /// <param name="o">The control to set the menu for.</param>
        /// <param name="menu">The menu to set.</param>
        public static void SetMenu(PresentationObject o, NativeMenu? menu) => o.SetValue(MenuProperty, menu);

        /// <summary>
        /// Gets the value of the attached <see cref="MenuProperty"/>.
        /// </summary>
        /// <param name="o">The control to get the menu for.</param>
        /// <returns>The menu of the control.</returns>
        public static NativeMenu? GetMenu(PresentationObject o) => o.GetValue(MenuProperty);
    }
}
