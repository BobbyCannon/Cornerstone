#region References

using Avalonia.Media;

#endregion

namespace Cornerstone.Avalonia.Themes;

public class ThemeColorDetails
{
	#region Fields

	private IImmutableBrush _brush;
	private IImmutableBrush _foregroundBrush;

	#endregion

	#region Constructors

	public ThemeColorDetails(string name, string color, string foreground)
	{
		Name = name;
		Color = color;
		Foreground = foreground;
	}

	#endregion

	#region Properties

	public IImmutableBrush Brush => _brush ??= SolidColorBrush.Parse(Color).ToImmutable();

	public string Color { get; }

	public string Foreground { get; }

	public IImmutableBrush ForegroundBrush => _foregroundBrush ??= SolidColorBrush.Parse(Foreground).ToImmutable();

	public string Name { get; }

	#endregion
}