using System.Collections.Generic;
using Cornerstone.Presentation.Collections;

namespace Cornerstone.Presentation;

/// <summary>
/// Represents a collection of <see cref="Point"/> values that can be individually accessed by index.
/// </summary>
public sealed class Points : OldPresentationList<Point>
{
    public Points()
    {
        
    }

    public Points(IEnumerable<Point> points) : base(points)
    {
        
    }
}
