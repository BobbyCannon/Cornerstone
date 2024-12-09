#region References

using System;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace Cornerstone.Weaver;

/// <summary>
/// Defines filters on which types to include in the
/// weaving process. These filters are Regex based and
/// are matched against the Type.FullName
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
[SuppressMessage("ReSharper", "UnusedParameter.Local")]
public class FilterTypeAttribute : Attribute
{
	#region Constructors

	/// <summary>
	/// Initializes a new instance of <see cref="FilterTypeAttribute" />.
	/// </summary>
	/// <param name="filter">
	/// The filter to apply to the types in this
	/// assembly. Matching is done by the Regex.IsMatch method using
	/// the Type.FullName as input for the Matching method.
	/// </param>
	public FilterTypeAttribute(string filter)
	{
	}

	#endregion
}