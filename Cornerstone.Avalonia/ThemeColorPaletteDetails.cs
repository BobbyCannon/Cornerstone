#region References

using Cornerstone.Collections;

#endregion

namespace Cornerstone.Avalonia;

public class ThemeColorPaletteDetails
{
	#region Properties

	public SpeedyList<ThemeColorDetails> Colors { get; set; }

	public string Name { get; set; }

	public int Order { get; set; }

	public ThemeColor ThemeColor { get; set; }

	#endregion
}