using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Navigation;

namespace Cornerstone.Presentation.Controls.Primitives
{
    /// <summary>
    /// Provides calculated values for use with the <see cref="SplitView"/>'s control theme or template.
    /// </summary>
    /// <remarks>
    /// This class is NOT intended for general use outside of control templates.
    /// </remarks>
    public class SplitViewTemplateSettings : PresentationObject
    {
        internal SplitViewTemplateSettings() { }

        public static readonly StyledProperty<double> ClosedPaneWidthProperty =
            PresentationProperty.Register<SplitViewTemplateSettings,
                double>(nameof(ClosedPaneWidth),
                0d);

        public static readonly StyledProperty<GridLength> PaneColumnGridLengthProperty =
            PresentationProperty.Register<SplitViewTemplateSettings, GridLength>(
                nameof(PaneColumnGridLength));

        public static readonly StyledProperty<double> ClosedPaneHeightProperty =
            PresentationProperty.Register<SplitViewTemplateSettings,
                double>(nameof(ClosedPaneHeight),
                0d);

        public static readonly StyledProperty<GridLength> PaneRowGridLengthProperty =
            PresentationProperty.Register<SplitViewTemplateSettings, GridLength>(
                nameof(PaneRowGridLength));

        [System.Diagnostics.CodeAnalysis.SuppressMessage("PresentationProperty", "AVP1032", Justification = "This property is supposed to be a styled readonly property.")]
        public double ClosedPaneWidth
        {
            get => GetValue(ClosedPaneWidthProperty);
            internal set => SetValue(ClosedPaneWidthProperty, value);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("PresentationProperty", "AVP1032", Justification = "This property is supposed to be a styled readonly property.")]
        public GridLength PaneColumnGridLength
        {
            get => GetValue(PaneColumnGridLengthProperty);
            internal set => SetValue(PaneColumnGridLengthProperty, value);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("PresentationProperty", "AVP1032", Justification = "This property is supposed to be a styled readonly property.")]
        public double ClosedPaneHeight
        {
            get => GetValue(ClosedPaneHeightProperty);
            internal set => SetValue(ClosedPaneHeightProperty, value);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("PresentationProperty", "AVP1032", Justification = "This property is supposed to be a styled readonly property.")]
        public GridLength PaneRowGridLength
        {
            get => GetValue(PaneRowGridLengthProperty);
            internal set => SetValue(PaneRowGridLengthProperty, value);
        }
    }
}
