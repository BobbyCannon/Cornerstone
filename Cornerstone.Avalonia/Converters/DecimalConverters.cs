#region References

using Avalonia.Data.Converters;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class DecimalConverters
{
	#region Constants

	// Conversion factor from millibars to inches of mercury
	private const decimal MbToInHg = 1 / 33.86389m;

	// Conversion factor from pascals to inches of mercury
	private const decimal PaToInHg = 1 / 3386.389m;

	#endregion

	#region Fields

	/// <summary>
	/// Millibars (mb) to inches of mercury (inHg)
	/// </summary>
	public static readonly FuncValueConverter<decimal, decimal> FromMillibarsToMercury;

	/// <summary>
	/// Pascals (Pa) to inches of mercury (inHg)
	/// </summary>
	public static readonly FuncValueConverter<decimal, decimal> FromPascalsToMercury;

	/// <summary>
	/// Convert from celsius to fahrenheit
	/// </summary>
	public static readonly FuncValueConverter<decimal, decimal> ToFahrenheit;

	#endregion

	#region Constructors

	static DecimalConverters()
	{
		ToFahrenheit = new(celsius => ((celsius * 9) / 5) + 32);
		FromPascalsToMercury = new(pa => pa * PaToInHg);
		FromMillibarsToMercury = new(mb => mb * MbToInHg);
	}

	#endregion
}