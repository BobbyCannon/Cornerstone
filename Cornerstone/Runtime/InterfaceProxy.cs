#region References

using System;
using System.Collections.Generic;
using System.Reflection;

#endregion

namespace Cornerstone.Runtime;

public static class InterfaceProxyFactory
{
	#region Methods

	public static TInterface Create<TInterface>(Action<InterfaceProxy<TInterface>> configure = null)
		where TInterface : class
	{
		if (!typeof(TInterface).IsInterface)
		{
			throw new ArgumentException("TInterface must be an interface");
		}

		var proxy = new InterfaceProxy<TInterface>();
		configure?.Invoke(proxy);
		proxy.PopulateDefaultValues();
		return proxy.Instance;
	}

	#endregion
}

public class InterfaceProxy<TInterface> where TInterface : class
{
	#region Fields

	private readonly Dictionary<string, Func<object[], object>> _methodHandlers;
	private readonly Dictionary<string, object> _properties;

	#endregion

	#region Constructors

	public InterfaceProxy()
	{
		_methodHandlers = new();
		_properties = new();

		Instance = DispatchProxy.Create<TInterface, DispatchProxyWrapper>();

		((DispatchProxyWrapper) (object) Instance).SetHandler(this);
	}

	#endregion

	#region Properties

	public TInterface Instance { get; }

	#endregion

	#region Methods

	public void PopulateDefaultValues()
	{
		foreach (var property in typeof(TInterface).GetProperties())
		{
			if (!_properties.ContainsKey(property.Name))
			{
				_properties[property.Name] = GetDefaultValue(property.PropertyType);
			}
		}
	}

	public void SetMethodHandler(string methodName, Func<object[], object> handler)
	{
		_methodHandlers[methodName] = handler;
	}

	public void SetProperty(string propertyName, object value)
	{
		_properties[propertyName] = value;
	}

	internal object Dispatch(MethodInfo method, object[] args)
	{
		var methodName = method.Name;

		if (methodName.StartsWith("get_"))
		{
			var propertyName = methodName.Substring(4);
			return _properties.TryGetValue(propertyName, out var value)
				? System.Convert.ChangeType(value, method.ReturnType)
				: GetDefaultValue(method.ReturnType);
		}

		if (methodName.StartsWith("set_"))
		{
			var propertyName = methodName.Substring(4);
			_properties[propertyName] = args[0];
			return null;
		}

		if (_methodHandlers.TryGetValue(methodName, out var handler))
		{
			return handler(args);
		}

		throw new NotImplementedException($"Method {methodName} not implemented");
	}

	private static object GetDefaultValue(Type type)
	{
		return type.IsValueType ? type.CreateInstance() : null;
	}

	#endregion

	#region Classes

	public class DispatchProxyWrapper : DispatchProxy
	{
		#region Fields

		private InterfaceProxy<TInterface> _handler;

		#endregion

		#region Methods

		public void SetHandler(InterfaceProxy<TInterface> handler)
		{
			_handler = handler;
		}

		protected override object Invoke(MethodInfo targetMethodInfo, object[] args)
		{
			if (_handler == null)
			{
				throw new InvalidOperationException("Handler not set");
			}
			return _handler.Dispatch(targetMethodInfo, args);
		}

		#endregion
	}

	#endregion
}