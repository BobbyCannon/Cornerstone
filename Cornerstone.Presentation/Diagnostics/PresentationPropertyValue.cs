using Cornerstone.Presentation.Data;

namespace Cornerstone.Presentation.Diagnostics
{
    /// <summary>
    /// Holds diagnostic-related information about the value of an <see cref="PresentationProperty"/>
    /// on an <see cref="PresentationObject"/>.
    /// </summary>
    public sealed class PresentationPropertyValue
    {
        internal PresentationPropertyValue(
            PresentationProperty property,
            object? value,
            BindingPriority priority,
            string? diagnostic,
            bool isOverriddenCurrentValue)
        {
            Property = property;
            Value = value;
            Priority = priority;
            Diagnostic = diagnostic;
            IsOverriddenCurrentValue = isOverriddenCurrentValue;
        }

        /// <summary>
        /// Gets the property.
        /// </summary>
        public PresentationProperty Property { get; }

        /// <summary>
        /// Gets the current property value.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Gets the priority of the current value.
        /// </summary>
        public BindingPriority Priority { get; }

        /// <summary>
        /// Gets a diagnostic string.
        /// </summary>
        public string? Diagnostic { get; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Value"/> was overridden by a call to 
        /// <see cref="PresentationObject.SetCurrentValue{T}"/>.
        /// </summary>
        public bool IsOverriddenCurrentValue { get; }
    }
}
