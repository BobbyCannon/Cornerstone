using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Rendering;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation.Controls.Items
{
    /// <summary>
    /// Represents a <see cref="Menu"/> or <see cref="ContextMenu"/>.
    /// </summary>
    internal interface IMenu : IMenuElement, IInputElement
    {
        /// <summary>
        /// Gets the menu interaction handler.
        /// </summary>
        IMenuInteractionHandler InteractionHandler { get; }

        /// <summary>
        /// Gets a value indicating whether the menu is open.
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// Gets the root of the visual tree, if the control is attached to a visual tree.
        /// </summary>
        TopLevel? TopLevel { get; }
    }
}
