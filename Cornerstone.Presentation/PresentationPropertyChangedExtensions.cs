namespace Cornerstone.Presentation
{
    /// <summary>
    /// Provides extensions for <see cref="PresentationPropertyChangedEventArgs"/>.
    /// </summary>
    public static class PresentationPropertyChangedExtensions
    {
        /// <summary>
        /// Gets a typed value from <see cref="PresentationPropertyChangedEventArgs.OldValue"/>.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="e">The event args.</param>
        /// <returns>The value.</returns>
        public static T GetOldValue<T>(this PresentationPropertyChangedEventArgs e)
        {
            return ((PresentationPropertyChangedEventArgs<T>)e).OldValue.GetValueOrDefault()!;
        }

        /// <summary>
        /// Gets a typed value from <see cref="PresentationPropertyChangedEventArgs.NewValue"/>.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="e">The event args.</param>
        /// <returns>The value.</returns>
        public static T GetNewValue<T>(this PresentationPropertyChangedEventArgs e)
        {
            return ((PresentationPropertyChangedEventArgs<T>)e).NewValue.GetValueOrDefault()!;
        }

        /// <summary>
        /// Gets a typed value from <see cref="PresentationPropertyChangedEventArgs.OldValue"/> and
        /// <see cref="PresentationPropertyChangedEventArgs.NewValue"/>.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="e">The event args.</param>
        /// <returns>The value.</returns>
        public static (T oldValue, T newValue) GetOldAndNewValue<T>(this PresentationPropertyChangedEventArgs e)
        {
            var ev = (PresentationPropertyChangedEventArgs<T>)e;
            return (ev.OldValue.GetValueOrDefault()!, ev.NewValue.GetValueOrDefault()!);
        }
    }
}
