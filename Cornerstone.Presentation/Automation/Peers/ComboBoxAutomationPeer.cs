using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Automation.Provider;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Text;

namespace Cornerstone.Presentation.Automation.Peers
{
    public class ComboBoxAutomationPeer : SelectingItemsControlAutomationPeer,
        IExpandCollapseProvider,
        IValueProvider
    {
        private UnrealizedSelectionPeer[]? _selection;

        public ComboBoxAutomationPeer(ComboBox owner)
            : base(owner)
        {
        }

        public new ComboBox Owner => (ComboBox)base.Owner;

        public ExpandCollapseState ExpandCollapseState => ToState(Owner.IsDropDownOpen);
        public bool ShowsMenu => true;
        public void Collapse() => Owner.IsDropDownOpen = false;
        public void Expand() => Owner.IsDropDownOpen = true;
        bool IValueProvider.IsReadOnly => !Owner.IsEditable;

        string? IValueProvider.Value
        {
            get
            {
                if (Owner.IsEditable)
                    return Owner.Text;

                var selection = GetSelection();
                return selection.Count == 1 ? selection[0].GetName() : null;
            }
        }

        void IValueProvider.SetValue(string? value)
        {
            if (!Owner.IsEditable)
                throw new InvalidOperationException("Cannot set the value of a non-editable ComboBox.");

            Owner.SetCurrentValue(ComboBox.TextProperty, value);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ComboBox;
        }

        protected override IReadOnlyList<AutomationPeer>? GetSelectionCore()
        {
            if (ExpandCollapseState == ExpandCollapseState.Expanded)
                return base.GetSelectionCore();

            // If the combo box is not open then we won't have an ItemsPresenter so the default
            // GetSelectionCore implementation won't work. For this case we create a separate
            // peer to represent the unrealized item.
            if (Owner.SelectedItem is object selection)
            {
                _selection ??= new[] { new UnrealizedSelectionPeer(this) };
                _selection[0].Item = selection;
                return _selection;
            }

            return null;
        }

        protected override void OwnerPropertyChanged(object? sender, PresentationPropertyChangedEventArgs e)
        {
            base.OwnerPropertyChanged(sender, e);

            if (e.Property == ComboBox.IsDropDownOpenProperty)
            {
                RaisePropertyChangedEvent(
                    ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                    ToState((bool)e.OldValue!),
                    ToState((bool)e.NewValue!));
            }
            else if (e.Property == ComboBox.TextProperty && Owner.IsEditable)
            {
                RaisePropertyChangedEvent(
                    ValuePatternIdentifiers.ValueProperty,
                    e.OldValue as string,
                    e.NewValue as string);
            }
            else if (e.Property == ComboBox.IsEditableProperty)
            {
                RaisePropertyChangedEvent(
                    ValuePatternIdentifiers.IsReadOnlyProperty,
                    !(bool)e.OldValue!,
                    !(bool)e.NewValue!);

                // The Value pattern switches between SelectedItem name and Text when IsEditable changes.
                RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, null, null);
            }
        }

        private static ExpandCollapseState ToState(bool value)
        {
            return value ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
        }

        private class UnrealizedSelectionPeer : UnrealizedElementAutomationPeer
        {
            private readonly ComboBoxAutomationPeer _owner;
            private object? _item;

            public UnrealizedSelectionPeer(ComboBoxAutomationPeer owner)
            {
                _owner = owner;
            }

            public object? Item
            {
                get => _item;
                set
                {
                    if (_item != value)
                    {
                        var oldValue = GetNameCore();
                        _item = value;
                        RaisePropertyChangedEvent(
                            AutomationElementIdentifiers.NameProperty,
                            oldValue,
                            GetNameCore());
                    }
                }
            }

            protected override string? GetAcceleratorKeyCore() => null;
            protected override string? GetAccessKeyCore() => null;
            protected override string? GetAutomationIdCore() => null;
            protected override string GetClassNameCore() => typeof(ComboBoxItem).Name;
            protected override AutomationPeer? GetLabeledByCore() => null;
            protected override AutomationPeer? GetParentCore() => _owner;
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;

            protected override string? GetNameCore()
            {
                if (_item is Control c)
                {
                    var result = AutomationProperties.GetName(c);

                    if (result is null && c is ContentControl cc && cc.Presenter?.Child is TextBlock text)
                    {
                        result = text.Text;
                    }

                    if (result is null)
                    {
                        result = c.GetValue(ContentControl.ContentProperty)?.ToString();
                    }

                    return result;
                }

                return _item?.ToString();
            }
        }
    }
}
