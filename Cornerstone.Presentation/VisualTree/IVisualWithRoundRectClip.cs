using System;
using System.ComponentModel;

namespace Cornerstone.Presentation.VisualTree
{
    internal interface IVisualWithRoundRectClip
    {
        /// <summary>
        /// Gets a value indicating the corner radius of control's clip bounds
        /// </summary>
        CornerRadius ClipToBoundsRadius { get; }
    }
}
