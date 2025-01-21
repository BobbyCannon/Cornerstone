#region References

using System;

#endregion

namespace Cornerstone.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class DecimalPrecisionAttribute : Attribute
{
	#region Fields

	/// <summary>
	/// The format string
	/// </summary>
	public readonly string FormatString;

	/// <summary>
	/// The increment
	/// </summary>
	public readonly decimal Increment;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="DecimalPrecisionAttribute" /> class.
	/// </summary>
	public DecimalPrecisionAttribute() : this(0.01m, "{0:0.00}")
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DecimalPrecisionAttribute" /> class.
	/// </summary>
	/// <param name="increment"> The increment. </param>
	/// <param name="formatString"> The format string. </param>
	public DecimalPrecisionAttribute(decimal increment, string formatString)
	{
		Increment = increment;
		FormatString = formatString;
	}

	#endregion
}