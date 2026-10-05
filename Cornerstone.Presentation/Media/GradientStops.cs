using System.Collections.Generic;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Media.Immutable;

namespace Cornerstone.Presentation.Media
{
    /// <summary>
    /// A collection of <see cref="GradientStop"/>s.
    /// </summary>
    public class GradientStops : OldPresentationList<GradientStop>
    {
        public GradientStops()
        {
            ResetBehavior = ResetBehavior.Remove;
        }

        public IReadOnlyList<ImmutableGradientStop> ToImmutable()
        {
            var count = Count;
            var stops = new ImmutableGradientStop[count];

            for (var i = 0; i < count; i++)
            {
                var currentStop = this[i];

                stops[i] = new ImmutableGradientStop(currentStop.Offset, currentStop.Color);
            }

            return stops;
        }
    }
}
