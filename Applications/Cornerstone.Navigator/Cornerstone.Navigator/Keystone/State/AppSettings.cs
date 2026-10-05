#region References

using System;
using System.Text.Json;
using Cornerstone.Data;
using Cornerstone.Navigator;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Theme;
using Cornerstone.Presentation.Theme.Theming;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Serialization;
using Cornerstone.Settings;

#endregion

namespace Cornerstone.Navigator.Keystone.State;

[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"], false)]
[DependencyInjected]
public partial class AppSettings : SettingsFile<AppSettings>
{
	#region Constructors

	/// <summary>
	/// Serialization use only.
	/// </summary>
	public AppSettings()
	{
	}

	[DependencyInjectionConstructor]
	public AppSettings(IRuntimeInformation runtimeInformation)
		: base("ApplicationSettings.json", runtimeInformation)
	{
		Favorites = new PresentationList<BrowserFavorite>();
		ThemeColor = ThemeColor.Blue;
		ThemeMode = ThemeMode.Dark;
		ThemeDensity = ThemeDensity.Normal;
		WindowLocation = new WindowLocation { Width = 1100, Height = 700 };
	}

	#endregion

	#region Properties

	public partial PresentationList<BrowserFavorite> Favorites { get; set; }

	public partial ThemeColor ThemeColor { get; set; }

	public partial ThemeDensity ThemeDensity { get; set; }

	public partial ThemeMode ThemeMode { get; set; }

	public partial WindowLocation WindowLocation { get; set; }

	#endregion

	#region Methods

	public void ApplyTheme()
	{
		var theme = Theme.GetCornerstoneTheme();
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
			|| (Favorites?.HasChanges() ?? false)
			|| (WindowLocation?.HasChanges() ?? false);
	}

	public override void ResetHasChanges()
	{
		Favorites?.ResetHasChanges();
		WindowLocation?.ResetHasChanges();
		base.ResetHasChanges();
	}

	protected override void FinalizeLoad()
	{
		Favorites ??= new PresentationList<BrowserFavorite>();
		WindowLocation ??= new WindowLocation { Width = 1100, Height = 700 };

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

		ApplyTheme();
		base.FinalizeLoad();
	}

	protected override void OnPropertyChanged<TValue>(string propertyName, TValue oldValue, TValue newValue)
	{
		base.OnPropertyChanged(propertyName, oldValue, newValue);

		if ((propertyName == nameof(ThemeColor))
			|| (propertyName == nameof(ThemeMode))
			|| (propertyName == nameof(ThemeDensity)))
		{
			ApplyTheme();
		}
	}

	#endregion
}