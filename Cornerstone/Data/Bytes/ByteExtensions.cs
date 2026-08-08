#region References

using Cornerstone.Text;
using Cornerstone.Text.Human;

#endregion

namespace Cornerstone.Data.Bytes;

public static class ByteExtensions
{
	#region Methods

	/// <summary>
	/// Convert the byte unit into string format value.
	/// </summary>
	/// <param name="byteUnit"> The unit of the byte value. </param>
	/// <param name="count"> The size of the data. </param>
	/// <param name="format"> The word format for the unit. </param>
	/// <returns> The string value of the unit. </returns>
	public static string GetHumanizeStringFormat(this ByteUnit byteUnit, decimal count, WordFormat format = WordFormat.Abbreviation)
	{
		var abbreviated = format == WordFormat.Abbreviation;
		var resourceKey = abbreviated
			? $"DataUnit_{byteUnit}Symbol"
			: $"DataUnit_{byteUnit}";
		var resourceValue = HumanFormatter.GetStringFormat(resourceKey);

		if (!abbreviated && (count > 1))
		{
			resourceValue += 's';
		}

		return resourceValue;
	}

	#endregion
}