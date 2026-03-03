namespace Cornerstone.Extensions;

/// <summary>
/// Extensions for numbers
/// </summary>
public static class IntegerExtensions
{
	#region Methods
	
	/// <summary>
	/// Ensure the value falls between the ranges.
	/// </summary>
	/// <param name="value"> The nullable int value. </param>
	/// <param name="inclusiveMinimum"> The inclusive minimal value. </param>
	/// <param name="inclusiveMaximum"> The inclusive maximum value. </param>
	/// <returns> The value within the provided ranges. </returns>
	public static void EnsureRange(ref int value, int inclusiveMinimum, int inclusiveMaximum)
	{
		if (value < inclusiveMinimum)
		{
			value = inclusiveMinimum;
		}

		if (value > inclusiveMaximum)
		{
			value = inclusiveMaximum;
		}
	}

	#endregion
}