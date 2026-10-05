using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation.Layout
{
    /// <summary>
    /// Defines the root of a layoutable tree.
    /// </summary>
    internal interface ILayoutRoot
    {
        /// <summary>
        /// The scaling factor to use in layout.
        /// </summary>
        public double LayoutScaling { get; }

        /// <summary>
        /// Associated instance of layout manager
        /// </summary>
        public ILayoutManager LayoutManager { get; }
        
        public Layoutable RootVisual { get; }
    }
}
