using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.PropertyStore;
using Cornerstone.Presentation.Utilities;
using static Cornerstone.Presentation.StyledPropertyNonGenericHelper;

namespace Cornerstone.Presentation
{
    /// <summary>
    /// A styled cornerstone property.
    /// </summary>
    public class StyledProperty<TValue> : PresentationProperty<TValue>, IStyledPropertyAccessor
    {
        // For performance, cache the default value if there's only one (mostly for PresentationObject.GetValue()),
        // avoiding a GetMetadata() call which might need to iterate through the control hierarchy.
        private Optional<TValue> _singleDefaultValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="StyledProperty{T}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="hostType">The class that the property being is registered on.</param>
        /// <param name="metadata">The property metadata.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="validate">
        /// <para>A method which returns "false" for values that are never valid for this property.</para>
        /// <para>This method is not part of the property's metadata and so cannot be changed after registration.</para>
        /// </param>
        /// <param name="notifying">A <see cref="PresentationProperty.Notifying"/> callback.</param>
        internal StyledProperty(
            string name,
            Type ownerType,
            Type hostType,
            StyledPropertyMetadata<TValue> metadata,
            bool inherits = false,
            Func<TValue, bool>? validate = null,
            Action<PresentationObject, bool>? notifying = null)
                : base(name, ownerType, hostType, metadata, notifying)
        {
            Inherits = inherits;
            ValidateValue = validate;

            if (validate?.Invoke(metadata.DefaultValue) == false)
            {
                ThrowInvalidDefaultValue(name, metadata.DefaultValue, name);
            }

            _singleDefaultValue = metadata.DefaultValue;
        }

        /// <summary>
        /// A method which returns "false" for values that are never valid for this property.
        /// </summary>
        public Func<TValue, bool>? ValidateValue { get; }

        /// <summary>
        /// Registers the property on another type.
        /// </summary>
        /// <typeparam name="TOwner">The type of the additional owner.</typeparam>
        /// <returns>The property.</returns>        
        public StyledProperty<TValue> AddOwner<TOwner>(StyledPropertyMetadata<TValue>? metadata = null) where TOwner : PresentationObject
        {
            PresentationPropertyRegistry.Instance.Register(typeof(TOwner), this);
            if (metadata != null)
            {
                OverrideMetadata<TOwner>(metadata);
            }

            return this;
        }

        public TValue CoerceValue(PresentationObject instance, TValue baseValue)
        {
            var metadata = GetMetadata(instance);

            if (metadata.CoerceValue != null)
            {
                return metadata.CoerceValue.Invoke(instance, baseValue);
            }

            return baseValue;
        }

        /// <summary>
        /// Gets the default value for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The default value.</returns>
        /// <remarks>
        /// For performance, prefer the <see cref="GetDefaultValue(Cornerstone.Presentation.PresentationObject)"/> overload when possible.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue GetDefaultValue(Type type)
        {
            return _singleDefaultValue.HasValue ?
                _singleDefaultValue.GetValueOrDefault()! :
                GetMetadata(type).DefaultValue;
        }

        /// <summary>
        /// Gets the default value for the property on the specified object.
        /// </summary>
        /// <param name="owner">The object.</param>
        /// <returns>The default value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue GetDefaultValue(PresentationObject owner)
        {
            return _singleDefaultValue.HasValue ?
                _singleDefaultValue.GetValueOrDefault()! :
                GetMetadata(owner).DefaultValue;
        }

        /// <inheritdoc cref="PresentationProperty.GetMetadata(System.Type)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new StyledPropertyMetadata<TValue> GetMetadata(Type type)
            => CastMetadata(base.GetMetadata(type));

        /// <inheritdoc cref="PresentationProperty.GetMetadata(Cornerstone.Presentation.PresentationObject)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new StyledPropertyMetadata<TValue> GetMetadata(PresentationObject owner)
            => CastMetadata(base.GetMetadata(owner));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static StyledPropertyMetadata<TValue> CastMetadata(PresentationPropertyMetadata metadata)
        {
#if DEBUG
            return (StyledPropertyMetadata<TValue>)metadata;
#else
            // Avoid casts in release mode for performance (GetMetadata is a hot path).
            // We control every path:
            // it shouldn't be possible a metadata type other than a StyledPropertyMetadata<T> stored for a StyledProperty<T>.
            return Unsafe.As<StyledPropertyMetadata<TValue>>(metadata);
#endif
        }

