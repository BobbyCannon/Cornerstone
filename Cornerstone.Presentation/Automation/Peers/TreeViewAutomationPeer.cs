using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Items;

namespace Cornerstone.Presentation.Automation.Peers;

public class TreeViewAutomationPeer : ItemsControlAutomationPeer
{
    public TreeViewAutomationPeer(TreeView owner)
        : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.Tree;
    }
}
