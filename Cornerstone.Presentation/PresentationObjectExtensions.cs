using System;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Reactive;

namespace Cornerstone.Presentation
{
    /// <summary>
    /// Provides extension methods for <see cref="PresentationObject"/> and related classes.
    /// </summary>
    public static class PresentationObjectExtensions
    {
        /// <summary>
        /// Converts an <see cref="IObservable{T}"/> to an <see cref="BindingBase"/>.
        /// </summary>
        /// <typeparam name="T">The type produced by the observable.</typeparam>
        /// <param name="source">The observable</param>
        /// <returns>An <see cref="BindingBase"/>.</returns>
        public static BindingBase ToBinding<T>(this IObservable<T> source)
        {
            return new BindingAdaptor(
                typeof(T).IsValueType
                    ? source.Select(x => (object?)x)
                    : (IObservable<object?>)source);
        }

        /// <summary>
        /// Gets an observable for an <see cref="PresentationProperty"/>.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>
        /// An observable which fires immediately with the current value of the property on the
        /// object and subsequently each time the property value changes.
        /// </returns>
        /// <remarks>
        /// The subscription to <paramref name="o"/> is created using a weak reference.
        /// </remarks>
        public static IObservable<object?> GetObservable(this PresentationObject o, PresentationProperty property)
        {
            return new PresentationPropertyObservable<object?, object?>(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)));
        }

        /// <summary>
        /// Gets an observable for an <see cref="PresentationProperty"/>.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>
        /// An observable which fires immediately with the current value of the property on the
        /// object and subsequently each time the property value changes.
        /// </returns>
        /// <remarks>
        /// The subscription to <paramref name="o"/> is created using a weak reference.
        /// </remarks>
        public static IObservable<T> GetObservable<T>(this PresentationObject o, PresentationProperty<T> property)
        {
            return new PresentationPropertyObservable<T, T>(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)));
        }

        /// <inheritdoc cref="GetObservable{T}(PresentationObject, PresentationProperty{T})"/>
        /// <typeparam name="TSource">The type of the values held by the <paramref name="property"/>.</typeparam>
        /// <typeparam name="TResult">The type of the value returned by the <paramref name="converter"/>.</typeparam>
        /// <param name="o"/>
        /// <param name="property"/>
        /// <param name="converter">A method which is executed to convert each property value to <typeparamref name="TResult"/>.</param>
        public static IObservable<TResult> GetObservable<TSource, TResult>(this PresentationObject o, PresentationProperty<TSource> property, Func<TSource, TResult> converter)
        {
            return new PresentationPropertyObservable<TSource, TResult>(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)),
                converter ?? throw new ArgumentNullException(nameof(converter)));
        }

        /// <inheritdoc cref="GetObservable{TSource,TResult}"/>
        public static IObservable<TResult> GetObservable<TResult>(this PresentationObject o, PresentationProperty property, Func<object?, TResult> converter)
        {
            return new PresentationPropertyObservable<object?, TResult>(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)),
                converter ?? throw new ArgumentNullException(nameof(converter)));
        }

        /// <summary>
        /// Gets an observable for an <see cref="PresentationProperty"/>.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>
        /// An observable which fires immediately with the current value of the property on the
        /// object and subsequently each time the property value changes.
        /// </returns>
        /// <remarks>
        /// The subscription to <paramref name="o"/> is created using a weak reference.
        /// </remarks>
        public static IObservable<BindingValue<object?>> GetBindingObservable(
            this PresentationObject o,
            PresentationProperty property)
        {
            return new PresentationPropertyBindingObservable<object?, object?>(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)));
        }

        /// <inheritdoc cref="GetObservable{TSource,TResult}"/>
        public static IObservable<BindingValue<TResult>> GetBindingObservable<TResult>(this PresentationObject o, PresentationProperty property, Func<object?, TResult> converter)
        {
            return new PresentationPropertyBindingObservable<object?, TResult>(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)),
                converter?? throw new ArgumentNullException(nameof(converter)));
        }

        /// <summary>
        /// Gets an observable for an <see cref="PresentationProperty"/>.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>
        /// An observable which fires immediately with the current value of the property on the
        /// object and subsequently each time the property value changes.
        /// </returns>
        /// <remarks>
        /// The subscription to <paramref name="o"/> is created using a weak reference.
        /// </remarks>
        public static IObservable<BindingValue<T>> GetBindingObservable<T>(
            this PresentationObject o,
            PresentationProperty<T> property)
        {
            return new PresentationPropertyBindingObservable<T, T>(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)));

        }

        /// <inheritdoc cref="GetBindingObservable{T}(PresentationObject, PresentationProperty{T})"/>
        /// <param name="o"/>
        /// <param name="property"/>
        /// <param name="converter">A method which is executed to convert each property value to <typeparamref name="TResult"/>.</param>
        public static IObservable<BindingValue<TResult>> GetBindingObservable<TSource, TResult>(
            this PresentationObject o,
            PresentationProperty<TSource> property,
            Func<TSource, TResult> converter)
        {
            return new PresentationPropertyBindingObservable<TSource, TResult>(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)),
                converter ?? throw new ArgumentNullException(nameof(converter)));
        }

        /// <summary>
        /// Gets an observable that listens for property changed events for an
        /// <see cref="PresentationProperty"/>.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>
        /// An observable which when subscribed pushes the property changed event args
        /// each time a <see cref="PresentationObject.PropertyChanged"/> event is raised
        /// for the specified property.
        /// </returns>
        public static IObservable<PresentationPropertyChangedEventArgs> GetPropertyChangedObservable(
            this PresentationObject o,
            PresentationProperty property)
        {
            return new PresentationPropertyChangedObservable(
                o ?? throw new ArgumentNullException(nameof(o)),
                property ?? throw new ArgumentNullException(nameof(property)));
        }

        /// <summary>
        /// Binds an <see cref="PresentationProperty"/> to an observable.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public static IDisposable Bind<T>(
            this PresentationObject target,
            PresentationProperty<T> property,
            IObservable<BindingValue<T>> source,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            property = property ?? throw new ArgumentNullException(nameof(property));
            source = source ?? throw new ArgumentNullException(nameof(source));

            return property switch
            {
                StyledProperty<T> styled => target.Bind(styled, source, priority),
                DirectPropertyBase<T> direct => target.Bind(direct, source),
                _ => throw new NotSupportedException("Unsupported PresentationProperty type."),
            };
        }

        /// <summary>
        /// Binds an <see cref="PresentationProperty"/> to an observable.
        /// </summary>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public static IDisposable Bind<T>(
            this PresentationObject target,
            PresentationProperty<T> property,
            IObservable<T> source,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            return property switch
            {
                StyledProperty<T> styled => target.Bind(styled, source, priority),
                DirectPropertyBase<T> direct => target.Bind(direct, source),
                _ => throw new NotSupportedException("Unsupported PresentationProperty type."),
            };
        }

        /// <summary>
        /// Gets a <see cref="PresentationProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        public static T GetValue<T>(this PresentationObject target, PresentationProperty<T> property)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            property = property ?? throw new ArgumentNullException(nameof(property));

            return property switch
            {
                StyledProperty<T> styled => target.GetValue(styled),
                DirectPropertyBase<T> direct => target.GetValue(direct),
                _ => throw new NotSupportedException("Unsupported PresentationProperty type.")
            };
        }

        /// <summary>
        /// Gets an <see cref="PresentationProperty"/> base value.
        /// </summary>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        /// <remarks>
        /// For styled properties, gets the value of the property excluding animated values, otherwise
        /// <see cref="PresentationProperty.UnsetValue"/>. Note that this method does not return
        /// property values that come from inherited or default values.
        /// 
        /// For direct properties returns the current value of the property.
        /// </remarks>
        public static object? GetBaseValue(
            this PresentationObject target,
            PresentationProperty property)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            property = property ?? throw new ArgumentNullException(nameof(property));

            return property.RouteGetBaseValue(target);
        }

        /// <summary>
        /// Gets an <see cref="PresentationProperty"/> base value.
        /// </summary>
        /// <param name="target">The object.</param>
        /// <param name="property">The property.</param>
        /// <remarks>
        /// For styled properties, gets the value of the property excluding animated values, otherwise
        /// <see cref="Optional{T}.Empty"/>. Note that this method does not return property values
        /// that come from inherited or default values.
        /// 
        /// For direct properties returns the current value of the property.
        /// </remarks>
        public static Optional<T> GetBaseValue<T>(
            this PresentationObject target,
            PresentationProperty<T> property)
        {
            target = target ?? throw new ArgumentNullException(nameof(target));
            property = property ?? throw new ArgumentNullException(nameof(property));

            return property switch
            {
                StyledProperty<T> styled => target.GetBaseValue(styled),
                DirectPropertyBase<T> direct => target.GetValue(direct),
                _ => throw new NotSupportedException("Unsupported PresentationProperty type.")
            };
        }

        /// <summary>
        /// Subscribes to a property changed notifications for changes that originate from a
        /// <typeparamref name="TTarget"/>.
        /// </summary>
        /// <typeparam name="TTarget">The type of the property change sender.</typeparam>
        /// <param name="observable">The property changed observable.</param>
        /// <param name="action">
        /// The method to call. The parameters are the sender and the event args.
        /// </param>
        /// <returns>A disposable that can be used to terminate the subscription.</returns>
        public static IDisposable AddClassHandler<TTarget>(
            this IObservable<PresentationPropertyChangedEventArgs> observable,
            Action<TTarget, PresentationPropertyChangedEventArgs> action)
            where TTarget : PresentationObject
        {
            return observable.Subscribe(new ClassHandlerObserver<TTarget>(action));
        }

        /// <summary>
        /// Subscribes to a property changed notifications for changes that originate from a
        /// <typeparamref name="TTarget"/>.
        /// </summary>
        /// <typeparam name="TTarget">The type of the property change sender.</typeparam>
        /// <typeparam name="TValue">The type of the property.</typeparam>
        /// <param name="observable">The property changed observable.</param>
        /// <param name="action">
        /// The method to call. The parameters are the sender and the event args.
        /// </param>
        /// <returns>A disposable that can be used to terminate the subscription.</returns>
        public static IDisposable AddClassHandler<TTarget, TValue>(
            this IObservable<PresentationPropertyChangedEventArgs<TValue>> observable,
            Action<TTarget, PresentationPropertyChangedEventArgs<TValue>> action) where TTarget : PresentationObject
        {
            return observable.Subscribe(new ClassHandlerObserver<TTarget, TValue>(action));
        }

        private class BindingAdaptor : BindingBase
        {
            private readonly IObservable<object?> _source;

            public BindingAdaptor(IObservable<object?> source)
            {
                this._source = source;
            }

            internal override BindingExpressionBase CreateInstance(
                PresentationObject target,
                PresentationProperty? property,
                object? anchor)
            {
                return new UntypedObservableBindingExpression(_source, BindingPriority.LocalValue);
            }
        }

        private class ClassHandlerObserver<TTarget, TValue> : IObserver<PresentationPropertyChangedEventArgs<TValue>>
        {
            private readonly Action<TTarget, PresentationPropertyChangedEventArgs<TValue>> _action;

            public ClassHandlerObserver(Action<TTarget, PresentationPropertyChangedEventArgs<TValue>> action)
            {
                _action = action;
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(PresentationPropertyChangedEventArgs<TValue> value)
            {
                if (value.Sender is TTarget target)
                {
                    _action(target, value);
                }
            }
        }

        private class ClassHandlerObserver<TTarget> : IObserver<PresentationPropertyChangedEventArgs>
        {
            private readonly Action<TTarget, PresentationPropertyChangedEventArgs> _action;

            public ClassHandlerObserver(Action<TTarget, PresentationPropertyChangedEventArgs> action)
            {
                _action = action;
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(PresentationPropertyChangedEventArgs value)
            {
                if (value.Sender is TTarget target)
                {
                    _action(target, value);
                }
            }
        }
    }
}
