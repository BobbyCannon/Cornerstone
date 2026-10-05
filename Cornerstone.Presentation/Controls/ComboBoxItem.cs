using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.Automation;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Controls.Items;

namespace Cornerstone.Presentation.Controls
{
    /// <summary>
    /// A selectable item in a <see cref="ComboBox"/>.
    /// </summary>
    public class ComboBoxItem : ListBoxItem
    {
        public ComboBoxItem()
        {
            this.GetObservable(ComboBoxItem.IsFocusedProperty)
                .Subscribe(focused =>
                {
                    if (focused)
                    {
                        (Parent as ComboBox)?.ItemFocused(this);
                    }
                });
        }

        static ComboBoxItem()
        {
            AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<ComboBoxItem>(AutomationControlType.ComboBoxItem);
        }
    }
}
