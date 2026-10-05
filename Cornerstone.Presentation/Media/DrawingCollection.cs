using System.Collections.Generic;
using Cornerstone.Presentation.Collections;

namespace Cornerstone.Presentation.Media
{
    public sealed class DrawingCollection : OldPresentationList<Drawing>
    {
        public DrawingCollection()
        {
            ResetBehavior = ResetBehavior.Remove;
        }

        public DrawingCollection(IEnumerable<Drawing> items) : base(items)
        {
            ResetBehavior = ResetBehavior.Remove;
        }
    }
}
