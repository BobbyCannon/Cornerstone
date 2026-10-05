using Cornerstone.Presentation.Animation.Utils;

namespace Cornerstone.Presentation.Animation.Easings
{
    /// <summary>
    /// Eases out a <see cref="double"/> value 
    /// using a simulated bounce function.
    /// </summary>
    public class BounceEaseOut : Easing
    {
        /// <inheritdoc/>
        public override double Ease(double progress)
        {
            return BounceEaseUtils.Bounce(progress);
        }
    }
}
