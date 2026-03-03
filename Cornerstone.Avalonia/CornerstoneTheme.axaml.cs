#region References

using System;
using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Cornerstone.Avalonia.Themes;
using Cornerstone.Presentation;
using Style = Avalonia.Styling.Style;

#endregion

namespace Cornerstone.Avalonia;

public class CornerstoneTheme : Style
{
	#region Fields

	public static readonly StyledProperty<ThemeColor> ThemeColorProperty;
	public static readonly StyledProperty<ThemeMode> ThemeModeProperty;
	private ResourceDictionary _colorTheme;

	#endregion

	#region Constructors

	public CornerstoneTheme() : this(null!)
	{
	}

	public CornerstoneTheme(IServiceProvider sp)
	{
		AvaloniaXamlLoader.Load(sp, this);
		SelectThemeColor(ThemeColor.Blue);
		SelectThemeMode(ThemeMode.Dark);
	}

	static CornerstoneTheme()
	{
		ThemeColorProperty = AvaloniaProperty.Register<CornerstoneTheme, ThemeColor>(nameof(ThemeColor), ThemeColor.Gray);
		ThemeModeProperty = AvaloniaProperty.Register<CornerstoneTheme, ThemeMode>(nameof(ThemeMode), ThemeMode.Dark);

		DejaVuSansLight = new("avares://Cornerstone.Avalonia/Assets/Fonts/DejaVuSansLight.ttf#DejaVu Sans Light");
		DejaVuSansMono = new("avares://Cornerstone.Avalonia/Assets/Fonts/DejaVuSansMono.ttf#DejaVu Sans Mono");
		OpenSansLight = new("avares://Cornerstone.Avalonia/Assets/Fonts/OpenSansLight.ttf#Open Sans");
		OpenSansRegular = new("avares://Cornerstone.Avalonia/Assets/Fonts/OpenSansRegular.ttf#Open Sans");

		ToggleThemeCommand = new RelayCommand(ToggleTheme);
	}

	#endregion

	#region Properties

	public static FontFamily DejaVuSansLight { get; }

	public static FontFamily DejaVuSansMono { get; }

	public static FontFamily OpenSansLight { get; }

	public static FontFamily OpenSansRegular { get; }

	public ThemeColor ThemeColor
	{
		get => GetValue(ThemeColorProperty);
		set
		{
			SetValue(ThemeColorProperty, value);
			SelectThemeColor(value);
		}
	}

	public ThemeMode ThemeMode
	{
		get => GetValue(ThemeModeProperty);
		set
		{
			SetValue(ThemeModeProperty, value);
			SelectThemeMode(value);
		}
	}

	public static ICommand ToggleThemeCommand { get; }

	#endregion

	#region Methods

	public void SelectThemeColor(ThemeColor themeColor)
	{
		if (_colorTheme is not null)
		{
			Resources.MergedDictionaries.Remove(_colorTheme);
		}

		_colorTheme = new ResourceDictionary();

		Populate(_colorTheme,
			themeColor switch
			{
				ThemeColor.Amber => ThemeColorPalette.Amber,
				ThemeColor.Blue => ThemeColorPalette.Blue,
				ThemeColor.BlueGray => ThemeColorPalette.BlueGray,
				ThemeColor.Brown => ThemeColorPalette.Brown,
				ThemeColor.DeepOrange => ThemeColorPalette.DeepOrange,
				ThemeColor.DeepPurple => ThemeColorPalette.DeepPurple,
				ThemeColor.Gray => ThemeColorPalette.Gray,
				ThemeColor.Green => ThemeColorPalette.Green,
				ThemeColor.Indigo => ThemeColorPalette.Indigo,
				ThemeColor.Orange => ThemeColorPalette.Orange,
				ThemeColor.Pink => ThemeColorPalette.Pink,
				ThemeColor.Purple => ThemeColorPalette.Purple,
				ThemeColor.Red => ThemeColorPalette.Red,
				ThemeColor.Teal => ThemeColorPalette.Teal,
				_ => ThemeColorPalette.Blue
			}
		);

		Resources.MergedDictionaries.Insert(0, _colorTheme);
	}

	private void Populate(ResourceDictionary dictionary, ThemeColorPaletteDetails colors)
	{
		for (var i = 0; i < colors.Colors.Count; i++)
		{
			dictionary.Add($"ThemeColor{i:00}", Color.Parse(colors.Colors[i].Color));
			dictionary.Add($"ThemeForeground{i:00}", Color.Parse(colors.Colors[i].Foreground));
		}
	}

	private static void SelectThemeMode(ThemeMode mode)
	{
		var application = Application.Current;
		if (application == null)
		{
			Debugger.Break();
			return;
		}

		var variant = mode switch
		{
			ThemeMode.Default => ThemeVariant.Default,
			ThemeMode.Dark => ThemeVariant.Dark,
			ThemeMode.Light => ThemeVariant.Light,
			_ => application.RequestedThemeVariant
		};

		application.RequestedThemeVariant = variant;
	}

	private static void ToggleTheme(object obj)
	{
		var application = Application.Current;
		var current = application?.RequestedThemeVariant;
		if (current == null)
		{
			Debugger.Break();
			return;
		}

		var newTheme = current == ThemeVariant.Dark
			? ThemeMode.Light
			: ThemeMode.Dark;

		SelectThemeMode(newTheme);
	}

	#endregion
}