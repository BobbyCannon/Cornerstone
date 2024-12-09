#region References

using System;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;
using Cornerstone.Weaver;

#endregion

namespace Cornerstone.Avalonia;

[DoNotNotify]
public class CornerstoneTheme : Style
{
	#region Fields

	public static readonly StyledProperty<ThemeColor> ThemeColorProperty = AvaloniaProperty.Register<CornerstoneTheme, ThemeColor>(nameof(ThemeColor), ThemeColor.Blue);

	private ResourceInclude _colorTheme;
	private readonly ResourceInclude _colorThemeAmber;
	private readonly ResourceInclude _colorThemeBlue;
	private readonly ResourceInclude _colorThemeBlueGray;
	private readonly ResourceInclude _colorThemeBrown;
	private readonly ResourceInclude _colorThemeDeepOrange;
	private readonly ResourceInclude _colorThemeDeepPurple;
	private readonly ResourceInclude _colorThemeGray;
	private readonly ResourceInclude _colorThemeGreen;
	private readonly ResourceInclude _colorThemeIndigo;
	private readonly ResourceInclude _colorThemeOrange;
	private readonly ResourceInclude _colorThemePink;
	private readonly ResourceInclude _colorThemePurple;
	private readonly ResourceInclude _colorThemeRed;
	private readonly ResourceInclude _colorThemeTeal;

	#endregion

	#region Constructors

	public CornerstoneTheme() : this(null)
	{
	}

	public CornerstoneTheme(IServiceProvider sp)
	{
		AvaloniaXamlLoader.Load(sp, this);

		var uriAmber = new Uri("avares://Cornerstone.Avalonia/Themes/Color/ThemeColorAmber.xaml");
		var uriBlue = new Uri("avares://Cornerstone.Avalonia/Themes/Color/ThemeColorBlue.xaml");
		var uriBlueGray = new Uri("avares://Cornerstone.Avalonia/Themes/Color/ThemeColorBlueGray.xaml");
		var uriBrown = new Uri("avares://Cornerstone.Avalonia/Themes/Color/ThemeColorBrown.xaml");
		var uriDeepOrange = new Uri("avares://Cornerstone.Avalonia/Themes/Color/ThemeColorDeepOrange.xaml");
		var uriDeepPurple = new Uri("avares://Cornerstone.Avalonia/Themes/Color/ThemeColorDeepPurple.xaml");
		var uriGray = new Uri("avares://Cornerstone.Avalonia/Themes/Color/ThemeColorGray.xaml");
		var uriGreen = new Uri("avares://Cornerstone.Avalonia/Themes/Color/ThemeColorGreen.xaml");
		var uriIndigo = new Uri("avares://Cornerstone.Avalonia/Themes/Color/ThemeColorIndigo.xaml");
		var uriOrange = new Uri("avares://Cornerstone.Avalonia/Themes/Color/ThemeColorOrange.xaml");
		var uriPink = new Uri("avares://Cornerstone.Avalonia/Themes/Color/ThemeColorPink.xaml");
		var uriPurple = new Uri("avares://Cornerstone.Avalonia/Themes/Color/ThemeColorPurple.xaml");
		var uriRed = new Uri("avares://Cornerstone.Avalonia/Themes/Color/ThemeColorRed.xaml");
		var uriTeal = new Uri("avares://Cornerstone.Avalonia/Themes/Color/ThemeColorTeal.xaml");

		_colorThemeAmber = new ResourceInclude(uriAmber) { Source = uriAmber };
		_colorThemeBlue = new ResourceInclude(uriBlue) { Source = uriBlue };
		_colorThemeBlueGray = new ResourceInclude(uriBlueGray) { Source = uriBlueGray };
		_colorThemeBrown = new ResourceInclude(uriBrown) { Source = uriBrown };
		_colorThemeDeepOrange = new ResourceInclude(uriDeepOrange) { Source = uriDeepOrange };
		_colorThemeDeepPurple = new ResourceInclude(uriDeepPurple) { Source = uriDeepPurple };
		_colorThemeGray = new ResourceInclude(uriGreen) { Source = uriGray };
		_colorThemeGreen = new ResourceInclude(uriGreen) { Source = uriGreen };
		_colorThemeIndigo = new ResourceInclude(uriIndigo) { Source = uriIndigo };
		_colorThemeOrange = new ResourceInclude(uriOrange) { Source = uriOrange };
		_colorThemePink = new ResourceInclude(uriPink) { Source = uriPink };
		_colorThemePurple = new ResourceInclude(uriPurple) { Source = uriPurple };
		_colorThemeRed = new ResourceInclude(uriRed) { Source = uriRed };
		_colorThemeTeal = new ResourceInclude(uriTeal) { Source = uriTeal };

		SelectThemeColor(ThemeColor.Blue);
	}

	static CornerstoneTheme()
	{
		DejaVuSansLight = new("avares://Cornerstone.Avalonia/Assets/Fonts/DejaVuSansLight.ttf#DejaVu Sans Light");
		DejaVuSansMono = new("avares://Cornerstone.Avalonia/Assets/Fonts/DejaVuSansMono.ttf#DejaVu Sans Mono");
	}

	#endregion

	#region Properties

	public static FontFamily DejaVuSansMono { get; }

	public static FontFamily DejaVuSansLight { get; }

	public ThemeColor ThemeColor
	{
		get => GetValue(ThemeColorProperty);
		set
		{
			SetValue(ThemeColorProperty, value);
			SelectThemeColor(value);
		}
	}

	#endregion

	#region Methods

	public void SelectThemeColor(ThemeColor themeColor)
	{
		var newThemeColor = themeColor switch
		{
			ThemeColor.Amber => _colorThemeAmber,
			ThemeColor.Blue => _colorThemeBlue,
			ThemeColor.BlueGray => _colorThemeBlueGray,
			ThemeColor.Brown => _colorThemeBrown,
			ThemeColor.DeepOrange => _colorThemeDeepOrange,
			ThemeColor.DeepPurple => _colorThemeDeepPurple,
			ThemeColor.Gray => _colorThemeGray,
			ThemeColor.Green => _colorThemeGreen,
			ThemeColor.Indigo => _colorThemeIndigo,
			ThemeColor.Orange => _colorThemeOrange,
			ThemeColor.Pink => _colorThemePink,
			ThemeColor.Purple => _colorThemePurple,
			ThemeColor.Red => _colorThemeRed,
			ThemeColor.Teal => _colorThemeTeal,
			_ => _colorThemeBlue
		};

		if (newThemeColor == _colorTheme)
		{
			return;
		}

		if (_colorTheme is not null)
		{
			Resources.MergedDictionaries.Remove(_colorTheme);
		}

		Resources.MergedDictionaries.Insert(0, newThemeColor);

		_colorTheme = newThemeColor;
	}

	#endregion
}