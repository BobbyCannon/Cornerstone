using System;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Media;

namespace Cornerstone.Presentation.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="Thickness"/> properties.
    /// </summary>
    internal class ThicknessAnimator : Animator<Thickness>
    {
        public override Thickness Interpolate(double progress, Thickness oldValue, Thickness newValue)
        {
            return ((newValue - oldValue) * progress) + oldValue;
        }
    }
}