#region References

using System;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Instruct Cornerstone.Generators to raise INPC notifications for this property whenever one of
/// the named generated properties is changed.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class DependsOnAttribute : Attribute
{
	#region Constructors

	/// <summary>
	/// Raise an INPC notification for this property whenever one of the named properties is changed
	/// </summary>
	/// <param name="dependsOn"> Other properties this property depends on </param>
	public DependsOnAttribute(params string[] dependsOn)
	{
		DependsOn = dependsOn;
	}

	#endregion

	#region Properties

	public string[] DependsOn { get; }

	#endregion
}