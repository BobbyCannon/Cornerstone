using System;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Reactive;
using Cornerstone.Presentation.Utilities;

namespace Cornerstone.Presentation
{
    /// <summary>
    /// A typed cornerstone property.
    /// </summary>
    /// <typeparam name="TValue">The value type of the property.</typeparam>
    public abstract class PresentationProperty<TValue> : PresentationProperty
    {
        private readonly LightweightSubject<PresentationPropertyChangedEventArgs<TValue>> _changed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PresentationProperty{TValue}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="hostType">The class that the property being is registered on.</param>
        /// <param name="metadata">The property metadata.</param>
        /// <param name="notifying">A <see cref="PresentationProperty.Notifying"/> callback.</param>
        private protected PresentationProperty(
            string name,
            Type ownerType,
            Type hostType,
            PresentationPropertyMetadata metadata,
            Action<PresentationObject, bool>? notifying = null)
            : base(name, typeof(TValue), ownerType, hostType, metadata, notifying)
        {
            _changed = new LightweightSubject<PresentationPropertyChangedEventArgs<TValue>>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PresentationProperty{TValue}"/> class.
        /// </summary>
        /// <param name="source">The property to copy.</param>
        /// <param name="ownerType">The new owner type.</param>
        /// <param name="metadata">Optional overridden metadata.</param>
        private protected PresentationProperty(
            PresentationProperty<TValue> source,
            Type ownerType,
            PresentationPropertyMetadata? metadata)
            : base(source, ownerType, metadata)
        {
            _changed = source._changed;
        }

        /// <summary>
        /// Gets an observable that is fired when this property changes on any
        /// <see cref="PresentationObject"/> instance.
        /// </summary>
        /// <value>
        /// An observable that is fired when this property changes on any
        /// <see cref="PresentationObject"/> instance.
        /// </value>

        public new IObservable<PresentationPropertyChangedEventArgs<TValue>> Changed => _changed;

        /// <summary>
        /// Notifies the <see cref="Changed"/> observable.
        /// </summary>
        /// <param name="e">The observable arguments.</param>
        internal void NotifyChanged(PresentationPropertyChangedEventArgs<TValue> e)
        {
            _changed.OnNext(e);
        }

        private protected override IObservable<PresentationPropertyChangedEventArgs> GetChanged() => Changed;

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingMessages.ImplicitTypeConversionSupressWarningMessage)]
        private protected BindingValue<object?> TryConvert(object? value)
        {
            if (value == UnsetValue)
            {
                return BindingValue<object?>.Unset;
            }
            else if (value == BindingOperations.DoNothing)
            {
                return BindingValue<object?>.DoNothing;
            }

            if (!TypeUtilities.TryConvertImplicit(PropertyType, value, out var converted))
            {
                var error = new ArgumentException(string.Format(
                    "Invalid value for Property '{0}': '{1}' ({2})",
                    Name,
                    value,
                    value?.GetType().FullName ?? "(null)"));
                return BindingValue<object?>.BindingError(error);
            }

            return converted;
        }
    }
}
