using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;

namespace Cornerstone.Presentation.Automation.Peers
{
    public class NativeMenuBarAutomationPeer(NativeMenuBar owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.MenuBar;
        }
    }
}
