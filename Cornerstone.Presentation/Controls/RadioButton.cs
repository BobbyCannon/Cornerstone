using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Controls.Automation.Peers;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.Rendering;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Controls.Input;

namespace Cornerstone.Presentation.Controls
{
    /// <summary>
    /// Represents a button that allows a user to select a single option from a group of options.
    /// </summary>
    public class RadioButton : ToggleButton, IRadioButton
    {
        /// <summary>
        /// Identifies the GroupName dependency property.
        /// </summary>
        public static readonly StyledProperty<string?> GroupNameProperty =
            PresentationProperty.Register<RadioButton, string?>(nameof(GroupName));

        private RadioButtonGroupManager? _groupManager;

        /// <summary>
        /// Gets or sets the name that specifies which RadioButton controls are mutually exclusive.
        /// </summary>
        public string? GroupName
        {
            get => GetValue(GroupNameProperty);
            set => SetValue(GroupNameProperty, value);
        }

        bool IRadioButton.IsChecked
        {
            get => IsChecked.GetValueOrDefault();
            set => SetCurrentValue(IsCheckedProperty, value);
        }

        MenuItemToggleType IRadioButton.ToggleType => MenuItemToggleType.Radio;

        protected override void Toggle()
        {
            if (!IsChecked.GetValueOrDefault())
            {
                SetCurrentValue(IsCheckedProperty, true);
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _groupManager?.Remove(this, GroupName);
            EnsureRadioGroupManager(e.PresentationSource);
            base.OnAttachedToVisualTree(e);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            _groupManager?.Remove(this, GroupName);
            _groupManager = null;
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new RadioButtonAutomationPeer(this);
        }

        protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsCheckedProperty)
            {
                IsCheckedChanged(change.GetNewValue<bool?>());
            }
            else if (change.Property == GroupNameProperty)
            {
                var (oldValue, newValue) = change.GetOldAndNewValue<string?>();
                OnGroupNameChanged(oldValue, newValue);
            }
        }

        private void OnGroupNameChanged(string? oldGroupName, string? newGroupName)
        {
            if (!string.IsNullOrEmpty(oldGroupName))
            {
                _groupManager?.Remove(this, oldGroupName);
            }
            if (!string.IsNullOrEmpty(newGroupName))
            {
                EnsureRadioGroupManager();
            }
        }

        private new void IsCheckedChanged(bool? value)
        {
            if (value.GetValueOrDefault())
            {
                EnsureRadioGroupManager();
                _groupManager.OnCheckedChanged(this);
            }
        }
        
        [MemberNotNull(nameof(_groupManager))]
        private void EnsureRadioGroupManager(IPresentationSource? source = null)
        {
            _groupManager = RadioButtonGroupManager.GetOrCreateForRoot(source ?? this.GetPresentationSource());
            _groupManager.Add(this);
        }
    }
}
