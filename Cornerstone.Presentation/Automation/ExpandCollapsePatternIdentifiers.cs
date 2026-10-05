using Cornerstone.Presentation.Automation.Provider;

namespace Cornerstone.Presentation.Automation
{
    /// <summary>
    /// Contains values used as identifiers by <see cref="IExpandCollapseProvider"/>.
    /// </summary>
    public static class ExpandCollapsePatternIdentifiers
    {
        /// <summary>
        /// Identifies <see cref="IExpandCollapseProvider.ExpandCollapseState"/> automation property.
        /// </summary>
        public static AutomationProperty ExpandCollapseStateProperty { get; } = new AutomationProperty();
    }
}
