using System;
using Cornerstone.Presentation.Utilities;

namespace Cornerstone.Presentation.Data.Core.Plugins
{
    /// <summary>
    /// Reads a property from a <see cref="PresentationObject"/>.
    /// </summary>
    internal class PresentationPropertyAccessorPlugin : IPropertyAccessorPlugin
    {
        /// <inheritdoc/>
        public bool Match(object obj, string propertyName)
        {
            if (obj is PresentationObject o)
            {
                return LookupProperty(o, propertyName) != null;
            }

            return false;
        }

        /// <summary>
        /// Starts monitoring the value of a property on an object.
        /// </summary>
        /// <param name="reference">A weak reference to the object.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>
        /// An <see cref="IPropertyAccessor"/> interface through which future interactions with the 
        /// property will be made.
        /// </returns>
        public IPropertyAccessor? Start(WeakReference<object?> reference, string propertyName)
        {
            _ = reference ?? throw new ArgumentNullException(nameof(reference));
            _ = propertyName ?? throw new ArgumentNullException(nameof(propertyName));

            if (!reference.TryGetTarget(out var instance) || instance is null)
                return null;

            var o = (PresentationObject)instance;
            var p = LookupProperty(o, propertyName);

            if (p != null)
            {
                return new Accessor(new WeakReference<PresentationObject>(o), p);
            }
            else if (instance != PresentationProperty.UnsetValue)
            {
                var message = $"Could not find PresentationProperty '{propertyName}' on '{instance}'";
                var exception = new MissingMemberException(message);
                return new PropertyError(new BindingNotification(exception, BindingErrorType.Error));
            }
            else
            {
                return null;
            }
        }

        private static PresentationProperty? LookupProperty(PresentationObject o, string propertyName)
        {
            return PresentationPropertyRegistry.Instance.FindRegistered(o, propertyName);
        }

        private class Accessor : PropertyAccessorBase, IWeakEventSubscriber<PresentationPropertyChangedEventArgs>
        {
            private readonly WeakReference<PresentationObject> _reference;
            private readonly PresentationProperty _property;

            public Accessor(WeakReference<PresentationObject> reference, PresentationProperty property)
            {
                _reference = reference ?? throw new ArgumentNullException(nameof(reference));
                _property = property ?? throw new ArgumentNullException(nameof(property));
            }

            public PresentationObject? Instance
            {
                get
                {
                    _reference.TryGetTarget(out var result);
                    return result;
                }
            }

            public override Type? PropertyType => _property?.PropertyType;
            public override object? Value => Instance?.GetValue(_property);

            public override bool SetValue(object? value, BindingPriority priority)
            {
                if (!_property.IsReadOnly)
                {
                    Instance?.SetValue(_property, value, priority);
                    return true;
                }

                return false;
            }

            void IWeakEventSubscriber<PresentationPropertyChangedEventArgs>.
                OnEvent(object? notifyPropertyChanged, WeakEvent ev, PresentationPropertyChangedEventArgs e)
            {
                if (e.Property == _property)
                {
                    SendCurrentValue();
                }
            }

            protected override void SubscribeCore()
            {
                SubscribeToChanges();
                SendCurrentValue();
            }

            protected override void UnsubscribeCore()
            {
                var instance = Instance;

                if (instance != null)
                    WeakEvents.PresentationPropertyChanged.Unsubscribe(instance, this);
            }

            private void SendCurrentValue()
            {
                try
                {
                    var value = Value;
                    PublishValue(value);
                }
                catch { }
            }

            private void SubscribeToChanges()
            {
                var instance = Instance;

                if (instance != null)
                    WeakEvents.PresentationPropertyChanged.Subscribe(instance, this);
            }
        }
    }
}
