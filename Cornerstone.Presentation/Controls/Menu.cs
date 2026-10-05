using Cornerstone.Presentation.Automation;
using Cornerstone.Presentation.Automation.Peers;
using Cornerstone.Presentation.Controls.Platform;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Controls.Items;

namespace Cornerstone.Presentation.Controls
{
    /// <summary>
    /// A top-level menu control.
    /// </summary>
    public class Menu : MenuBase, IMainMenu
    {
        private IAccessKeyHandler? _accessKeyHandler;

        private static readonly FuncTemplate<Panel?> DefaultPanel =
            new (() => new StackPanel { Orientation = Orientation.Horizontal });

        /// <summary>
        /// Initializes a new instance of the <see cref="Menu"/> class.
        /// </summary>
        public Menu()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Menu"/> class.
        /// </summary>
        /// <param name="interactionHandler">The menu interaction handler.</param>
        public Menu(IMenuInteractionHandler interactionHandler)
            : base(interactionHandler)
        {
        }

        static Menu()
        {
            ItemsPanelProperty.OverrideDefaultValue(typeof(Menu), DefaultPanel);
            KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue(
                typeof(Menu),
                KeyboardNavigationMode.Once);
            AutomationProperties.AccessibilityViewProperty.OverrideDefaultValue<Menu>(AccessibilityView.Control);
            AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<Menu>(AutomationControlType.Menu);
            AccessKeyHandler.AccessKeyPressedEvent.AddClassHandler<Menu>(OnAccessKeyPressed);
        }
        
        /// <inheritdoc cref="IMainMenu.Close"/>
        public override void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            foreach (var i in ((IMenu)this).SubItems)
            {
                i.Close();
            }

            IsOpen = false;
            SelectedIndex = -1;

            RaiseEvent(new RoutedEventArgs
            {
                RoutedEvent = ClosedEvent,
                Source = this,
            });
        }

        /// <inheritdoc cref="IMainMenu.Open"/>
        public override void Open()
        {
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;

            RaiseEvent(new RoutedEventArgs
            {
                RoutedEvent = OpenedEvent,
                Source = this,
            });
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            _accessKeyHandler = TopLevel.GetTopLevel(this)?.AccessKeyHandler;
            _accessKeyHandler?.MainMenu = this;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            if (_accessKeyHandler?.MainMenu == this)
                _accessKeyHandler.MainMenu = null;

            _accessKeyHandler = null;

            base.OnDetachedFromVisualTree(e);
        }

        protected internal override void PrepareContainerForItemOverride(Control element, object? item, int index)
        {
            base.PrepareContainerForItemOverride(element, item, index);

            // Child menu items should not inherit the menu's ItemContainerTheme as that is specific
            // for top-level menu items.
            if ((element as MenuItem)?.ItemContainerTheme == ItemContainerTheme)
                element.ClearValue(ItemContainerThemeProperty);
        }
        
        private static void OnAccessKeyPressed(Menu sender, AccessKeyPressedEventArgs e)
        {
            if (e.Handled || e.Source is not StyledElement target) 
                return;
            
            e.Target = DefaultMenuInteractionHandler.GetMenuItemCore(target);
            e.Handled = true;
        }
    }
}
