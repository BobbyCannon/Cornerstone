using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Controls.Items;

namespace Cornerstone.Presentation.Controls
{
    /// <summary>
    /// Represents a tab in a <see cref="TabStrip"/>.
    /// </summary>
    public class TabStripItem : ListBoxItem
    {
        protected override void OnGotFocus(FocusChangedEventArgs e)
        {
            base.OnGotFocus(e);
            UpdateSelectionFromEvent(e);
        }
    }
}
