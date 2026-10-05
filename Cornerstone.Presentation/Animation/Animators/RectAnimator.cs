using System;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Media;

namespace Cornerstone.Presentation.Animation.Animators
{
    /// <summary>
    /// Animator that handles <see cref="Rect"/> properties.
    /// </summary>
    internal class RectAnimator : Animator<Rect>
    {
        public override Rect Interpolate(double progress, Rect oldValue, Rect newValue)
        {
            var deltaPos = newValue.Position - oldValue.Position;
            var deltaSize = newValue.Size - oldValue.Size;

            var newPos = (deltaPos * progress) + oldValue.Position;
            var newSize = (deltaSize * progress) + oldValue.Size;

            return new Rect(newPos, newSize);
        }
    }
}