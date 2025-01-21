#region References

using System;

#endregion

namespace Cornerstone.Attributes;

/// <summary>
/// Represents changes for properties.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class IntegerChangeAttribute : Attribute
{
	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="IntegerChangeAttribute" /> class.
	/// </summary>
	/// <param name="change"> The value for large and small change. </param>
	public IntegerChangeAttribute(int change) : this(change, change)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="IntegerChangeAttribute" /> class.
	/// </summary>
	/// <param name="largeChange"> The value for large change. </param>
	/// <param name="smallChange"> The value for small change. </param>
	public IntegerChangeAttribute(int largeChange, int smallChange)
	{
		LargeChange = largeChange;
		SmallChange = smallChange;
	}

	#endregion

	#region Properties

	public int LargeChange { get; }
	public int SmallChange { get; }

	#endregion
}