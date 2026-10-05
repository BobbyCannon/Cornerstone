using Cornerstone.Presentation.Automation.Provider;

namespace Cornerstone.Presentation.Automation
{
    /// <summary>
    /// Contains values used as identifiers by <see cref="ISelectionItemProvider"/>.
    /// </summary>
    public static class SelectionItemPatternIdentifiers
    {
        /// <summary>Indicates the element is currently selected.</summary>
        public static AutomationProperty IsSelectedProperty { get; } = new AutomationProperty();

        /// <summary>Indicates the element is currently selected.</summary>
        public static AutomationProperty SelectionContainerProperty { get; } = new AutomationProperty();
    }
}
