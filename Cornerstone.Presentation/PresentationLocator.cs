using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Metadata;

#pragma warning disable CS1591 // Enable me later

namespace Cornerstone.Presentation
{
    [PrivateApi]
    public class PresentationLocator : ICornerstoneDependencyResolver
    {
        private readonly ICornerstoneDependencyResolver? _parentScope;
        public static ICornerstoneDependencyResolver Current { get; set; }
        public static PresentationLocator CurrentMutable { get; set; }
        private readonly Dictionary<Type, Func<object?>> _registry = new Dictionary<Type, Func<object?>>();

        static PresentationLocator()
        {
            Current = CurrentMutable = new PresentationLocator();
        }

        public PresentationLocator()
        {
            
        }

        public PresentationLocator(ICornerstoneDependencyResolver parentScope)
        {
            _parentScope = parentScope;
        }

        public object? GetService(Type t)
        {
            return _registry.TryGetValue(t, out var rv) ? rv() : _parentScope?.GetService(t);
        }

        public class RegistrationHelper<TService>
        {
            private readonly PresentationLocator _locator;

            public RegistrationHelper(PresentationLocator locator)
            {
                _locator = locator;
            }

            public PresentationLocator ToConstant<TImpl>(TImpl constant) where TImpl : TService
            {
                _locator._registry[typeof (TService)] = () => constant;
                return _locator;
            }

            public PresentationLocator ToFunc<TImlp>(Func<TImlp> func) where TImlp : TService
            {
                _locator._registry[typeof (TService)] = () => func();
                return _locator;
            }

            public PresentationLocator ToLazy<TImlp>(Func<TImlp> func) where TImlp : TService
            {
                var constructed = false;
                TImlp? instance = default;
                _locator._registry[typeof (TService)] = () =>
                {
                    if (!constructed)
                    {
                        instance = func();
                        constructed = true;
                    }

                    return instance;
                };
                return _locator;
            }
            
            public PresentationLocator ToSingleton<TImpl>() where TImpl : class, TService, new()
            {
                TImpl? instance = null;
                return ToFunc(() => instance ?? (instance = new TImpl()));
            }

            public PresentationLocator ToTransient<TImpl>() where TImpl : class, TService, new() => ToFunc(() => new TImpl());
        }

        public RegistrationHelper<T> Bind<T>() => new RegistrationHelper<T>(this);


        public PresentationLocator BindToSelf<T>(T constant)
            => Bind<T>().ToConstant(constant);

        public PresentationLocator BindToSelfSingleton<T>() where T : class, new() => Bind<T>().ToSingleton<T>();

        class ResolverDisposable : IDisposable
        {
            private readonly ICornerstoneDependencyResolver _resolver;
            private readonly PresentationLocator _mutable;

            public ResolverDisposable(ICornerstoneDependencyResolver resolver, PresentationLocator mutable)
            {
                _resolver = resolver;
                _mutable = mutable;
            }

            public void Dispose()
            {
                Current = _resolver;
                CurrentMutable = _mutable;
            }
        }


        public static IDisposable EnterScope()
        {
            var d = new ResolverDisposable(Current, CurrentMutable);
            Current = CurrentMutable =  new PresentationLocator(Current);
            return d;
        }
    }

    [PrivateApi]
    public interface ICornerstoneDependencyResolver
    {
        object? GetService(Type t);
    }

    [PrivateApi]
    public static class LocatorExtensions
    {
        public static T? GetService<T>(this ICornerstoneDependencyResolver resolver)
        {
            return (T?) resolver.GetService(typeof (T));
        }

        public static object GetRequiredService(this ICornerstoneDependencyResolver resolver, Type t)
        {
            return resolver.GetService(t) ?? throw new InvalidOperationException($"Unable to locate '{t}'.");
        }

        public static T GetRequiredService<T>(this ICornerstoneDependencyResolver resolver)
        {
            return (T?)resolver.GetService(typeof(T)) ?? throw new InvalidOperationException($"Unable to locate '{typeof(T)}'.");
        }
    }
}

