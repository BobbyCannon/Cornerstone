using Cornerstone.Presentation.Automation.Provider;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Input;

namespace Cornerstone.Presentation.Automation.Peers
{
    public class TextBoxAutomationPeer : ControlAutomationPeer, IValueProvider
    {
        public TextBoxAutomationPeer(TextBox owner)
            : base(owner)
        {
            Owner.PropertyChanged += OwnerPropertyChanged;
        }

        public new TextBox Owner => (TextBox)base.Owner;
        public bool IsReadOnly => Owner.IsReadOnly;
        public string? Value => Owner.Text;
        public void SetValue(string? value) => Owner.Text = value;

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Edit;
        }

        protected override string? GetPlaceholderTextCore() => Owner.PlaceholderText;

        protected virtual void OwnerPropertyChanged(object? sender, PresentationPropertyChangedEventArgs e)
        {
            if(e.Property == TextBox.TextProperty)
            {
                RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, e.OldValue, e.NewValue);
            }
        }
    }
}
