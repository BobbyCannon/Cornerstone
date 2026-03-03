#region References

using Avalonia.Data.Converters;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class IntegerConverters
{
	#region Fields

	public static readonly FuncValueConverter<int, object, bool> IsGreaterThan;
	public static readonly FuncValueConverter<int, bool> IsGreaterThanOrEqualToZero;
	public static readonly FuncValueConverter<int, bool> IsGreaterThanZero;
	public static readonly FuncValueConverter<int, bool> IsOne;
	public static readonly FuncValueConverter<int, bool> IsZero;
	public static readonly FuncValueConverter<bool, object, int> OneOrTheOtherNumber;

	#endregion

	#region Constructors

	static IntegerConverters()
	{
		IsGreaterThan = new((x, y) => int.TryParse(y.ToString(), out var number) && (x > number));
		IsGreaterThanOrEqualToZero = new(x => x >= 0);
		IsGreaterThanZero = new(x => x > 0);
		IsOne = new(x => x == 1);
		IsZero = new(x => x == 0);
		OneOrTheOtherNumber = new(OneOrTheOtherNumberSelector);
	}

	#endregion

	#region Methods

	private static int OneOrTheOtherNumberSelector(bool value, object options)
	{
		var parts = options.ToString()?.Split(",");
		if (parts is not { Length: 2 })
		{
			return 0;
		}

		var part = value ? parts[0] : parts[1];
		return int.TryParse(part, out var iValue) ? iValue : 0;
	}

	#endregion
}