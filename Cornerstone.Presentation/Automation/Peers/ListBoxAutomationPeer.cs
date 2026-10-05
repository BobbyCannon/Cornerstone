using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Items;

namespace Cornerstone.Presentation.Automation.Peers;

public class ListBoxAutomationPeer : SelectingItemsControlAutomationPeer
{
    public ListBoxAutomationPeer(ListBox owner)
        : base(owner)
    {
    }
}
