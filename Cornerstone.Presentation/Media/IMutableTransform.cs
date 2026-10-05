using System;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Media
{
    public interface IMutableTransform : ITransform
    {
        /// <summary>
        /// Raised when the transform changes.
        /// </summary>
        event EventHandler Changed;
    }
}
