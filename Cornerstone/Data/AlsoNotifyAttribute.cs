#region References

using System;

#endregion

namespace Cornerstone.Data;

/// <summary>
/// Instruct Cornerstone.Generators to also raise INPC notifications for the named properties whenever the property this is applied to changes
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public class AlsoNotifyAttribute : Attribute
{
	#region Constructors

	/// <summary>
	/// Raise INPC notifications for the given properties when the property generated for this backing field changes
	/// </summary>
	/// <param name="otherProperties"> Other properties to raise INPC notifications for. </param>
	public AlsoNotifyAttribute(params string[] otherProperties)
	{
		OtherProperties = otherProperties;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Other properties to raise INPC notifications for.
	/// </summary>
	public string[] OtherProperties { get; }

	#endregion
}