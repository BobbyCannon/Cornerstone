#region References

using System;
using System.Text.Json;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Theme;
using Cornerstone.Presentation.Theme.Theming;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Sample.Theming;
using Cornerstone.Serialization;
using Cornerstone.Settings;

#endregion

namespace Cornerstone.Sample.Keystone.State;

[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"], false)]
[DependencyInjected]
public partial class AppSettings : SettingsFile<AppSettings>
{
	#region Constructors

	/// <summary>
	/// Serialization use only
	/// </summary>
	public AppSettings()
	{
	}

	[DependencyInjectionConstructor]
	public AppSettings(IRuntimeInformation runtimeInformation)
		: base("ApplicationSettings.json", runtimeInformation)
	{
		EsriApiKey = string.Empty;
		ThemeSkin = SampleThemeCatalog.CornerstoneName;
		ThemeColor = ThemeColor.Blue;
		ThemeMode = ThemeMode.Dark;
		ThemeDensity = ThemeDensity.Normal;
	}

	#endregion

	#region Properties

	public partial string EsriApiKey { get; set; }

	/// <summary>
	/// Navigation menu page name: Home, About, or Settings.
	/// Older builds stored a section type name. Startup still opens that section under Home once.
	/// </summary>
	public partial string SelectedTab { get; set; }

	public partial ThemeColor ThemeColor { get; set; }

	/// <summary>
	/// Skin name from <see cref="SampleThemeCatalog" />. Cornerstone is the product theme.
	/// </summary>
	public partial string ThemeSkin { get; set; }

	/// <summary>
	/// Compact / Normal / Large chrome and list text.
	/// </summary>
	public partial ThemeDensity ThemeDensity { get; set; }

	/// <summary>
	/// Dark / Light / Default (replaces legacy UseDarkMode bool).
	/// </summary>
	public partial ThemeMode ThemeMode { get; set; }

	public partial WindowLocation WindowLocation { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Put the selected skin on Application.Styles, then push Color / Mode / Density onto it.
	/// Skins share the product control templates, so the swap replaces the style in place.
	/// Rebuilding templates collapses layout and makes the window jump.
	/// </summary>
	public void ApplyTheme()
	{
		var theme = EnsureThemeSkin();
		if (theme != null)
		{
			theme.ThemeColor = ThemeColor;
			theme.ThemeMode = ThemeMode;
		}

		CornerstoneTheme.SelectThemeDensity(ThemeDensity);
	}

	public override JsonSerializerOptions GetSerializationSettings()
	{
		return Serializer.SerializationOptions;
	}

	public override bool HasChanges(IncludeExcludeSettings settings)
	{
		return base.HasChanges(settings)
			|| (WindowLocation?.HasChanges() ?? false);
	}

	public override void ResetHasChanges()
	{
		WindowLocation?.ResetHasChanges();
		base.ResetHasChanges();
	}

	protected override void FinalizeLoad()
	{
		WindowLocation ??= new WindowLocation();

		// Defaults when missing from older JSON (UseDarkMode removed).
		if (!Enum.IsDefined(typeof(ThemeMode), ThemeMode))
		{
			ThemeMode = ThemeMode.Dark;
		}

		if (!Enum.IsDefined(typeof(ThemeDensity), ThemeDensity))
		{
			ThemeDensity = ThemeDensity.Normal;
		}

		if (ThemeColor is ThemeColor.None or ThemeColor.Current)
		{
			ThemeColor = ThemeColor.Blue;
		}

		var skin = SampleThemeCatalog.Find(ThemeSkin);
		if (!string.Equals(ThemeSkin, skin.Name, StringComparison.Ordinal))
		{
			ThemeSkin = skin.Name;
		}

		ApplyTheme();
		base.FinalizeLoad();
	}

	protected override void OnPropertyChanged<TValue>(string propertyName, TValue oldValue, TValue newValue)
	{
		base.OnPropertyChanged(propertyName, oldValue, newValue);

		if ((propertyName == nameof(ThemeSkin))
			|| (propertyName == nameof(ThemeColor))
			|| (propertyName == nameof(ThemeMode))
			|| (propertyName == nameof(ThemeDensity)))
		{
			ApplyTheme();
		}
	}

	private CornerstoneTheme EnsureThemeSkin()
	{
		var application = Application.Current;
		if (application == null)
		{
			return null;
		}

		var skin = SampleThemeCatalog.Find(ThemeSkin);
		var styles = application.Styles;
		CornerstoneTheme current = null;
		var index = -1;
		var i = 0;
		foreach (var style in styles)
		{
			if (style is CornerstoneTheme found)
			{
				current = found;
				index = i;
				break;
			}

			i++;
		}

		if ((current != null) && (current.GetType() == skin.ThemeType))
		{
			return current;
		}

		var created = skin.Create();
		created.ThemeColor = ThemeColor;
		created.ThemeMode = ThemeMode;
		if (index >= 0)
		{
			styles[index] = created;
		}
		else
		{
			styles.Insert(0, created);
		}

		return created;
	}

	#endregion
}