using Cornerstone.Presentation.Automation.Provider;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Overlays;

namespace Cornerstone.Presentation.Automation.Peers
{
    public class ToolTipAutomationPeer(ToolTip owner) : ControlAutomationPeer(owner)
    {
        public new ToolTip Owner => (ToolTip)base.Owner;

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolTip;

        protected override string GetClassNameCore() => "ToolTip";
    }
}
