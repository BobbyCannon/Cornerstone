using Cornerstone.Presentation.Data;

namespace Cornerstone.Presentation
{
    /// <summary>
    /// Provides information for an Cornerstone property change.
    /// </summary>
    public class PresentationPropertyChangedEventArgs<T> : PresentationPropertyChangedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PresentationPropertyChangedEventArgs"/> class.
        /// </summary>
        /// <param name="sender">The object that the property changed on.</param>
        /// <param name="property">The property that changed.</param>
        /// <param name="oldValue">The old value of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        /// <param name="priority">The priority of the binding that produced the value.</param>
        public PresentationPropertyChangedEventArgs(
            PresentationObject sender,
            PresentationProperty<T> property,
            Optional<T> oldValue,
            BindingValue<T> newValue,
            BindingPriority priority)
            : this(sender, property, oldValue, newValue, priority, true)
        {
        }

        internal PresentationPropertyChangedEventArgs(
            PresentationObject sender,
            PresentationProperty<T> property,
            Optional<T> oldValue,
            BindingValue<T> newValue,
            BindingPriority priority,
            bool isEffectiveValueChange)
            : base(sender, priority, isEffectiveValueChange)
        {
            Property = property;
            OldValue = oldValue;
            NewValue = newValue;
        }

        /// <summary>
        /// Gets the property that changed.
        /// </summary>
        /// <value>
        /// The property that changed.
        /// </value>
        public new PresentationProperty<T> Property { get; }

        /// <summary>
        /// Gets the old value of the property.
        /// </summary>
        public new Optional<T> OldValue { get; private set; }

        /// <summary>
        /// Gets the new value of the property.
        /// </summary>
        public new BindingValue<T> NewValue { get; private set; }

        protected override PresentationProperty GetProperty() => Property;

        protected override object? GetOldValue() => OldValue.GetValueOrDefault(PresentationProperty.UnsetValue);

        protected override object? GetNewValue() => NewValue.GetValueOrDefault(PresentationProperty.UnsetValue);
    }
}
