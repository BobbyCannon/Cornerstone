// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Cornerstone Project under the MIT License.

using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Controls;

namespace Cornerstone.Presentation.Controls.ColorPickers
{
    /// <summary>
    /// Defines a specific component in the HSV color model.
    /// </summary>
    public enum HsvComponent
    {
        /// <summary>
        /// The Alpha component.
        /// </summary>
        /// <remarks>
        /// Also see: <see cref="HsvColor.A"/>
        /// </remarks>
        Alpha = 0,

        /// <summary>
        /// The Hue component.
        /// </summary>
        /// <remarks>
        /// Also see: <see cref="HsvColor.H"/>
        /// </remarks>
        Hue = 1,

        /// <summary>
        /// The Saturation component.
        /// </summary>
        /// <remarks>
        /// Also see: <see cref="HsvColor.S"/>
        /// </remarks>
        Saturation = 2,

        /// <summary>
        /// The Value component.
        /// </summary>
        /// <remarks>
        /// Also see: <see cref="HsvColor.V"/>
        /// </remarks>
        Value = 3
    };
}
