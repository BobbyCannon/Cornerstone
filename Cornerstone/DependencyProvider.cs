#region References

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Cornerstone.Attributes;
using Cornerstone.Data.TypeActivators;
using Cornerstone.Exceptions;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone;

/// <summary>
/// Dependency provider for configured services and / or instances of types.
/// </summary>
/// <remarks>
/// https://stackoverflow.com/questions/38138100/addtransient-addscoped-and-addsingleton-services-differences
/// </remarks>
public class DependencyProvider : IDependencyProvider
{
	#region Constructors

	public DependencyProvider(string name)
	{
		Name = name;
		Factories = new();
	}

	#endregion

	#region Properties

	public string Name { get; }

	private ConcurrentDictionary<Type, TypeActivator> Factories { get; }

	#endregion

	#region Methods

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
	public void AddSingleton<T, T2>(Action<T2> initialize = null)
		where T2 : T
	{
		AddSingleton<T, T2>(() => CreateInstanceForDependencyInjection(initialize));
	}

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
	/// <typeparam name="T"> The type (interface/abstract) to add. </typeparam>
	/// <typeparam name="T2"> The type of the implementation. </typeparam>
	/// <param name="value"> The value of the type. </param>
	public void AddSingleton<T, T2>(T2 value)
	{
		AddSingleton<T, T2>(() => value);
	}

	/// <summary>
	/// Add a singleton of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type (interface/abstract) to add. </typeparam>
	/// <typeparam name="T2"> The type of the implementation. </typeparam>
	/// <param name="create"> The function to create the type. </param>
	public void AddSingleton<T, T2>(Func<T2> create)
	{
		Factories.GetOrAdd(typeof(T),
			new TypeActivator<T2>(_ => create())
			{
				ForDependencyInjection = true,
				Lifetime = TypeLifetime.Singleton
			}
		);
	}

