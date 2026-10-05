using System;
using Cornerstone.Presentation.Data;

namespace Cornerstone.Presentation.PropertyStore
{
    /// <summary>
    /// An <see cref="IValueEntry"/> that holds a binding whose source observable and target
    /// property are both typed.
    /// </summary>
    internal sealed class TypedBindingEntry<T> : BindingEntryBase<T, T>
    {
        public TypedBindingEntry(
            PresentationObject target,
            ValueFrame frame, 
            StyledProperty<T> property,
            IObservable<T> source)
                : base(target, frame, property, source)
        {
        }

        public TypedBindingEntry(
            PresentationObject target,
            ValueFrame frame,
            StyledProperty<T> property,
            IObservable<BindingValue<T>> source)
                : base(target, frame, property, source)
        {
        }

        public new StyledProperty<T> Property => (StyledProperty<T>)base.Property;

        protected override BindingValue<T> ConvertAndValidate(T value)
        {
            if (Property.ValidateValue?.Invoke(value) == false)
            {
                return BindingValue<T>.BindingError(
                    new InvalidCastException($"'{value}' is not a valid value."));
            }

            return value;
        }

        protected override BindingValue<T> ConvertAndValidate(BindingValue<T> value)
        {
            if (value.HasValue && Property.ValidateValue?.Invoke(value.Value) == false)
            {
                return BindingValue<T>.BindingError(
                    new InvalidCastException($"'{value.Value}' is not a valid value."));
            }
            
            return value;
        }

        protected override T GetDefaultValue(PresentationObject owner) => Property.GetDefaultValue(owner);
    }
}
