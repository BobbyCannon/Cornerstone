#region References

using Avalonia.Data.Converters;
using Avalonia.Sample.FontAwesome;

#endregion

namespace Avalonia.Sample.Converters;

public static class IconConverters
{
	#region Fields

	public static readonly FuncValueConverter<IconSvg, string> GetPath = new(x => x?.Path ?? string.Empty);

	#endregion
}