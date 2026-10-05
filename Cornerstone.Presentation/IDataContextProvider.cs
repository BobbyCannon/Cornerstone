using Cornerstone.Presentation.Metadata;

namespace Cornerstone.Presentation
{
    /// <summary>
    /// Defines an element with a data context that can be used for binding.
    /// </summary>
    [NotClientImplementable]
    public interface IDataContextProvider
    {
        /// <summary>
        /// Gets or sets the element's data context.
        /// </summary>
        object? DataContext { get; set; }
    }
}
