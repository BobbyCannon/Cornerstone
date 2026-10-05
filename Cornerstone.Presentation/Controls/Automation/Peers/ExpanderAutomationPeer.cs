using Cornerstone.Presentation.Automation;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Automation.Provider;
using Cornerstone.Presentation.Controls.Elements;

namespace Cornerstone.Presentation.Controls.Automation.Peers
{
    public class ExpanderAutomationPeer : ControlAutomationPeer,
        IExpandCollapseProvider
    {
        public ExpanderAutomationPeer(Control owner)
            : base(owner)
        {
            owner.PropertyChanged += OwnerPropertyChanged;
        }

        public new Expander Owner => (Expander)base.Owner;

        public ExpandCollapseState ExpandCollapseState => ToState(Owner.IsExpanded);
        public bool ShowsMenu => false;
        public void Collapse() => Owner.IsExpanded = false;
        public void Expand() => Owner.IsExpanded = true;

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Expander;
        }

        private void OwnerPropertyChanged(object? sender, PresentationPropertyChangedEventArgs e)
        {
            if (e.Property == Expander.IsExpandedProperty)
            {
                RaisePropertyChangedEvent(
                    ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                    ToState((bool)e.OldValue!),
                    ToState((bool)e.NewValue!));
            }
        }

        private static ExpandCollapseState ToState(bool value)
        {
            return value ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
        }

        protected override bool IsContentElementCore() => true;
        protected override bool IsControlElementCore() => true;
    }
}
