using Cornerstone.Presentation.Automation;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Controls.Primitives;

namespace Cornerstone.Presentation.Controls
{
    /// <summary>
    /// A check box control.
    /// </summary>
    public class CheckBox : ToggleButton
    {
        static CheckBox()
        {
            AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<CheckBox>(AutomationControlType.CheckBox);
        }
    }
}
