#region References

using Avalonia.Data.Converters;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class IntegerConverters
{
	#region Fields

	public static readonly FuncValueConverter<int, bool> IsGreaterThanOrEqualToZero;
	public static readonly FuncValueConverter<int, bool> IsGreaterThanZero;
	public static readonly FuncValueConverter<int, bool> IsOne;
	public static readonly FuncValueConverter<int, bool> IsZero;

	#endregion

	#region Constructors

	static IntegerConverters()
	{
		IsGreaterThanOrEqualToZero = new(x => x >= 0);
		IsGreaterThanZero = new(x => x > 0);
		IsOne = new(x => x == 1);
		IsZero = new(x => x == 0);
	}

	#endregion
}