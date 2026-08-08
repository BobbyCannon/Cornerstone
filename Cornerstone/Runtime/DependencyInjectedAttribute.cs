#region References

using System;

#endregion

namespace Cornerstone.Runtime;

/// <summary>
/// Marks a type for default dependency registration via source generation.
/// Registration uses factory-based <see cref="DependencyProvider" /> AddSingleton / AddTransient
/// only so tests can override with SetSingleton before first resolve.
/// </summary>
/// <remarks>
/// Do not use for instance-based registration. Eager instances break override semantics
/// once dependents have already resolved the original singleton.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class DependencyInjectedAttribute : CornerstoneAttribute
{
	#region Constructors

	/// <summary>
	/// Register the attributed type as itself with the given lifetime.
	/// </summary>
	/// <param name="lifetime"> Service lifetime. Defaults to <see cref="TypeLifetime.Singleton" />. </param>
	public DependencyInjectedAttribute(TypeLifetime lifetime = TypeLifetime.Singleton)
	{
		Lifetime = lifetime;
		ServiceType = null;
	}

	/// <summary>
	/// Register the attributed type as the implementation of <paramref name="serviceType" />.
	/// </summary>
	/// <param name="serviceType">
	/// Service type to register (interface or base type). Use the attributed type as the implementation.
	/// </param>
	/// <param name="lifetime"> Service lifetime. Defaults to <see cref="TypeLifetime.Singleton" />. </param>
	public DependencyInjectedAttribute(Type serviceType, TypeLifetime lifetime = TypeLifetime.Singleton)
	{
		ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
		Lifetime = lifetime;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Lifetime used when adding the default factory registration.
	/// </summary>
	public TypeLifetime Lifetime { get; }

	/// <summary>
	/// Optional service type (interface/base). When null, the attributed type is registered as itself.
	/// </summary>
	public Type ServiceType { get; }

	#endregion
}
