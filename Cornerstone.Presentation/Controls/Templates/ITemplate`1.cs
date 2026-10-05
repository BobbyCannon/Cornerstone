using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Controls.Templates
{
    /// <summary>
    /// Creates a control.
    /// </summary>
    /// <typeparam name="TControl">The type of control.</typeparam>
    public interface ITemplate<TControl> : ITemplate where TControl : Control?
    {
        /// <summary>
        /// Creates the control.
        /// </summary>
        /// <returns>
        /// The created control.
        /// </returns>
        new TControl Build();
    }
}
