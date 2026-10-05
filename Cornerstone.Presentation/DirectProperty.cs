using System;
using Cornerstone.Presentation.Data;

namespace Cornerstone.Presentation
{
    /// <summary>
    /// A direct cornerstone property.
    /// </summary>
    /// <typeparam name="TOwner">The class that registered the property.</typeparam>
    /// <typeparam name="TValue">The type of the property's value.</typeparam>
    /// <remarks>
    /// Direct cornerstone properties are backed by a field on the object, but exposed via the
    /// <see cref="PresentationProperty"/> system. They hold a getter and an optional setter which
    /// allows the cornerstone property system to read and write the current value.
    /// </remarks>
    public class DirectProperty<TOwner, TValue> : DirectPropertyBase<TValue>, IDirectPropertyAccessor
        where TOwner : PresentationObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectProperty{TOwner, TValue}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="getter">Gets the current value of the property.</param>
        /// <param name="setter">Sets the value of the property. May be null.</param>
        /// <param name="metadata">The property metadata.</param>
        internal DirectProperty(
            string name,
            Func<TOwner, TValue> getter,
            Action<TOwner, TValue>? setter,
            DirectPropertyMetadata<TValue> metadata)
            : base(name, typeof(TOwner), metadata)
        {
            Getter = getter ?? throw new ArgumentNullException(nameof(getter));
            Setter = setter;
            IsReadOnly = setter is null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PresentationProperty"/> class.
        /// </summary>
        /// <param name="source">The property to copy.</param>
        /// <param name="getter">Gets the current value of the property.</param>
        /// <param name="setter">Sets the value of the property. May be null.</param>
        /// <param name="metadata">Optional overridden metadata.</param>
        private DirectProperty(
            DirectPropertyBase<TValue> source,
            Func<TOwner, TValue> getter,
            Action<TOwner, TValue>? setter,
            DirectPropertyMetadata<TValue> metadata)
            : base(source, typeof(TOwner), metadata)
        {
            Getter = getter ?? throw new ArgumentNullException(nameof(getter));
            Setter = setter;
            IsReadOnly = setter is null;
        }

        /// <summary>
        /// Gets the getter function.
        /// </summary>
        public Func<TOwner, TValue> Getter { get; }

        /// <summary>
        /// Gets the setter function.
        /// </summary>
        public Action<TOwner, TValue>? Setter { get; }

        /// <summary>
        /// Registers the direct property on another type.
        /// </summary>
        /// <typeparam name="TNewOwner">The type of the additional owner.</typeparam>
        /// <param name="getter">Gets the current value of the property.</param>
        /// <param name="setter">Sets the value of the property.</param>
        /// <param name="unsetValue">
        /// The value to use when the property is set to <see cref="PresentationProperty.UnsetValue"/>
        /// </param>
        /// <param name="defaultBindingMode">The default binding mode for the property.</param>
        /// <param name="enableDataValidation">
        /// Whether the property is interested in data validation.
        /// </param>
        /// <returns>The property.</returns>
        public DirectProperty<TNewOwner, TValue> AddOwner<TNewOwner>(
            Func<TNewOwner, TValue> getter,
            Action<TNewOwner, TValue>? setter = null,
            TValue unsetValue = default!,
            BindingMode defaultBindingMode = BindingMode.Default,
            bool enableDataValidation = false)
                where TNewOwner : PresentationObject
        {
            var metadata = new DirectPropertyMetadata<TValue>(
                unsetValue: unsetValue,
                defaultBindingMode: defaultBindingMode,
                enableDataValidation: enableDataValidation);

            metadata.Merge(GetMetadata<TOwner>(), this);
            metadata.Freeze();

            var result = new DirectProperty<TNewOwner, TValue>(
                this,
                getter,
                setter,
                metadata);

            PresentationPropertyRegistry.Instance.Register(typeof(TNewOwner), result);
            return result;
        }

        /// <inheritdoc/>
        internal override TValue InvokeGetter(PresentationObject instance)
        {
            return Getter((TOwner)instance);
        }

        /// <inheritdoc/>
        internal override void InvokeSetter(PresentationObject instance, BindingValue<TValue> value)
        {
            if (Setter == null)
            {
                throw new ArgumentException($"The property {Name} is readonly.");
            }

            if (value.HasValue)
            {
                Setter((TOwner)instance, value.Value);
            }
        }

        /// <inheritdoc/>
        object? IDirectPropertyAccessor.GetValue(PresentationObject instance)
        {
            return Getter((TOwner)instance);
        }

        /// <inheritdoc/>
        void IDirectPropertyAccessor.SetValue(PresentationObject instance, object? value)
        {
            if (Setter == null)
            {
                throw new ArgumentException($"The property {Name} is readonly.");
            }

            Setter((TOwner)instance, (TValue)value!);
        }

        object? IDirectPropertyAccessor.GetUnsetValue(Type type)
            => GetMetadata(type).UnsetValue;

        object? IDirectPropertyAccessor.GetUnsetValue(PresentationObject owner)
            => GetMetadata(owner).UnsetValue;
    }
}