        /// <summary>
        /// Overrides the default value for the property on the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="defaultValue">The default value.</param>
        public void OverrideDefaultValue<T>(TValue defaultValue) where T : PresentationObject
        {
            OverrideDefaultValue(typeof(T), defaultValue);
        }

        /// <summary>
        /// Overrides the default value for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="defaultValue">The default value.</param>
        public void OverrideDefaultValue(Type type, TValue defaultValue)
        {
            OverrideMetadata(type, new StyledPropertyMetadata<TValue>(defaultValue));
        }

        /// <summary>
        /// Overrides the metadata for the property on the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="metadata">The metadata.</param>
        public void OverrideMetadata<T>(StyledPropertyMetadata<TValue> metadata) where T : PresentationObject => OverrideMetadata(typeof(T), metadata);

        /// <summary>
        /// Overrides the metadata for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="metadata">The metadata.</param>
        public void OverrideMetadata(Type type, StyledPropertyMetadata<TValue> metadata)
        {
            if (ValidateValue != null)
            {
                if (!ValidateValue(metadata.DefaultValue))
                {
                    ThrowInvalidDefaultValue(Name, metadata.DefaultValue, nameof(metadata));
                }
            }

            base.OverrideMetadata(type, metadata);

            if (_singleDefaultValue != metadata.DefaultValue)
            {
                _singleDefaultValue = default;
            }
        }

        /// <summary>
        /// Gets the string representation of the property.
        /// </summary>
        /// <returns>The property's string representation.</returns>
        public override string ToString()
        {
            return Name;
        }

        object? IStyledPropertyAccessor.GetDefaultValue(Type type) => GetDefaultValue(type);

        object? IStyledPropertyAccessor.GetDefaultValue(PresentationObject owner) => GetDefaultValue(owner);

        bool IStyledPropertyAccessor.ValidateValue(object? value)
        {
            if (value is null)
            {
                if (!typeof(TValue).IsValueType || Nullable.GetUnderlyingType(typeof(TValue)) != null)
                    return ValidateValue?.Invoke(default!) ?? true;
            }
            else if (value is TValue typed)
            {
                return ValidateValue?.Invoke(typed) ?? true;
            }

            return false;
        }

        internal override EffectiveValue CreateEffectiveValue(PresentationObject o)
        {
            return o.GetValueStore().CreateEffectiveValue(this);
        }

        /// <inheritdoc/>
        internal override void RouteClearValue(PresentationObject o)
        {
            o.ClearValue<TValue>(this);
        }

        internal override void RouteCoerceDefaultValue(PresentationObject o)
        {
            o.GetValueStore().CoerceDefaultValue(this);
        }

        /// <inheritdoc/>
        internal override object? RouteGetValue(PresentationObject o)
        {
            return o.GetValue<TValue>(this);
        }

        /// <inheritdoc/>
        internal override object? RouteGetBaseValue(PresentationObject o)
        {
            var value = o.GetBaseValue<TValue>(this);
            return value.HasValue ? value.Value : PresentationProperty.UnsetValue;
        }

        /// <inheritdoc/>
        internal override IDisposable? RouteSetValue(
            PresentationObject target,
            object? value,
            BindingPriority priority)
        {
            if (ShouldSetValue(target, value, out var converted))
                return target.SetValue<TValue>(this, converted, priority);
            return null;
        }

        internal override void RouteSetCurrentValue(PresentationObject target, object? value)
        {
            if (ShouldSetValue(target, value, out var converted))
                target.SetCurrentValue<TValue>(this, converted);
        }

        internal override IDisposable RouteBind(
            PresentationObject target,
            IObservable<object?> source,
            BindingPriority priority)
        {
            return target.Bind<TValue>(this, source, priority);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingMessages.ImplicitTypeConversionSupressWarningMessage)]
        private bool ShouldSetValue(PresentationObject target, object? value, [NotNullWhen(true)] out TValue? converted)
        {
            if (value != BindingOperations.DoNothing)
            {
                if (value == UnsetValue)
                {
                    target.ClearValue(this);
                }
                else if (TypeUtilities.TryConvertImplicit(PropertyType, value, out var v))
                {
                    converted = (TValue)v!;
                    return true;
                }
                else
                {
                    ThrowInvalidValue(Name, value, nameof(value));
                }
            }

            converted = default;
            return false;
        }
    }
}
