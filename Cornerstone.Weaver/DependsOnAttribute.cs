#region References

using System;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace Cornerstone.Weaver;

/// <summary>
/// Injects this property to be notified when a dependent property is set.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
[SuppressMessage("ReSharper", "UnusedParameter.Local")]
public class DependsOnAttribute : Attribute
{
	#region Constructors

	/// <summary>
	/// Initializes a new instance of <see cref="DependsOnAttribute" />.
	/// </summary>
	/// <param name="dependency"> A property that the assigned property depends on. </param>
	public DependsOnAttribute(string dependency)
	{
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DependsOnAttribute" />.
	/// </summary>
	/// <param name="dependency"> A property that the assigned property depends on. </param>
	/// <param name="otherDependencies"> The properties that the assigned property depends on. </param>
	public DependsOnAttribute(string dependency, params string[] otherDependencies)
	{
	}

	#endregion
}