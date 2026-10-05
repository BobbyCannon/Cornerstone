using System;

namespace Cornerstone.Presentation.Controls.Chrome
{
    /// <summary>
    /// Determines how a <see cref="Window"/> will size itself to fit its content.
    /// </summary>
    [Flags]
    public enum SizeToContent
    {
        /// <summary>
        /// The window will not automatically size itself to fit its content.
        /// </summary>
        Manual = 0,

        /// <summary>
        /// The window will size itself horizontally to fit its content.
        /// </summary>
        Width = 1,

        /// <summary>
        /// The window will size itself vertically to fit its content.
        /// </summary>
        Height = 2,

        /// <summary>
        /// The window will size itself horizontally and vertically to fit its content.
        /// </summary>
        WidthAndHeight = 3,
    }
}
