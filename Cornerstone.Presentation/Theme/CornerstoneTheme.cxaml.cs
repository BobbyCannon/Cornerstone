#region References

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Theme.Theming;
using Cornerstone.Profiling;
using Cornerstone.Runtime;
using Style = Cornerstone.Presentation.Styling.Style;
using Cornerstone.Presentation.Controls.Input;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Presentation.Theme;

public class CornerstoneTheme : Style, IApplicationTheme
{
	#region Constants

	/// <summary>
	/// Primary body / list font size resource key (matches Theme.Constants.axaml).
	/// </summary>
	public const string ControlFontSizeKey = "ControlFontSize";

	/// <summary>
	/// Secondary / muted font size resource key.
	/// </summary>
	public const string ControlFontSizeSmallKey = "ControlFontSizeSmall";

	/// <summary>
	/// Emphasis / section title font size resource key.
	/// </summary>
	public const string ControlFontSizeLargeKey = "ControlFontSizeLarge";

	/// <summary>
	/// Compact and Large density step from the Normal values in Theme.Constants.axaml.
	/// </summary>
	private const double FontSizeDensityStep = 2;

	/// <summary>
	/// Compact secondary is one point below Normal so captions stay readable.
	/// </summary>
	private const double FontSizeSmallCompactStep = 1;

	#endregion

	#region Fields

	public static readonly StyledProperty<ThemeColor> ThemeColorProperty;
	public static readonly StyledProperty<ThemeDensity> ThemeDensityProperty;
	public static readonly StyledProperty<ThemeMode> ThemeModeProperty;
	private ResourceDictionary _colorTheme;
	private static double _controlFontSize = 14;
	private static double _controlFontSizeLarge = 16;
	private static double _controlFontSizeSmall = 12;
	private static bool _fontSizeDefaultsLoaded;

	#endregion

	#region Constructors

	public CornerstoneTheme() : this(null!)
	{
	}

	public CornerstoneTheme(IServiceProvider sp)
	{
		using (AppBootstrap.StartupProfiler.Start("CornerstoneTheme.Xaml"))
		{
			CornerstoneXamlLoader.Load(sp, this);
		}

		TryCaptureFontSizeDefaults(Resources);

		using (AppBootstrap.StartupProfiler.Start("CornerstoneTheme.SelectThemeColor"))
		{
			SelectThemeColor(ThemeColor);
		}

		using (AppBootstrap.StartupProfiler.Start("CornerstoneTheme.SelectThemeMode"))
		{
			SelectThemeMode(ThemeMode);
		}

		using (AppBootstrap.StartupProfiler.Start("CornerstoneTheme.SelectThemeDensity"))
		{
			SelectThemeDensity(ThemeDensity);
		}
	}

