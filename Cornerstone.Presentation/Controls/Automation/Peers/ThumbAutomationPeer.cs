using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Controls.Primitives;

namespace Cornerstone.Presentation.Controls.Automation.Peers
{
    public class ThumbAutomationPeer : ControlAutomationPeer
    {
        public ThumbAutomationPeer(Thumb owner) : base(owner) { }
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Thumb;
        protected override bool IsContentElementCore() => false;
    }
}
