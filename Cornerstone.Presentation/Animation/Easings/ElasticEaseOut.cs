using System;
using Cornerstone.Presentation.Animation.Utils;

namespace Cornerstone.Presentation.Animation.Easings
{
    /// <summary>
    /// Eases out a <see cref="double"/> value 
    /// using a damped sine function.
    /// </summary>
    public class ElasticEaseOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            double p = progress;
            return Math.Sin(-13d * EasingUtils.HALFPI * (p + 1)) * Math.Pow(2d, -10d * p) + 1d;
        }
    }
}
