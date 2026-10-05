using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Controls.Resources;

namespace Cornerstone.Presentation.Styling
{
    /// <summary>
    /// Defines the interface for styles.
    /// </summary>
    [NotClientImplementable]
    public interface IStyle : IResourceNode
    {
        /// <summary>
        /// Gets a collection of child styles.
        /// </summary>
        IReadOnlyList<IStyle> Children { get; }
    }
}
