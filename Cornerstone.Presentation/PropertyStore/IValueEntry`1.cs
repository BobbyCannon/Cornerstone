namespace Cornerstone.Presentation.PropertyStore
{
    /// <summary>
    /// Represents a typed value entry in a <see cref="ValueFrame"/>.
    /// </summary>
    internal interface IValueEntry<T> : IValueEntry
    {
        /// <summary>
        /// Gets the value associated with the entry.
        /// </summary>
        /// <exception cref="PresentationInternalException">
        /// The entry has no value.
        /// </exception>
        new T GetValue();
    }
}
