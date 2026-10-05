using Android.OS;
using AndroidX.Core.View.Accessibility;
using AndroidX.CustomView.Widget;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Automation.Provider;

namespace Cornerstone.Presentation.Android.Automation
{
    internal class RangeValueNodeInfoProvider : NodeInfoProvider<IRangeValueProvider>
    {
        public RangeValueNodeInfoProvider(ExploreByTouchHelper owner, AutomationPeer peer, int virtualViewId) : 
            base(owner, peer, virtualViewId)
        {
        }

        public override bool PerformNodeAction(int action, Bundle? arguments)
        {
            return false;
        }

        public override void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo)
        {
            IRangeValueProvider provider = GetProvider();
            nodeInfo.RangeInfo = new AccessibilityNodeInfoCompat.RangeInfoCompat(
                AccessibilityNodeInfoCompat.RangeInfoCompat.RangeTypeFloat, 
                (float)provider.Minimum, (float)provider.Maximum, 
                (float)provider.Value
                );
        }
    }
}