	static CornerstoneTheme()
	{
		Control.VisualTreeAttachedHook = DispatchableVisualTree.OnAttachedToVisualTree;
		Control.VisualTreeDetachedHook = DispatchableVisualTree.OnDetachedFromVisualTree;
		Control.DataContextChangedHook = DispatchableVisualTree.OnDataContextChanged;
		Control.ViewModelChangedHook = DispatchableVisualTree.OnViewModelChanged;
		Control.DesignDataFactory = static type => AppBootstrap.GetInstance(type);
		Control.DesignLifecycleHook = static data =>
		{
			if (data is ViewModel viewModel && !viewModel.IsLifecycleInitialized())
			{
				viewModel.InitializeLifecycle();
				viewModel.LoadLifecycle();
				viewModel.StartLifecycle();
			}
		};

		ThemeColorProperty = PresentationProperty.Register<CornerstoneTheme, ThemeColor>(nameof(ThemeColor), ThemeColor.Blue);
		ThemeDensityProperty = PresentationProperty.Register<CornerstoneTheme, ThemeDensity>(nameof(ThemeDensity), ThemeDensity.Normal);
		ThemeModeProperty = PresentationProperty.Register<CornerstoneTheme, ThemeMode>(nameof(ThemeMode), ThemeMode.Dark);
		ThemeColorProperty.Changed.AddClassHandler<CornerstoneTheme>((x, e) => x.SelectThemeColor(e.GetNewValue<ThemeColor>()));
		ThemeModeProperty.Changed.AddClassHandler<CornerstoneTheme>((_, e) => SelectThemeMode(e.GetNewValue<ThemeMode>()));
		ThemeDensityProperty.Changed.AddClassHandler<CornerstoneTheme>((_, e) => SelectThemeDensity(e.GetNewValue<ThemeDensity>()));

		DejaVuSansLight = new("csres://Cornerstone.Presentation/Assets/Fonts/DejaVuSansLight.ttf#DejaVu Sans Light");
		// Directory of Regular/Bold/Oblique cuts. Pointing at one .ttf makes Bold synthetic and fuzzy.
		DejaVuSansMono = new("csres://Cornerstone.Presentation/Assets/Fonts/DejaVuSansMono#DejaVu Sans Mono");
		OpenSansLight = new("csres://Cornerstone.Presentation/Assets/Fonts/OpenSans/OpenSans-Light.ttf#Open Sans");
		// Directory of Regular/Bold/Italic cuts. A single Regular.ttf makes FontWeight.Bold synthetic.
		OpenSansRegular = new("csres://Cornerstone.Presentation/Assets/Fonts/OpenSans#Open Sans");

		ToggleThemeCommand = new RelayCommand(ToggleTheme);
		EnsureFontSizeDefaults();
		DefaultFocus.EnsureRegistered();
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
		set => SetValue(ThemeColorProperty, value);
	}

	/// <summary>
	/// Compact / Normal / Large chrome and list text density.
	/// </summary>
	public ThemeDensity ThemeDensity
	{
		get => GetValue(ThemeDensityProperty);
		set => SetValue(ThemeDensityProperty, value);
	}

	public ThemeMode ThemeMode
	{
		get => GetValue(ThemeModeProperty);
		set => SetValue(ThemeModeProperty, value);
	}

	public static ICommand ToggleThemeCommand { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Primary UI font size for the density preset. Normal is ControlFontSize in Theme.Constants.axaml.
	/// </summary>
	public static double GetControlFontSize(ThemeDensity density)
	{
		EnsureFontSizeDefaults();
		return ScaleFontSize(_controlFontSize, density, -FontSizeDensityStep, FontSizeDensityStep);
	}

	/// <summary>
	/// Emphasis UI font size for the density preset. Normal is ControlFontSizeLarge in Theme.Constants.axaml.
	/// </summary>
	public static double GetControlFontSizeLarge(ThemeDensity density)
	{
		EnsureFontSizeDefaults();
		return ScaleFontSize(_controlFontSizeLarge, density, -FontSizeDensityStep, FontSizeDensityStep);
	}

	/// <summary>
	/// Secondary UI font size for the density preset. Normal is ControlFontSizeSmall in Theme.Constants.axaml.
	/// </summary>
	public static double GetControlFontSizeSmall(ThemeDensity density)
	{
		EnsureFontSizeDefaults();
		return ScaleFontSize(_controlFontSizeSmall, density, -FontSizeSmallCompactStep, FontSizeDensityStep);
	}

	/// <summary>
	/// Normalize unknown values to <see cref="ThemeDensity.Normal" />.
	/// </summary>
	public static ThemeDensity NormalizeThemeDensity(ThemeDensity density)
	{
		return density is ThemeDensity.Compact or ThemeDensity.Normal or ThemeDensity.Large
			? density
			: ThemeDensity.Normal;
	}

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
		Theming.Theme.RaiseAccentChanged();
	}

