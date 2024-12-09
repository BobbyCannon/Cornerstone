#region References

using Avalonia.Data.Converters;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class DoubleConverters
{
	#region Fields

	public static readonly FuncValueConverter<double, decimal> DoubleToDecimal;
	public static readonly FuncValueConverter<double, bool> IsGreaterThanOrEqualToZero;
	public static readonly FuncValueConverter<double, bool> IsGreaterThanZero;
	public static readonly FuncValueConverter<double, bool> IsZero;

	#endregion

	#region Constructors

	static DoubleConverters()
	{
		DoubleToDecimal = new(x => double.IsNormal(x) ? (decimal) x : 0);
		IsGreaterThanOrEqualToZero = new(x => x >= 0);
		IsGreaterThanZero = new(x => x > 0);
		IsZero = new(x => x == 0);
	}

	#endregion
}