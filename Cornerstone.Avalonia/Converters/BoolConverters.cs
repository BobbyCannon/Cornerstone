#region References

using Avalonia.Data.Converters;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class BoolConverters
{
	#region Fields

	public static readonly FuncValueConverter<bool, bool> Inverted = new(x => !x);

	#endregion
}