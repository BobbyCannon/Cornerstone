#region References

using System;
using System.Collections.Generic;

#endregion

namespace Cornerstone.Internal;

public interface IServiceContainer : IServiceProvider
{
	#region Methods

	void AddService(Type serviceType, object serviceInstance);

	void RemoveService(Type serviceType);

	#endregion
}

public static class ServiceExtensions
{
	#region Methods

	public static void AddService<T>(this IServiceContainer container, T serviceInstance)
	{
		if (container == null)
		{
			throw new ArgumentNullException(nameof(container));
		}

		container.AddService(typeof(T), serviceInstance);
	}

	public static T GetService<T>(this IServiceProvider provider) where T : class
	{
		return provider.GetService(typeof(T)) as T;
	}

	public static void RemoveService<T>(this IServiceContainer container)
	{
		if (container == null)
		{
			throw new ArgumentNullException(nameof(container));
		}

		container.RemoveService(typeof(T));
	}

	#endregion
}

internal class ServiceContainer : IServiceContainer
{
	#region Fields

	private readonly Dictionary<Type, object> _services = new();

	#endregion

	#region Constructors

	public ServiceContainer()
	{
		_services.Add(typeof(IServiceProvider), this);
		_services.Add(typeof(IServiceContainer), this);
	}

	#endregion

	#region Methods

	public void AddService(Type serviceType, object serviceInstance)
	{
		_services[serviceType] = serviceInstance;
	}

	public object GetService(Type serviceType)
	{
		_services.TryGetValue(serviceType, out var service);
		return service;
	}

	public void RemoveService(Type serviceType)
	{
		_services.Remove(serviceType);
	}

	#endregion
}