#region References

using System.Text.Json;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Themes;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
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
		ThemeColor = ThemeColor.Blue;
		ThemeMode = ThemeMode.Dark;
		ThemeDensity = ThemeDensity.Normal;
	}

	#endregion

	#region Properties

	public partial string SelectedTab { get; set; }

	public partial ThemeColor ThemeColor { get; set; }

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
	/// Push Color / Mode / Density onto the live <see cref="CornerstoneTheme" />.
	/// </summary>
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
			|| WindowLocation.HasChanges();
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
		if (!System.Enum.IsDefined(typeof(ThemeMode), ThemeMode))
		{
			ThemeMode = ThemeMode.Dark;
		}

		if (!System.Enum.IsDefined(typeof(ThemeDensity), ThemeDensity))
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