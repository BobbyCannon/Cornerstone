#region References

using Avalonia.Data.Converters;
using Cornerstone.Sample.FontAwesome;

#endregion

namespace Cornerstone.Sample.Converters;

public static class IconConverters
{
	#region Fields

	public static readonly FuncValueConverter<IconSvg, string> GetPath = new(x => x?.Path ?? string.Empty);

	#endregion
}