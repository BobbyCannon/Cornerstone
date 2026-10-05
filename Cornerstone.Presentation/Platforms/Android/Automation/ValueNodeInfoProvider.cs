using Android.OS;
using AndroidX.Core.View;
using AndroidX.Core.View.Accessibility;
using AndroidX.CustomView.Widget;
using Cornerstone.Presentation.Automation;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Automation.Provider;

namespace Cornerstone.Presentation.Android.Automation
{
    internal class ValueNodeInfoProvider : NodeInfoProvider<IValueProvider>
    {
        public ValueNodeInfoProvider(ExploreByTouchHelper owner, AutomationPeer peer, int virtualViewId) : 
            base(owner, peer, virtualViewId)
        {
        }

        protected override void PeerPropertyChanged(object? sender, AutomationPropertyChangedEventArgs e)
        {
            base.PeerPropertyChanged(sender, e);
            if (e.Property == ValuePatternIdentifiers.ValueProperty)
            {
                InvalidateSelf(AccessibilityEventCompat.ContentChangeTypeText);
            }
        }

        public override bool PerformNodeAction(int action, Bundle? arguments)
        {
            IValueProvider provider = GetProvider();
            switch (action)
            {
                case AccessibilityNodeInfoCompat.ActionSetText:
                    string? text = arguments?.GetCharSequence(
                        AccessibilityNodeInfoCompat.ActionArgumentSetTextCharsequence
                        );
                    provider.SetValue(provider.Value + text);
                    return true;

                default:
                    return false;
            }
        }

        public override void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo)
        {
            nodeInfo.AddAction(AccessibilityNodeInfoCompat.ActionSetText);

            IValueProvider provider = GetProvider();
            nodeInfo.Text = provider.Value;
            nodeInfo.Editable = !provider.IsReadOnly;

            nodeInfo.SetTextSelection(
                provider.Value?.Length ?? 0, 
                provider.Value?.Length ?? 0
                );
            nodeInfo.LiveRegion = ViewCompat.AccessibilityLiveRegionPolite;
        }
    }
}
