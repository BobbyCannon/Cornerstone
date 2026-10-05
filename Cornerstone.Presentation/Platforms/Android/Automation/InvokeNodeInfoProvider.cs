using Android.OS;
using AndroidX.Core.View.Accessibility;
using AndroidX.CustomView.Widget;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Automation.Provider;

namespace Cornerstone.Presentation.Android.Automation
{
    internal class InvokeNodeInfoProvider : NodeInfoProvider<IInvokeProvider>
    {
        public InvokeNodeInfoProvider(ExploreByTouchHelper owner, AutomationPeer peer, int virtualViewId) : 
            base(owner, peer, virtualViewId)
        {
        }

        public override bool PerformNodeAction(int action, Bundle? arguments)
        {
            IInvokeProvider provider = GetProvider();
            switch (action)
            {
                case AccessibilityNodeInfoCompat.ActionClick:
                    provider.Invoke();
                    return true;
                default:
                    return false;
            }
        }

        public override void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo)
        {
            nodeInfo.AddAction(AccessibilityNodeInfoCompat.ActionClick);
            nodeInfo.Clickable = true;
        }
    }
}
