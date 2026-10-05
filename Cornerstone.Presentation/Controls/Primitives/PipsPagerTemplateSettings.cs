using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls.Items;

namespace Cornerstone.Presentation.Controls.Primitives
{
    /// <summary>
    /// Provides calculated values for use with the <see cref="PipsPager"/>'s control theme or template.
    /// </summary>
    public class PipsPagerTemplateSettings : PresentationObject
    {
        private OldPresentationList<int> _pips;

        internal PipsPagerTemplateSettings()
        {
            _pips = new OldPresentationList<int>();
        }

        public static readonly DirectProperty<PipsPagerTemplateSettings, OldPresentationList<int>> PipsProperty =
            PresentationProperty.RegisterDirect<PipsPagerTemplateSettings, OldPresentationList<int>>(
                nameof(Pips),
                o => o.Pips);

        /// <summary>
        /// Gets the collection of pips indices.
        /// </summary>
        public OldPresentationList<int> Pips
        {
            get => _pips;
        }
    }
}
