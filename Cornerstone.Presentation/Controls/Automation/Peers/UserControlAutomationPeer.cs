using Cornerstone.Presentation.Automation;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Controls.Automation.Peers;

public class UserControlAutomationPeer : ControlAutomationPeer
{
    public UserControlAutomationPeer(UserControl owner)
        : base(owner)
    {
    }
    
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
}
