namespace Cornerstone.Presentation.Controls.Chrome
{
    /// <summary>
    /// Describes how the <see cref="Window.Closing"/> event behaves in the presence of child windows.
    /// </summary>
    public enum WindowClosingBehavior
    {
        /// <summary>
        /// When the owner window is closed, the child windows' <see cref="Window.Closing"/> event
        /// will be raised, followed by the owner window's <see cref="Window.Closing"/> events. A child
        /// canceling the close will result in the owner Window's close being cancelled.
        /// </summary>
        OwnerAndChildWindows,

        /// <summary>
        /// When the owner window is closed, only the owner window's <see cref="Window.Closing"/> event
        /// will be raised. This behavior is the same as WPF's.
        /// </summary>
        OwnerWindowOnly,
    }
}
