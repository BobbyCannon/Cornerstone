using System;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Media;

namespace Cornerstone.Presentation.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="Point"/> properties.
    /// </summary>
    internal class PointAnimator : Animator<Point>
    {
        public override Point Interpolate(double progress, Point oldValue, Point newValue)
        { 
            return ((newValue - oldValue) * progress) + oldValue;
        }
    }
}