	/// <summary>
	/// Apply Compact / Normal / Large font tokens (ControlFontSize / Small / Large).
	/// </summary>
	public static void SelectThemeDensity(ThemeDensity density)
	{
		density = NormalizeThemeDensity(density);

		var primary = GetControlFontSize(density);
		var small = GetControlFontSizeSmall(density);
		var large = GetControlFontSizeLarge(density);

		// Prefer the live theme instance so resources sit with the rest of CornerstoneTheme.
		CornerstoneTheme theme = null;
		var current = Application.Current;
		if (current != null)
		{
			foreach (var style in current.Styles)
			{
				if (style is CornerstoneTheme found)
				{
					theme = found;
					break;
				}
			}
		}

		if (theme != null)
		{
			theme.SetValue(ThemeDensityProperty, density);
			theme.Resources[ControlFontSizeKey] = primary;
			theme.Resources[ControlFontSizeSmallKey] = small;
			theme.Resources[ControlFontSizeLargeKey] = large;
		}

		var application = Application.Current;
		if (application == null)
		{
			#if DEBUG
			if (Debugger.IsAttached && (theme == null))
			{
				Debugger.Break();
			}
			#endif
			return;
		}

		// Application-level override so DynamicResource resolves even when theme lookup order varies.
		application.Resources[ControlFontSizeKey] = primary;
		application.Resources[ControlFontSizeSmallKey] = small;
		application.Resources[ControlFontSizeLargeKey] = large;
	}

	[UnconditionalSuppressMessage("Trim", "IL2026", Justification = "Loads compiled Theme.Constants.cxaml from this assembly; fallback is compiled field defaults.")]
	private static void EnsureFontSizeDefaults()
	{
		if (_fontSizeDefaultsLoaded)
		{
			return;
		}

		_fontSizeDefaultsLoaded = true;

		try
		{
			var uri = new Uri("csres://Cornerstone.Presentation/Theming/Theme.Constants.cxaml");
			if (CornerstoneXamlLoader.Load(uri) is ResourceDictionary dictionary)
			{
				TryCaptureFontSizeDefaults(dictionary);
			}
		}
		catch
		{
			// Compiled field defaults match Theme.Constants.axaml Normal values.
		}
	}

	private static double ScaleFontSize(double normalSize, ThemeDensity density, double compactDelta, double largeDelta)
	{
		return NormalizeThemeDensity(density) switch
		{
			ThemeDensity.Compact => normalSize + compactDelta,
			ThemeDensity.Large => normalSize + largeDelta,
			_ => normalSize
		};
	}

	private static void TryAssignDouble(IResourceDictionary dictionary, string key, ref double target)
	{
		if (dictionary.TryGetValue(key, out var value) && (value is double parsed))
		{
			target = parsed;
		}
	}

	private static void TryCaptureFontSizeDefaults(IResourceDictionary resources)
	{
		if (resources == null)
		{
			return;
		}

		TryAssignDouble(resources, ControlFontSizeKey, ref _controlFontSize);
		TryAssignDouble(resources, ControlFontSizeSmallKey, ref _controlFontSizeSmall);
		TryAssignDouble(resources, ControlFontSizeLargeKey, ref _controlFontSizeLarge);

		foreach (var merged in resources.MergedDictionaries)
		{
			if (merged is ResourceDictionary dictionary)
			{
				TryAssignDouble(dictionary, ControlFontSizeKey, ref _controlFontSize);
				TryAssignDouble(dictionary, ControlFontSizeSmallKey, ref _controlFontSizeSmall);
				TryAssignDouble(dictionary, ControlFontSizeLargeKey, ref _controlFontSizeLarge);
			}
		}

		_fontSizeDefaultsLoaded = true;
	}

	private void Populate(ResourceDictionary dictionary, ThemeColorPaletteDetails colors)
	{
		for (var i = 0; i < colors.Colors.Count; i++)
		{
			dictionary.Add($"ThemeColor{i:00}", Color.Parse(colors.Colors[i].Color));
			dictionary.Add($"ThemeText{i:00}", Color.Parse(colors.Colors[i].Foreground));
			dictionary.Add($"ThemeColorBrush{i:00}", colors.Colors[i].Brush);
		}
	}

	private static void SelectThemeMode(ThemeMode mode)
	{
		var application = Application.Current;
		if (application == null)
		{
			#if DEBUG
			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}
			#endif
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
			#if DEBUG
			if (Debugger.IsAttached)
			{
				Debugger.Break();
			}
			#endif
			return;
		}

		var newTheme = current == ThemeVariant.Dark
			? ThemeMode.Light
			: ThemeMode.Dark;

		SelectThemeMode(newTheme);
	}

	#endregion
}
