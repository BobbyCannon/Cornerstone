#region References

using System.Collections.Generic;
using System.Collections.Immutable;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.Themes;

public class ThemeColorPaletteDetails
{
	#region Fields

	private static readonly ImmutableDictionary<ThemeColor, EnumDetails> _details;

	#endregion

	#region Constructors

	public ThemeColorPaletteDetails(ThemeColor color, ThemeColorDetails[] colors)
	{
		var details = _details[color];

		ThemeColor = color;
		Name = details.DisplayName;
		Order = details.DisplayOrder;
		Colors = new(colors);
	}

	static ThemeColorPaletteDetails()
	{
		_details = SourceReflector.GetEnumDetailsDictionary<ThemeColor>();
	}

	#endregion

	#region Properties

	public ThemeColorDetails Color => new(Name, Colors[5].Color, Colors[5].Foreground);

	public List<ThemeColorDetails> Colors { get; }

	public string Name { get; }

	public int Order { get; }

	public ThemeColor ThemeColor { get; }

	#endregion
}