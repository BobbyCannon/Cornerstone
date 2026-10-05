using System;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Controls.Diagnostics
{
    /// <summary>
    /// Diagnostics interface to retrieve an associated <see cref="IPopupHost"/>.
    /// </summary>
    [NotClientImplementable]
    internal interface IPopupHostProvider
    {
        /// <summary>
        /// The popup host.
        /// </summary>
        IPopupHost? PopupHost { get; }

        /// <summary>
        /// Raised when the popup host changes.
        /// </summary>
        event Action<IPopupHost?>? PopupHostChanged;
    }
}