	/// <summary>
	/// Add a provider for a singleton of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type (interface/abstract) to add. </typeparam>
	/// <typeparam name="T2"> The type of the provider. </typeparam>
	/// <param name="create"> The function to provide the type. </param>
	public void AddSingletonProvider<T, T2>(Func<T2, T> create)
	{
		Factories.GetOrAdd(typeof(T),
			new TypeActivator<T>(_ => create(GetInstance<T2>()))
			{
				ForDependencyInjection = true,
				Lifetime = TypeLifetime.Singleton
			}
		);
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
	public void AddTransient<T, T2>()
	{
		AddTransient<T, T2>(() => CreateInstanceForDependencyInjection<T2>());
	}

	/// <summary>
	/// Add a transient of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type of the implementation. </typeparam>
	/// <param name="value"> The value of the type. </param>
	public void AddTransient<T>(T value)
	{
		AddTransient<T, T>(() => value);
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
	/// <typeparam name="T"> The type (interface/abstract) to add. </typeparam>
	/// <typeparam name="T2"> The type of the implementation. </typeparam>
	/// <param name="create"> The function to create the type. </param>
	public void AddTransient<T, T2>(Func<T2> create)
	{
		Factories.GetOrAdd(typeof(T),
			new TypeActivator<T2>(_ => create.Invoke())
			{
				ForDependencyInjection = true,
				Lifetime = TypeLifetime.Transient
			}
		);
	}

	/// <summary>
	/// Add a transient of the provided type.
	/// </summary>
	public void AddTransientType(Type type, Action<object> initialize = null)
	{
		AddTransientType(type, () => CreateInstanceForDependencyInjection(type, initialize));
	}

	/// <summary>
	/// Add a transient of the provided type.
	/// </summary>
	/// <param name="type"> The type (interface/abstract) to add. </param>
	/// <param name="create"> The function to create the type. </param>
	public void AddTransientType(Type type, Func<object> create)
	{
		Factories.GetOrAdd(type,
			new TypeActivator(type, _ => create.Invoke())
			{
				ForDependencyInjection = true,
				Lifetime = TypeLifetime.Transient
			}
		);
	}

	/// <summary>
	/// Determine if a factory exists for the provided generic type.
	/// </summary>
	/// <typeparam name="T"> The type to check. </typeparam>
	/// <returns> True if the type has factory otherwise false. </returns>
	public bool Contains<T>()
	{
		return Factories.ContainsKey(typeof(T));
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
		return Factories.TryGetValue(type, out var activator)
			? activator.CreateInstanceObject()
			: CreateInstanceForDependencyInjection(type);
	}

	/// <summary>
	/// Import the factories from another set of dependency injectors.
	/// </summary>
	/// <param name="dependencyProvider"> The dependencies settings to import. </param>
	public void Import(DependencyProvider dependencyProvider)
	{
		Factories.Add(dependencyProvider.Factories);
	}

	/// <summary>
	/// Lock the injector where no other types can be configured.
	/// </summary>
	public void Lock()
	{
	}

	public void Reset()
	{
		Factories.Clear();
	}

	/// <summary>
	/// Setup the default Cornerstone services.
	/// </summary>
	public void SetupCornerstoneServices(
		IDateTimeProvider dateTimeProvider = null,
		IRuntimeInformation runtimeInformation = null,
		IDispatcher dispatcher = null,
		IWeakEventManager weakEventManager = null)
	{
		AddSingleton(dateTimeProvider ?? DateTimeProvider.RealTime);
		AddSingleton(dispatcher);
		AddSingleton(runtimeInformation ?? new RuntimeInformation());
		AddSingleton<IDependencyProvider>(this);
		AddSingleton(weakEventManager ?? new WeakEventManager());
	}

	private T CreateInstanceForDependencyInjection<T>(Action<T> initialize = null)
	{
		return (T) CreateInstanceForDependencyInjection(typeof(T), x => initialize?.Invoke((T) x));
	}

	private object CreateInstanceForDependencyInjection(Type type, Action<object> initialize = null)
	{
		// locate constructors
		var constructors = type.GetCachedConstructors();
		var primaryConstructor = constructors.FirstOrDefault(x => x.GetCustomAttribute<DependencyInjectionConstructorAttribute>() != null);
		if (primaryConstructor != null)
		{
			// If a primary constructor is found, use it.
			var primaryArguments = primaryConstructor
				.GetParameters()
				.Select(x => GetInstance(x.ParameterType))
				.ToArray();

			var response1 = primaryConstructor.Invoke(primaryArguments);
			initialize?.Invoke(response1);
			return response1;
		}

		var emptyConstructor = constructors.FirstOrDefault(x => x.GetParameters().Length <= 0);
		if (emptyConstructor != null)
		{
			// If an empty constructor is found, use it.
			return emptyConstructor.Invoke(null);
		}

		var availableConstructor = constructors
			.Where(x =>
			{
				var parameters = x.GetParameters();
				var parametersAvailable = parameters.Select(p => Factories.Keys.Contains(p.ParameterType));
				var allValid = parametersAvailable.All(p => p);
				if (!allValid)
				{
					// How can we make this less configurable, more automatic...
					Debugger.Break();
				}
				return allValid;
			})
			.ToList();

		if (availableConstructor.Count != 1)
		{
			Debugger.Break();
			throw new DependencyInjectorConstructorException(availableConstructor.Count == 0
					? $"An injectable constructor could not be found for {type?.FullName}."
					: "Too many injectable constructor was found.",
				type?.FullName ?? string.Empty
			);
		}

		var constructor = availableConstructor[0];
		var arguments = constructor
			.GetParameters()
			.Select(x => GetInstance(x.ParameterType))
			.ToArray();

		var response = constructor.Invoke(arguments);
		initialize?.Invoke(response);
		return response;
	}

	#endregion
}

public interface IDependencyProvider
{
	#region Methods

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
	/// <returns> The type from the configured types. </returns>
	object GetInstance(Type type);

	#endregion
}