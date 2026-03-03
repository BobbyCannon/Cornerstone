#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Cornerstone.Presentation;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Runtime;

/// <summary>
/// Dependency provider for configured services and / or instances of types.
/// </summary>
/// <remarks>
/// https://stackoverflow.com/questions/38138100/addtransient-addscoped-and-addsingleton-services-differences
/// </remarks>
[SourceReflection]
public class DependencyProvider : IDependencyProvider
{
	#region Fields

	private bool _locked;
	private readonly ThreadLocal<HashSet<Type>> _resolving;

	#endregion

	#region Constructors

	public DependencyProvider(string name)
	{
		_resolving = new(() => []);

		Name = name;
		Factories = new();
		SingletonInstances = new ConcurrentDictionary<Type, bool>();
	}

	#endregion

	#region Properties

	public string Name { get; }

	protected ConcurrentDictionary<Type, bool> SingletonInstances { get; }

	private ConcurrentDictionary<Type, TypeActivator> Factories { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Add a singleton of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type of the implementation. </typeparam>
	/// <param name="value"> The value of the type. </param>
	public void AddSingleton<T>(T value)
	{
		AddSingleton(() => value);
	}

	/// <summary>
	/// Add a singleton of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type to be added. </typeparam>
	/// <param name="create"> The function to create the type. </param>
	public void AddSingleton<T>(Func<T> create)
	{
		AddSingleton<T, T>(create);
	}

	/// <summary>
	/// Add a singleton of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type of the implementation. </typeparam>
	public void AddSingleton<T>(Action<T> initialize = null)
	{
		AddSingleton<T, T>(() => CreateInstanceForDependencyInjection(initialize));
	}

	/// <summary>
	/// Add a singleton of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type (interface/abstract) to add. </typeparam>
	/// <typeparam name="T2"> The type of the implementation. </typeparam>
	/// <param name="create"> The function to create the type. </param>
	public void AddSingleton<T, T2>(Func<T2> create) where T2 : T
	{
		var types = new List<Type> { typeof(T) };

		if (typeof(T) != typeof(T2))
		{
			types.Add(typeof(T2));
		}

		FactoriesGetOrAdd(
			types.ToArray(),
			new TypeActivator<T2>(_ => create())
			{
				ForDependencyInjection = true,
				Lifetime = TypeLifetime.Singleton
			}
		);
	}

	/// <summary>
	/// Add a singleton of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type (interface/abstract) to add. </typeparam>
	/// <typeparam name="T2"> The type of the implementation. </typeparam>
	public void AddSingleton<T, T2>(Action<T2> initialize = null) where T2 : T
	{
		AddSingleton<T, T2>(() => CreateInstanceForDependencyInjection(initialize));
	}

	/// <summary>
	/// Add a transient of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type (interface/abstract) to add. </typeparam>
	/// <param name="create"> The function to create the type. </param>
	public void AddTransient<T>(Func<T> create)
	{
		AddTransient<T, T>(create);
	}

	/// <summary>
	/// Add a transient of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type of the implementation. </typeparam>
	public void AddTransient<T>(Action<T> initialize = null)
	{
		AddTransient<T, T>(() => CreateInstanceForDependencyInjection(initialize));
	}

	/// <summary>
	/// Add a transient of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type (interface/abstract) to add. </typeparam>
	/// <typeparam name="T2"> The type of the implementation. </typeparam>
	public void AddTransient<T, T2>() where T2 : T
	{
		AddTransient<T, T2>(() => CreateInstanceForDependencyInjection<T2>());
	}

	/// <summary>
	/// Add a transient of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type (interface/abstract) to add. </typeparam>
	/// <typeparam name="T2"> The type of the implementation. </typeparam>
	/// <param name="create"> The function to create the type. </param>
	public void AddTransient<T, T2>(Func<T2> create) where T2 : T
	{
		var types = new List<Type> { typeof(T) };

		if (typeof(T) != typeof(T2))
		{
			types.Add(typeof(T2));
		}

		FactoriesGetOrAdd(
			types.ToArray(),
			new TypeActivator<T2>(_ => create.Invoke())
			{
				ForDependencyInjection = true,
				Lifetime = TypeLifetime.Transient
			}
		);
	}

	public T CreateNewInstance<T>()
	{
		return (T) CreateInstanceForDependencyInjection(typeof(T));
	}

	public object CreateNewInstance(Type type)
	{
		return CreateInstanceForDependencyInjection(type);
	}

	/// <summary>
	/// Just a check to ensure that an object is only instantiated once.
	/// </summary>
	public void ExpectSingleton(object value)
	{
		var typeOfT = value.GetType();

		if (SingletonInstances.TryAdd(typeOfT, true))
		{
			return;
		}

		Debugger.Break();
		throw new ConstraintException("This can only be instantiated once.");
	}

	/// <summary>
	/// Get an instance of the object. This instance is limited to the configured
	/// Singleton / Transient services.
	/// </summary>
	/// <typeparam name="T"> The type to get. </typeparam>
	/// <returns> The type from the configured types. </returns>
	public T GetInstance<T>()
	{
		var type = typeof(T);
		return (T) GetInstance(type);
	}

	/// <summary>
	/// Get an instance of the object. This instance is limited to the configured
	/// Singleton / Transient services.
	/// </summary>
	/// <returns> The type from the configured types. </returns>
	public object GetInstance(Type type)
	{
		if (type == null)
		{
			return null!;
		}

		// todo: this is not working with property injection
		if (!Factories.TryGetValue(type, out var activator))
		{
			return CreateInstanceForDependencyInjection(type);
		}

		var resolving = _resolving.Value!;
		if (!resolving.Add(type))
		{
			throw new InvalidOperationException($"Circular dependency detected:{Environment.NewLine}\t{string.Join($"{Environment.NewLine}\t", resolving.Append(type))}");
		}

		try
		{
			return activator.GetOrCreateInstance();
		}
		finally
		{
			resolving.Remove(type);
		}
	}

	/// <summary>
	/// Lock the injector where no other types can be configured.
	/// </summary>
	public void Lock()
	{
		_locked = true;
	}

	public void Reset()
	{
		Factories.Clear();
	}

	/// <summary>
	/// Add a singleton of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type of the implementation. </typeparam>
	/// <param name="value"> The value of the type. </param>
	public void SetSingleton<T>(T value)
	{
		SetSingleton(() => value);
	}

	/// <summary>
	/// Set a singleton of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type to be set. </typeparam>
	/// <param name="create"> The function to create the type. </param>
	public void SetSingleton<T>(Func<T> create)
	{
		SetSingleton<T, T>(create);
	}

	/// <summary>
	/// Set a singleton of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type (interface/abstract) to set. </typeparam>
	/// <typeparam name="T2"> The type of the implementation. </typeparam>
	/// <param name="create"> The function to create the type. </param>
	public void SetSingleton<T, T2>(Func<T2> create) where T2 : T
	{
		FactoriesSet(typeof(T),
			new TypeActivator<T2>(_ => create())
			{
				ForDependencyInjection = true,
				Lifetime = TypeLifetime.Singleton
			}
		);
	}

	/// <summary>
	/// Set a singleton of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type (interface/abstract) to add. </typeparam>
	/// <typeparam name="T2"> The type of the implementation. </typeparam>
	public void SetSingleton<T, T2>(Action<T2> initialize = null) where T2 : T
	{
		var value = () => CreateInstanceForDependencyInjection(initialize);
		SetSingleton<T, T2>(value);
		SetSingleton(value);
	}

	/// <summary>
	/// Set a transient of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type (interface/abstract) to set. </typeparam>
	/// <typeparam name="T2"> The type of the implementation. </typeparam>
	public void SetTransient<T, T2>() where T2 : T
	{
		SetTransient<T, T2>(() => CreateInstanceForDependencyInjection<T2>());
	}

	/// <summary>
	/// Set a transient of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type (interface/abstract) to set. </typeparam>
	/// <typeparam name="T2"> The type of the implementation. </typeparam>
	/// <param name="create"> The function to create the type. </param>
	public void SetTransient<T, T2>(Func<T2> create) where T2 : T
	{
		FactoriesSet(typeof(T),
			new TypeActivator<T2>(_ => create.Invoke())
			{
				ForDependencyInjection = true,
				Lifetime = TypeLifetime.Transient
			}
		);
	}

	/// <summary>
	/// Setup the default Cornerstone services.
	/// </summary>
	public IDependencyProvider SetupCornerstoneDependencies(
		IDateTimeProvider dateTimeProvider = null,
		IRuntimeInformation runtimeInformation = null,
		IDispatcher dispatcher = null)
	{
		dateTimeProvider ??= DateTimeProvider.RealTime;
		runtimeInformation ??= new RuntimeInformation();

		AddSingleton(dateTimeProvider);
		AddSingleton(dispatcher);
		AddSingleton(runtimeInformation);
		AddSingleton<IRuntimeInformation, RuntimeInformation>();
		AddSingleton(this);
		AddSingleton<IDependencyProvider, DependencyProvider>();

		return this;
	}

	private T CreateInstanceForDependencyInjection<T>(Action<T> initialize = null)
	{
		return (T) CreateInstanceForDependencyInjection(typeof(T), x => initialize?.Invoke((T) x));
	}

	private object CreateInstanceForDependencyInjection(Type type, Action<object> initialize = null)
	{
		// locate constructors
		var typeInfo = SourceReflector.GetSourceType(type);
		if (typeInfo == null)
		{
			throw new CornerstoneException(string.Format(Babel.Tower[BabelKeys.SourceReflectionTypeNotDefined], type.FullName));
		}

		var constructors = typeInfo.DeclaredConstructors;
		var primaryConstructor = constructors.FirstOrDefault(x => x.IsDependencyConstructor);
		if (primaryConstructor != null)
		{
			// If a primary constructor is found, use it.
			var primaryArguments = primaryConstructor
				.Parameters.Select(x => GetInstance(x.ParameterType)).ToArray();

			var primaryResponse = primaryConstructor.Invoke(primaryArguments);
			initialize?.Invoke(primaryResponse);
			InjectProperties(typeInfo, primaryResponse);
			return primaryResponse;
		}

		var emptyConstructor = constructors.FirstOrDefault(x => x.Parameters.Length <= 0);
		if (emptyConstructor != null)
		{
			// If an empty constructor is found, use it.
			var defaultResponse = emptyConstructor.Invoke(null);
			initialize?.Invoke(defaultResponse);
			InjectProperties(typeInfo, defaultResponse);
			return defaultResponse;
		}

		var availableConstructor = constructors.Where(x =>
		{
			var missingParameters = x
				.Parameters.Where(p => !Factories.Keys.Contains(p.ParameterType!)).ToList();

			if (missingParameters.Count > 0)
			{
				// How can we make this less configurable, more automatic...
				Debugger.Break();
				return false;
			}
			return true;
		}).ToList();

		if (availableConstructor.Count != 1)
		{
			Debugger.Break();
			throw new DependencyInjectorConstructorException(
				availableConstructor.Count == 0
					? $"An injectable constructor could not be found for {type.FullName}."
					: "Too many injectable constructor was found.",
				type.FullName
			);
		}

		var constructor = availableConstructor[0];
		var arguments = constructor
			.Parameters.Select(x => GetInstance(x.ParameterType)).ToArray();

		var response = constructor.Invoke(arguments);
		initialize?.Invoke(response);
		InjectProperties(typeInfo, response);
		return response;
	}

	private void FactoriesGetOrAdd(Type[] types, TypeActivator typeActivator)
	{
		if (_locked)
		{
			throw new InvalidOperationException(Babel.Tower[BabelKeys.DependencyProviderLocked]);
		}

		var activator =
			(typeActivator.Type != null)

			// See if we have an existing factory of the given type
			&& Factories.TryGetValue(typeActivator.Type, out var foundActivator)
			&& (foundActivator.Lifetime == typeActivator.Lifetime)
				? foundActivator
				: typeActivator;

		foreach (var type in types)
		{
			Factories.GetOrAdd(type, activator);
		}
	}

	private void FactoriesSet(Type type, TypeActivator typeActivator)
	{
		if (_locked)
		{
			throw new InvalidOperationException(Babel.Tower[BabelKeys.DependencyProviderLocked]);
		}

		Factories.AddOrUpdate(type, typeActivator, (_, _) => typeActivator);
	}

	private void InjectProperties(SourceTypeInfo typeInfo, object response)
	{
		var propertiesToInject = typeInfo
			.GetProperties().Where(x => x.IsDependencyInjected).ToList();

		foreach (var property in propertiesToInject)
		{
			var value = GetInstance(property.PropertyInfo.PropertyType);
			if (property.CanWrite)
			{
				property.PropertyInfo.SetValue(response, value);
			}
		}
	}

	#endregion
}

public interface IDependencyProvider
{
	#region Methods

	/// <summary>
	/// Create a new instance of the object no matter the configuration.
	/// </summary>
	/// <typeparam name="T"> The type to get. </typeparam>
	/// <returns> The type from the configured types. </returns>
	T CreateNewInstance<T>();

	/// <summary>
	/// Create a new instance of the object no matter the configuration.
	/// </summary>
	/// <param name="type"> The type to get. </param>
	/// <returns> The type from the configured types. </returns>
	object CreateNewInstance(Type type);

	/// <summary>
	/// Mark an item as expected single instance.
	/// </summary>
	/// <param name="value"> The singleton instance. </param>
	void ExpectSingleton(object value);

	/// <summary>
	/// Get an instance of the object. This instance is limited to the configured
	/// Singleton / Transient services.
	/// </summary>
	/// <typeparam name="T"> The type to get. </typeparam>
	/// <returns> The type from the configured types. </returns>
	T GetInstance<T>();

	/// <summary>
	/// Get an instance of the object. This instance is limited to the configured
	/// Singleton / Transient services.
	/// </summary>
	/// <param name="type"> The type to get. </param>
	/// <returns> The type from the configured types. </returns>
	object GetInstance(Type type);

	#endregion
}