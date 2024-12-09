#region References

#endregion

namespace Cornerstone.Avalonia;

public class ThemeColorDetails
{
	#region Constructors

	public ThemeColorDetails(string name, string background, string foreground)
	{
		Name = name;
		Background = background;
		Foreground = foreground;
	}

	#endregion

	#region Properties

	public string Background { get; set; }

	public string Name { get; set; }

	public string Foreground { get; set; }

	#endregion
}