#region References

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using Cornerstone.Data.TypeActivators;
using Cornerstone.Exceptions;
using Cornerstone.Extensions;
using Cornerstone.FileSystem;
using Cornerstone.Input;
using Cornerstone.Media;
using Cornerstone.Presentation;
using Cornerstone.Runtime;
using Cornerstone.Security;
using Cornerstone.Security.SecurityKeys;
using Cornerstone.Web;

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
	#region Fields

	private bool _locked;

	#endregion

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
	/// Add or update a transient of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type (interface/abstract) to add or update. </typeparam>
	/// <typeparam name="T2"> The type of the implementation. </typeparam>
	public void AddOrUpdateTransient<T, T2>() where T2 : T
	{
		AddOrUpdateTransient<T, T2>(() => CreateInstanceForDependencyInjection<T2>());
	}

	/// <summary>
	/// Add a transient of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type (interface/abstract) to add. </typeparam>
	/// <typeparam name="T2"> The type of the implementation. </typeparam>
	/// <param name="create"> The function to create the type. </param>
	public void AddOrUpdateTransient<T, T2>(Func<T2> create) where T2 : T
	{
		FactoriesAddOrUpdate(typeof(T),
			new TypeActivator<T2>(_ => create.Invoke())
			{
				ForDependencyInjection = true,
				Lifetime = TypeLifetime.Transient
			}
		);
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
	public void AddSingleton<T, T2>(Action<T2> initialize = null) where T2 : T
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
	public void AddSingleton<T, T2>(T2 value) where T2 : T
	{
		AddSingleton<T, T2>(() => value);
	}

	/// <summary>
	/// Add a singleton of the provided type.
	/// </summary>
	/// <typeparam name="T"> The type (interface/abstract) to add. </typeparam>
	/// <typeparam name="T2"> The type of the implementation. </typeparam>
	/// <param name="create"> The function to create the type. </param>
	public void AddSingleton<T, T2>(Func<T2> create) where T2 : T
	{
		FactoriesGetOrAdd(typeof(T),
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
	public void AddSingleton<T, T2>(Func<T2, T> create) where T2 : T
	{
		FactoriesGetOrAdd(typeof(T),
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
	public void AddTransient<T, T2>() where T2 : T
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
	public void AddTransient<T, T2>(Func<T2> create) where T2 : T
	{
		FactoriesGetOrAdd(typeof(T),
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
		FactoriesGetOrAdd(type,
			new TypeActivator<object>(_ => create.Invoke())
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
			? activator.GetOrCreateInstance()
			: CreateInstanceForDependencyInjection(type);
	}

	/// <summary>
	/// Import the factories from another set of dependency injectors.
	/// </summary>
	/// <param name="dependencyProvider"> The dependencies settings to import. </param>
	public void Import(DependencyProvider dependencyProvider)
	{
		foreach (var kp in dependencyProvider.Factories)
		{
			FactoriesAddOrUpdate(kp.Key, kp.Value);
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
	/// Setup the default Cornerstone services.
	/// </summary>
	public IDependencyProvider SetupCornerstoneServices(
		IDateTimeProvider dateTimeProvider = null,
		IRuntimeInformation runtimeInformation = null,
		IDispatcher dispatcher = null)
	{
		AddSingleton(dateTimeProvider ?? DateTimeProvider.RealTime);
		AddSingleton(dispatcher);
		AddSingleton(runtimeInformation ?? new RuntimeInformation());
		AddSingleton<IRuntimeInformation, RuntimeInformation>();
		AddSingleton(this);
		AddSingleton<IDependencyProvider, DependencyProvider>();
		AddSingleton<IPopupManager, PopupManager>();

		// Add stub placeholders
		AddSingleton<AudioPlayer, AudioPlayerStub>();
		AddSingleton<IBrowserInterop, BrowserInteropStub>();
		AddSingleton<IClipboardService, ClipboardServiceStub>();
		AddSingleton<FileService>();
		AddSingleton<Gamepad, GamepadStub>();
		AddSingleton<Keyboard, KeyboardStub>();
		AddSingleton<Mouse, MouseStub>();
		AddSingleton<IPermissions, Permissions>();
		AddSingleton<PlatformCredentialVault, PlatformCredentialVaultStub>();
		AddSingleton<SmartCardReader, SmartCardReaderStub>();
		AddSingleton<IWindowsHelloService, WindowsHelloServiceStub>();

		return this;
	}

	private T CreateInstanceForDependencyInjection<T>(Action<T> initialize = null)
	{
		return (T) CreateInstanceForDependencyInjection(typeof(T), x => initialize?.Invoke((T) x));
	}

	private object CreateInstanceForDependencyInjection(Type type, Action<object> initialize = null)
	{
		// locate constructors
		var constructors = type.GetCachedConstructors();
		var primaryConstructor = type.GetDependencyInjectionConstructor(constructors);
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
			var response2 = emptyConstructor.Invoke(null);
			initialize?.Invoke(response2);
			return response2;
		}

		var availableConstructor = constructors
			.Where(x =>
			{
				var parameters = x.GetParameters();
				var missingParameters = parameters.Where(p => !Factories.Keys.Contains(p.ParameterType)).ToList();
				if (missingParameters.Count > 0)
				{
					// How can we make this less configurable, more automatic...
					Debugger.Break();
					return false;
				}
				return true;
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

	private void FactoriesAddOrUpdate(Type type, TypeActivator typeActivator)
	{
		if (_locked)
		{
			throw new InvalidOperationException(Babel.Tower[BabelKeys.DependencyProviderLocked]);
		}

		var activator =
			// See if we have an existing factory of the given type
			Factories.TryGetValue(typeActivator.Type, out var foundActivator)
			&& (foundActivator.Lifetime == typeActivator.Lifetime)
				? foundActivator
				: typeActivator;

		Factories.AddOrUpdate(type, activator);
	}

	private void FactoriesGetOrAdd(Type type, TypeActivator typeActivator)
	{
		if (_locked)
		{
			throw new InvalidOperationException(Babel.Tower[BabelKeys.DependencyProviderLocked]);
		}

		var activator =
			// See if we have an existing factory of the given type
			Factories.TryGetValue(typeActivator.Type, out var foundActivator)
			&& (foundActivator.Lifetime == typeActivator.Lifetime)
				? foundActivator
				: typeActivator;

		Factories.GetOrAdd(type, activator);
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