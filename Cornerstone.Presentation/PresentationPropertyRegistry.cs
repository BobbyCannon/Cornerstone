using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Cornerstone.Presentation
{
    /// <summary>
    /// Tracks registered <see cref="PresentationProperty"/> instances.
    /// </summary>
    public class PresentationPropertyRegistry
    {
        private readonly Dictionary<int, PresentationProperty> _properties =
            new Dictionary<int, PresentationProperty>();
        private readonly Dictionary<Type, Dictionary<int, PresentationProperty>> _registered =
            new Dictionary<Type, Dictionary<int, PresentationProperty>>();
        private readonly Dictionary<Type, Dictionary<int, PresentationProperty>> _attached =
            new Dictionary<Type, Dictionary<int, PresentationProperty>>();
        private readonly Dictionary<Type, Dictionary<int, PresentationProperty>> _direct =
            new Dictionary<Type, Dictionary<int, PresentationProperty>>();
        private readonly Dictionary<Type, List<PresentationProperty>> _registeredCache =
            new Dictionary<Type, List<PresentationProperty>>();
        private readonly Dictionary<Type, List<PresentationProperty>> _attachedCache =
            new Dictionary<Type, List<PresentationProperty>>();
        private readonly Dictionary<Type, List<PresentationProperty>> _directCache =
            new Dictionary<Type, List<PresentationProperty>>();
        private readonly Dictionary<Type, List<PresentationProperty>> _inheritedCache =
            new Dictionary<Type, List<PresentationProperty>>();

        /// <summary>
        /// Gets the <see cref="PresentationPropertyRegistry"/> instance
        /// </summary>
        public static PresentationPropertyRegistry Instance { get; }
            = new PresentationPropertyRegistry();

        /// <summary>
        /// Gets a list of all registered properties.
        /// </summary>
        internal IReadOnlyCollection<PresentationProperty> Properties => _properties.Values;

        private object _unregisteringLocker = new object();
        /// <summary>
        /// Unregister all<see cref="PresentationProperty"/>s registered on types
        /// </summary>
        /// <param name="types"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public bool UnregisterByModule(IEnumerable<Type> types)
        {
            _ = types ?? throw new ArgumentNullException(nameof(types));

            lock (_unregisteringLocker)
            {
                try
                {
                    foreach (var type in types)
                    {
                        Unregister(_registered, type);
                        Unregister(_attached, type);
                        Unregister(_direct, type);
                        Unregister(_registeredCache,type);
                        Unregister(_attachedCache,type);
                        Unregister(_directCache,type);
                        Unregister(_inheritedCache,type);
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return true;
        }
        private void Unregister( Dictionary<Type, List<PresentationProperty>> dictionary,Type type)
        {
            dictionary.Remove(type);
        }
        private void Unregister( Dictionary<Type, Dictionary<int, PresentationProperty>> dictionary,Type type)
        {
            foreach (var keyValuePair in dictionary)
            {
                foreach (var key in keyValuePair.Value)
                {
                    key.Value.Unregister(type);
                }
            }
        }
        /// <summary>
        /// Gets all non-attached <see cref="PresentationProperty"/>s registered on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A collection of <see cref="PresentationProperty"/> definitions.</returns>
        [UnconditionalSuppressMessage("Trimming", "IL2059", Justification = "If type was trimmed out, no properties were referenced")]
        public IReadOnlyList<PresentationProperty> GetRegistered(Type type)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            if (_registeredCache.TryGetValue(type, out var result))
            {
                return result;
            }

            var t = type;
            result = new List<PresentationProperty>();
            lock (_unregisteringLocker)
            {
                while (t != null)
                {
                    // Ensure the type's static ctor has been run.
                    RuntimeHelpers.RunClassConstructor(t.TypeHandle);
                    if (_registered.TryGetValue(t, out var registered))
                    {
                        result.AddRange(registered.Values);
                    }
                    t = t.BaseType;
                }
                _registeredCache.Add(type, result);
            }
            return result;
        }

        /// <summary>
        /// Gets all attached <see cref="PresentationProperty"/>s registered on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A collection of <see cref="PresentationProperty"/> definitions.</returns>
        public IReadOnlyList<PresentationProperty> GetRegisteredAttached(Type type)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            if (_attachedCache.TryGetValue(type, out var result))
            {
                return result;
            }

            var t = type;
            result = new List<PresentationProperty>();
            lock (_unregisteringLocker)
            {
                while (t != null)
                {
                    if (_attached.TryGetValue(t, out var attached))
                    {
                        result.AddRange(attached.Values);
                    }
                    t = t.BaseType;
                }
                _attachedCache.Add(type, result);
            }
            return result;
        }

        /// <summary>
        /// Gets all direct <see cref="PresentationProperty"/>s registered on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A collection of <see cref="PresentationProperty"/> definitions.</returns>
        public IReadOnlyList<PresentationProperty> GetRegisteredDirect(Type type)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            if (_directCache.TryGetValue(type, out var result))
            {
                return result;
            }

            var t = type;
            result = new List<PresentationProperty>();
            lock (_unregisteringLocker)
            {
                while (t != null)
                {
                    if (_direct.TryGetValue(t, out var direct))
                    {
                        result.AddRange(direct.Values);
                    }
                    t = t.BaseType;
                }
                _directCache.Add(type, result);
            }
            return result;
        }

        /// <summary>
        /// Gets all inherited <see cref="PresentationProperty"/>s registered on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A collection of <see cref="PresentationProperty"/> definitions.</returns>
        public IReadOnlyList<PresentationProperty> GetRegisteredInherited(Type type)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            if (_inheritedCache.TryGetValue(type, out var result))
            {
                return result;
            }

            result = new List<PresentationProperty>();
            var visited = new HashSet<PresentationProperty>();

            var registered = GetRegistered(type);
            var registeredCount = registered.Count;

            for (var i = 0; i < registeredCount; i++)
            {
                var property = registered[i];

                if (property.Inherits)
                {
                    result.Add(property);
                    visited.Add(property);
                }
            }

            var registeredAttached = GetRegisteredAttached(type);
            var registeredAttachedCount = registeredAttached.Count;

            for (var i = 0; i < registeredAttachedCount; i++)
            {
                var property = registeredAttached[i];

                if (property.Inherits)
                {
                    if (!visited.Contains(property))
                    {
                        result.Add(property);
                    }
                }
            }

            lock (_unregisteringLocker)
            {
                _inheritedCache.Add(type, result);
            }
            
            return result;
        }

        /// <summary>
        /// Gets all <see cref="PresentationProperty"/>s registered on a object.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <returns>A collection of <see cref="PresentationProperty"/> definitions.</returns>
        public IReadOnlyList<PresentationProperty> GetRegistered(PresentationObject o)
        {
            _ = o ?? throw new ArgumentNullException(nameof(o));

            return GetRegistered(o.GetType());
        }

        /// <summary>
        /// Gets a direct property as registered on an object.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The direct property.</param>
        /// <returns>
        /// The registered.
        /// </returns>
        public DirectPropertyBase<T> GetRegisteredDirect<T>(
            PresentationObject o,
            DirectPropertyBase<T> property)
        {
            return (DirectPropertyBase<T>)GetRegisteredDirectUntyped(o, property);
        }

        internal PresentationProperty GetRegisteredDirectUntyped(PresentationObject o, PresentationProperty property)
        {
            return FindRegisteredDirectUntyped(o, property) ??
                   throw new ArgumentException($"Property '{property.Name} not registered on '{o.GetType()}");
        }

        /// <summary>
        /// Finds a registered property on a type by name.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="name">The property name.</param>
        /// <returns>
        /// The registered property or null if no matching property found.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The property name contains a '.'.
        /// </exception>
        public PresentationProperty? FindRegistered(Type type, string name)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            _ = name ?? throw new ArgumentNullException(nameof(name));

            if (name.Contains('.'))
            {
                throw new InvalidOperationException("Attached properties not supported.");
            }

            var registered = GetRegistered(type);
            var registeredCount = registered.Count;

            for (var i = 0; i < registeredCount; i++)
            {
                PresentationProperty x = registered[i];

                if (x.Name == name)
                {
                    return x;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds a registered property on an object by name.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="name">The property name.</param>
        /// <returns>
        /// The registered property or null if no matching property found.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The property name contains a '.'.
        /// </exception>
        public PresentationProperty? FindRegistered(PresentationObject o, string name)
        {
            _ = o ?? throw new ArgumentNullException(nameof(o));
            _ = name ?? throw new ArgumentNullException(nameof(name));

            return FindRegistered(o.GetType(), name);
        }

        /// <summary>
        /// Finds a direct property as registered on an object.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The direct property.</param>
        /// <returns>
        /// The registered property or null if no matching property found.
        /// </returns>
        public DirectPropertyBase<T>? FindRegisteredDirect<T>(
            PresentationObject o,
            DirectPropertyBase<T> property)
        {
            return (DirectPropertyBase<T>?)FindRegisteredDirectUntyped(o, property);
        }

        private PresentationProperty? FindRegisteredDirectUntyped(PresentationObject o, PresentationProperty property)
        {
            Debug.Assert(property.IsDirect);

            if (property.OwnerType == o.GetType())
            {
                return property;
            }

            var registeredDirect = GetRegisteredDirect(o.GetType());
            var registeredDirectCount = registeredDirect.Count;

            for (var i = 0; i < registeredDirectCount; i++)
            {
                var p = registeredDirect[i];

                if (p == property)
                {
                    return p;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds a registered property by Id.
        /// </summary>
        /// <param name="id">The property Id.</param>
        /// <returns>The registered property or null if no matching property found.</returns>
        internal PresentationProperty? FindRegistered(int id)
        {
            return id < _properties.Count ? _properties[id] : null;
        }

        /// <summary>
        /// Checks whether a <see cref="PresentationProperty"/> is registered on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is registered, otherwise false.</returns>
        public bool IsRegistered(Type type, PresentationProperty property)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            _ = property ?? throw new ArgumentNullException(nameof(property));

            static bool ContainsProperty(IReadOnlyList<PresentationProperty> properties, PresentationProperty property)
            {
                var propertiesCount = properties.Count;

                for (var i = 0; i < propertiesCount; i++)
                {
                    if (properties[i] == property)
                    {
                        return true;
                    }
                }

                return false;
            }

            return ContainsProperty(Instance.GetRegistered(type), property) ||
                   ContainsProperty(Instance.GetRegisteredAttached(type), property);
        }

        /// <summary>
        /// Checks whether a <see cref="PresentationProperty"/> is registered on a object.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is registered, otherwise false.</returns>
        public bool IsRegistered(object o, PresentationProperty property)
        {
            _ = o ?? throw new ArgumentNullException(nameof(o));
            _ = property ?? throw new ArgumentNullException(nameof(property));

            return IsRegistered(o.GetType(), property);
        }

        /// <summary>
        /// Registers a <see cref="PresentationProperty"/> on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="property">The property.</param>
        /// <remarks>
        /// You won't usually want to call this method directly, instead use the
        /// <see cref="PresentationProperty.Register{TOwner, TValue}(string, TValue, bool, Data.BindingMode, Func{TValue, bool}, Func{PresentationObject, TValue, TValue}, bool)"/>
        /// method.
        /// </remarks>
        public void Register(Type type, PresentationProperty property)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            _ = property ?? throw new ArgumentNullException(nameof(property));
            lock (_unregisteringLocker)
            {
                if (!_registered.TryGetValue(type, out var inner))
                {
                    inner = new Dictionary<int, PresentationProperty>();
                    inner.Add(property.Id, property);
                    _registered.Add(type, inner);
                }
                else if (!inner.ContainsKey(property.Id))
                {
                    inner.Add(property.Id, property);
                }

                if (property.IsDirect)
                {
                    if (!_direct.TryGetValue(type, out inner))
                    {
                        inner = new Dictionary<int, PresentationProperty>();
                        inner.Add(property.Id, property);
                        _direct.Add(type, inner);
                    }
                    else if (!inner.ContainsKey(property.Id))
                    {
                        inner.Add(property.Id, property);
                    }

                    _directCache.Clear();
                }

                if (!_properties.ContainsKey(property.Id))
                {
                    _properties.Add(property.Id, property);
                }
            
                _registeredCache.Clear();
                _inheritedCache.Clear();
            }
            
        }

        /// <summary>
        /// Registers an attached <see cref="PresentationProperty"/> on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="property">The property.</param>
        /// <remarks>
        /// You won't usually want to call this method directly, instead use the
        /// <see cref="PresentationProperty.RegisterAttached{THost, TValue}(string, Type, TValue, bool, Data.BindingMode, Func{TValue, bool}, Func{PresentationObject, TValue, TValue})"/>
        /// method.
        /// </remarks>
        public void RegisterAttached(Type type, PresentationProperty property)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            _ = property ?? throw new ArgumentNullException(nameof(property));
            
            if (!property.IsAttached)
            {
                throw new InvalidOperationException(
                    "Cannot register a non-attached property as attached.");
            }
            lock (_unregisteringLocker)
            {
                if (!_attached.TryGetValue(type, out var inner))
                {
                    inner = new Dictionary<int, PresentationProperty>();
                    inner.Add(property.Id, property);
                    _attached.Add(type, inner);
                }
                else
                {
                    inner.Add(property.Id, property);
                }
            
                _attachedCache.Clear();
                _inheritedCache.Clear();
            }
           
        }
    }
}
