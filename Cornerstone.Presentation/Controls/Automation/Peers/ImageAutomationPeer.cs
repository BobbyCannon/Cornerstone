using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Controls.Automation.Peers
{
    public class ImageAutomationPeer : ControlAutomationPeer
    {
        public ImageAutomationPeer(Control owner) : base(owner)
        {
        }

        override protected string GetClassNameCore()
        {
            return "Image";
        }

        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Image;
        }
    }
}
