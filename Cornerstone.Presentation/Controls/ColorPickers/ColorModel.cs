using Cornerstone.Presentation.Controls;

namespace Cornerstone.Presentation.Controls.ColorPickers
{
    /// <summary>
    /// Defines the model used to represent colors.
    /// </summary>
    public enum ColorModel
    {
        /// <summary>
        /// Color is represented by hue, saturation, value and alpha components.
        /// </summary>
        Hsva,

        /// <summary>
        /// Color is represented by red, green, blue and alpha components.
        /// </summary>
        Rgba
    }
}
