using Cornerstone.Presentation.Automation;
using Cornerstone.Presentation.Automation.Provider;
using UIA = Cornerstone.Presentation.Platforms.Windows.Automation.Interop;

namespace Cornerstone.Presentation.Platforms.Windows.Automation
{
    internal partial class AutomationNode : UIA.IExpandCollapseProvider
    {
        int UIA.IExpandCollapseProvider.GetExpandCollapseState()
        {
            return ToUiaExpandCollapseState(InvokeSync<IExpandCollapseProvider, ExpandCollapseState>(x => x.ExpandCollapseState));
        }

        // Cornerstone declares PartiallyExpanded and LeafNode in the opposite order
        // from the UIA wire values.
        internal static int ToUiaExpandCollapseState(ExpandCollapseState state) => state switch
        {
            ExpandCollapseState.Collapsed => 0,
            ExpandCollapseState.Expanded => 1,
            ExpandCollapseState.PartiallyExpanded => 2,
            ExpandCollapseState.LeafNode => 3,
            _ => 0,
        };

        void UIA.IExpandCollapseProvider.Expand() => InvokeSync<IExpandCollapseProvider>(x => x.Expand());
        void UIA.IExpandCollapseProvider.Collapse() => InvokeSync<IExpandCollapseProvider>(x => x.Collapse());
    }
}
