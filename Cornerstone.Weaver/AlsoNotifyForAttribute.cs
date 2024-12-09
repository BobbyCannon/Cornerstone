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
public class AlsoNotifyForAttribute : Attribute
{
	#region Constructors

	/// <summary>
	/// Initializes a new instance of <see cref="DependsOnAttribute" />.
	/// </summary>
	/// <param name="property"> A property that will be notified for. </param>
	public AlsoNotifyForAttribute(string property)
	{
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DependsOnAttribute" />.
	/// </summary>
	/// <param name="property"> A property that will be notified for. </param>
	/// <param name="otherProperties"> The properties that will be notified for. </param>
	public AlsoNotifyForAttribute(string property, params string[] otherProperties)
	{
	}

	#endregion
}