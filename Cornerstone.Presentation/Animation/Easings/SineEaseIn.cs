using System;
using Cornerstone.Presentation.Animation.Utils;

namespace Cornerstone.Presentation.Animation.Easings
{
    /// <summary>
    /// Eases in a <see cref="double"/> value 
    /// using the quarter-wave of sine function.
    /// </summary>
    public class SineEaseIn : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            return Math.Sin((progress - 1) * EasingUtils.HALFPI) + 1;
        }
    }
}
