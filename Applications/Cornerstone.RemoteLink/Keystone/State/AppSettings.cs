#region References

using System;
using System.Text.Json;
using Cornerstone.Presentation.Theme;
using Cornerstone.Presentation.Theme.Theming;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Serialization;
using Cornerstone.Settings;

#endregion

namespace Cornerstone.RemoteLink.Keystone.State;

[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"], false)]
[DependencyInjected]
public partial class AppSettings : SettingsFile<AppSettings>
{
	#region Constructors

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
		WindowLocation = new WindowLocation { Width = 960, Height = 640 };
		VncHost = "127.0.0.1";
		VncPort = 5900;
		AdbPath = string.Empty;
		SelectedShellTab = "AirPlay";
	}

	#endregion

	#region Properties

	public partial string AdbPath { get; set; }

	public partial ThemeColor ThemeColor { get; set; }

	public partial ThemeDensity ThemeDensity { get; set; }

	public partial string SelectedShellTab { get; set; }

	public partial ThemeMode ThemeMode { get; set; }

	public partial string VncHost { get; set; }

	public partial int VncPort { get; set; }

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

		if (string.IsNullOrWhiteSpace(VncHost))
		{
			VncHost = "127.0.0.1";
		}

		if (VncPort is < 0 or > 65535)
		{
			VncPort = 5900;
		}

		if (string.IsNullOrWhiteSpace(SelectedShellTab))
		{
			SelectedShellTab = "AirPlay";
		}

		ApplyTheme();
		base.FinalizeLoad();
	}

	protected override void OnPropertyChanged<TValue>(string propertyName, TValue oldValue, TValue newValue)
	{
		base.OnPropertyChanged(propertyName, oldValue, newValue);

		if (propertyName is nameof(ThemeColor) or nameof(ThemeMode) or nameof(ThemeDensity))
		{
			ApplyTheme();
		}
	}

	#endregion
}