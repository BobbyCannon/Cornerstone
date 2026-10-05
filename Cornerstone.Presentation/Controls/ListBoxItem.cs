using Cornerstone.Presentation.Automation;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Controls.Metadata;
using Cornerstone.Presentation.Controls.Mixins;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Controls.Primitives;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Controls.Feedback;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Items;

namespace Cornerstone.Presentation.Controls
{
    /// <summary>
    /// A selectable item in a <see cref="ListBox"/>.
    /// </summary>
    [PseudoClasses(":pressed", ":selected")]
    public class ListBoxItem : ContentControl, ISelectable
    {
        /// <summary>
        /// Defines the <see cref="IsSelected"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsSelectedProperty =
            SelectingItemsControl.IsSelectedProperty.AddOwner<ListBoxItem>();

        /// <summary>
        /// Initializes static members of the <see cref="ListBoxItem"/> class.
        /// </summary>
        static ListBoxItem()
        {
            SelectableMixin.Attach<ListBoxItem>(IsSelectedProperty);
            PressedMixin.Attach<ListBoxItem>();
            FocusableProperty.OverrideDefaultValue<ListBoxItem>(true);
            AutomationProperties.IsOffscreenBehaviorProperty.OverrideDefaultValue<ListBoxItem>(IsOffscreenBehavior.FromClip);
            PlatformFeedback.FeedbackTypeProperty.OverrideDefaultValue<ListBoxItem>(FeedbackType.Auto);
        }

        /// <summary>
        /// Gets or sets the selection state of the item.
        /// </summary>
        public bool IsSelected
        {
            get => GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ListItemAutomationPeer(this);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            UpdateSelectionFromEvent(e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            UpdateSelectionFromEvent(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            UpdateSelectionFromEvent(e);
        }

        protected bool UpdateSelectionFromEvent(RoutedEventArgs e) => SelectingItemsControl.ItemsControlFromItemContainer(this)?.UpdateSelectionFromEvent(this, e) ?? false;
    }
}
