using System;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Media;

namespace Cornerstone.Presentation.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="Size"/> properties.
    /// </summary>
    internal class SizeAnimator : Animator<Size>
    {
        public override Size Interpolate(double progress, Size oldValue, Size newValue)
        {
            return ((newValue - oldValue) * progress) + oldValue;
        }
    }
}