#region References

using System;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using PropertyChanged;

#endregion

namespace Cornerstone.Avalonia;

[DoNotNotify]
[PropertyChanging.DoNotNotify]
public class CornerstoneTheme : Style
{
	#region Fields

	public static readonly StyledProperty<ThemeColor> ColorThemeProperty = AvaloniaProperty.Register<CornerstoneTheme, ThemeColor>(nameof(ThemeColor), ThemeColor.Blue);

	private ResourceInclude _colorTheme;
	private readonly ResourceInclude _colorThemeAmber;
	private readonly ResourceInclude _colorThemeBlue;
	private readonly ResourceInclude _colorThemeBlueGrey;
	private readonly ResourceInclude _colorThemeBrown;
	private readonly ResourceInclude _colorThemeDeepOrange;
	private readonly ResourceInclude _colorThemeDeepPurple;
	private readonly ResourceInclude _colorThemeGreen;
	private readonly ResourceInclude _colorThemeIndigo;
	private readonly ResourceInclude _colorThemeOrange;
	private readonly ResourceInclude _colorThemePink;
	private readonly ResourceInclude _colorThemePurple;
	private readonly ResourceInclude _colorThemeRed;
	private readonly ResourceInclude _colorThemeTeal;

	#endregion

	#region Constructors

	public CornerstoneTheme(IServiceProvider sp = null)
	{
		AvaloniaXamlLoader.Load(sp, this);

		var uriAmber = new Uri("avares://Cornerstone.Avalonia/Resources/Color/ColorThemeAmber.xaml");
		var uriBlue = new Uri("avares://Cornerstone.Avalonia/Resources/Color/ColorThemeBlue.xaml");
		var uriBlueGrey = new Uri("avares://Cornerstone.Avalonia/Resources/Color/ColorThemeBlueGrey.xaml");
		var uriBrown = new Uri("avares://Cornerstone.Avalonia/Resources/Color/ColorThemeBrown.xaml");
		var uriDeepOrange = new Uri("avares://Cornerstone.Avalonia/Resources/Color/ColorThemeDeepOrange.xaml");
		var uriDeepPurple = new Uri("avares://Cornerstone.Avalonia/Resources/Color/ColorThemeDeepPurple.xaml");
		var uriGreen = new Uri("avares://Cornerstone.Avalonia/Resources/Color/ColorThemeGreen.xaml");
		var uriIndigo = new Uri("avares://Cornerstone.Avalonia/Resources/Color/ColorThemeIndigo.xaml");
		var uriOrange = new Uri("avares://Cornerstone.Avalonia/Resources/Color/ColorThemeOrange.xaml");
		var uriPink = new Uri("avares://Cornerstone.Avalonia/Resources/Color/ColorThemePink.xaml");
		var uriPurple = new Uri("avares://Cornerstone.Avalonia/Resources/Color/ColorThemePurple.xaml");
		var uriRed = new Uri("avares://Cornerstone.Avalonia/Resources/Color/ColorThemeRed.xaml");
		var uriTeal = new Uri("avares://Cornerstone.Avalonia/Resources/Color/ColorThemeTeal.xaml");

		_colorThemeAmber = new ResourceInclude(uriAmber) { Source = uriAmber };
		_colorThemeBlue = new ResourceInclude(uriBlue) { Source = uriBlue };
		_colorThemeBlueGrey = new ResourceInclude(uriBlueGrey) { Source = uriBlueGrey };
		_colorThemeBrown = new ResourceInclude(uriBrown) { Source = uriBrown };
		_colorThemeDeepOrange = new ResourceInclude(uriDeepOrange) { Source = uriDeepOrange };
		_colorThemeDeepPurple = new ResourceInclude(uriDeepPurple) { Source = uriDeepPurple };
		_colorThemeGreen = new ResourceInclude(uriGreen) { Source = uriGreen };
		_colorThemeIndigo = new ResourceInclude(uriIndigo) { Source = uriIndigo };
		_colorThemeOrange = new ResourceInclude(uriOrange) { Source = uriOrange };
		_colorThemePink = new ResourceInclude(uriPink) { Source = uriPink };
		_colorThemePurple = new ResourceInclude(uriPurple) { Source = uriPurple };
		_colorThemeRed = new ResourceInclude(uriRed) { Source = uriRed };
		_colorThemeTeal = new ResourceInclude(uriTeal) { Source = uriTeal };

		SelectColorTheme(ThemeColor.Blue);
	}

	static CornerstoneTheme()
	{
		DejaVuSansMono = new("avares://Cornerstone.Avalonia/Assets/Fonts/DejaVuSansMono.ttf#DejaVu Sans Mono");
		OpenSansLight = new("avares://Cornerstone.Avalonia/Assets/Fonts/OpenSans-Light.ttf#Open Sans");
		OpenSansRegular = new("avares://Cornerstone.Avalonia/Assets/Fonts/OpenSans-Regular.ttf#Open Sans");
		OpenSansSemibold = new("avares://Cornerstone.Avalonia/Assets/Fonts/OpenSans-Semibold.ttf#Open Sans");
	}

	#endregion

	#region Properties

	public static FontFamily DejaVuSansMono { get; }

	public static FontFamily OpenSansLight { get; }

	public static FontFamily OpenSansRegular { get; }

	public static FontFamily OpenSansSemibold { get; }

	public ThemeColor ThemeColor
	{
		get => GetValue(ColorThemeProperty);
		set
		{
			SetValue(ColorThemeProperty, value);
			SelectColorTheme(value);
		}
	}

	#endregion

	#region Methods

	public void SelectColorTheme(ThemeColor themeColor)
	{
		var newColorTheme = themeColor switch
		{
			ThemeColor.Amber => _colorThemeAmber,
			ThemeColor.Blue => _colorThemeBlue,
			ThemeColor.BlueGrey => _colorThemeBlueGrey,
			ThemeColor.Brown => _colorThemeBrown,
			ThemeColor.DeepOrange => _colorThemeDeepOrange,
			ThemeColor.DeepPurple => _colorThemeDeepPurple,
			ThemeColor.Green => _colorThemeGreen,
			ThemeColor.Indigo => _colorThemeIndigo,
			ThemeColor.Orange => _colorThemeOrange,
			ThemeColor.Pink => _colorThemePink,
			ThemeColor.Purple => _colorThemePurple,
			ThemeColor.Red => _colorThemeRed,
			ThemeColor.Teal => _colorThemeTeal,
			_ => _colorThemeBlue
		};

		if (newColorTheme == _colorTheme)
		{
			return;
		}

		if (_colorTheme is not null)
		{
			Resources.MergedDictionaries.Remove(_colorTheme);
		}

		Resources.MergedDictionaries.Insert(0, newColorTheme);

		_colorTheme = newColorTheme;
	}

	#endregion
}