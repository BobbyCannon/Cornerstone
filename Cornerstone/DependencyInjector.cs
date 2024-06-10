#region References

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Cornerstone.Attributes;
using Cornerstone.Data.TypeActivators;
using Cornerstone.Exceptions;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone;

/// <summary>
/// Dependency injection for configured services and / or instances of types.
/// </summary>
/// <remarks>
/// https://stackoverflow.com/questions/38138100/addtransient-addscoped-and-addsingleton-services-differences
/// </remarks>
public class DependencyInjector
{
	#region Fields

	private static readonly ConcurrentDictionary<Type, TypeActivator> _factories;

	#endregion

	#region Constructors

	static DependencyInjector()
	{
		// Dependency Injection configurations
		_factories = new();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Add a singleton of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type of the implementation. </typeparam>
	public void AddSingleton<T>()
	{
		AddSingleton<T, T>(CreateInstanceForDependencyInjection<T>);
	}

	/// <summary>
	/// Add a singleton of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type (interface/abstract) to add. </typeparam>
	/// <typeparam name="T2"> The type of the implementation. </typeparam>
	public void AddSingleton<T, T2>()
	{
		AddSingleton<T, T2>(CreateInstanceForDependencyInjection<T2>);
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
	/// <param name="create"> The function to create the type. </param>
	public void AddSingleton<T, T2>(Func<T2> create)
	{
		_factories.GetOrAdd(typeof(T),
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
		_factories.GetOrAdd(typeof(T),
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
	public void AddTransient<T>()
	{
		AddTransient<T, T>(CreateInstanceForDependencyInjection<T>);
	}

	/// <summary>
	/// Add a transient of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type (interface/abstract) to add. </typeparam>
	/// <typeparam name="T2"> The type of the implementation. </typeparam>
	public void AddTransient<T, T2>()
	{
		AddTransient<T, T2>(CreateInstanceForDependencyInjection<T2>);
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
	/// <typeparam name="T2"> The type of the implementation. </typeparam>
	/// <param name="create"> The function to create the type. </param>
	public void AddTransient<T, T2>(Func<T2> create)
	{
		_factories.GetOrAdd(typeof(T),
			new TypeActivator<T2>(_ => create.Invoke())
			{
				ForDependencyInjection = true,
				Lifetime = TypeLifetime.Transient
			}
		);
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
		if (_factories.TryGetValue(type, out var activator))
		{
			return activator.CreateInstanceObject();
		}

		return default;
	}

	/// <summary>
	/// Lock the injector where no other types can be configured.
	/// </summary>
	public void Lock()
	{
	}

	private T CreateInstanceForDependencyInjection<T>()
	{
		return (T) CreateInstanceForDependencyInjection(typeof(T));
	}

	private object CreateInstanceForDependencyInjection(Type type)
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

			return primaryConstructor.Invoke(primaryArguments);
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
				var allValid = parameters.All(p => _factories.Keys.Contains(p.ParameterType));
				return allValid;
			})
			.ToList();

		if (availableConstructor.Count != 1)
		{
			throw new DependencyInjectorConstructorException(availableConstructor.Count == 0
					? "An injectable constructor could not be found."
					: "Too many injectable constructor was found.",
				type?.FullName ?? string.Empty
			);
		}
		
		var constructor = availableConstructor[0];
		var arguments = constructor
			.GetParameters()
			.Select(x => GetInstance(x.ParameterType))
			.ToArray();

		return constructor.Invoke(arguments);
	}

	#endregion
}