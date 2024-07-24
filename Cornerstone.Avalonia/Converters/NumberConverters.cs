#region References

using Avalonia.Data.Converters;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class NumberConverters
{
	#region Fields

	public static readonly FuncValueConverter<double, decimal> DoubleToDecimal;
	public static readonly FuncValueConverter<int, bool> IsGreaterThanZero;
	public static readonly FuncValueConverter<int, bool> IsOne;
	public static readonly FuncValueConverter<int, bool> IsZero;

	#endregion

	#region Constructors

	static NumberConverters()
	{
		DoubleToDecimal = new(x => double.IsNormal(x) ? (decimal) x : 0);
		IsGreaterThanZero = new(x => x > 0);
		IsOne = new(x => x == 1);
		IsZero = new(x => x == 0);
	}

	#endregion
}