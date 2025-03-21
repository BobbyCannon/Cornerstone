#region References

using System;
using System.Linq;
using System.Reflection;
using Cornerstone.Attributes;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone;

public static class DependencyProviderExtensions
{
	#region Methods

	public static ConstructorInfo GetDependencyInjectionConstructor(this Type type)
	{
		var constructors = type.GetCachedConstructors();
		return GetDependencyInjectionConstructor(type, constructors);
	}

	public static ConstructorInfo GetDependencyInjectionConstructor(this Type type, ConstructorInfo[] constructors)
	{
		var primaryConstructor = constructors.FirstOrDefault(x => x.GetCustomAttribute<DependencyInjectionConstructorAttribute>() != null);
		return primaryConstructor;
	}

	#endregion
}