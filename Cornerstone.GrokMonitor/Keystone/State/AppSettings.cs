#region References

using System;
using System.Text.Json;
using Cornerstone.Avalonia;
using Cornerstone.Avalonia.Themes;
using Cornerstone.Data;
using Cornerstone.GrokMonitor.GrokUsage;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Serialization;
using Cornerstone.Settings;

#endregion

namespace Cornerstone.GrokMonitor.Keystone.State;

/// <summary>
/// Persisted shell settings for Grok Monitor (theme, session row heat).
/// </summary>
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
		ThemeColor = ThemeColor.Blue;
		ThemeMode = ThemeMode.Dark;
		ThemeDensity = ThemeDensity.Normal;
		SessionTokenHeatEnabled = true;
		SessionTokenHeatSoftTokens = GrokUsageAnalytics.TokenHeatSoftThreshold;
		SessionTokenHeatHotTokens = GrokUsageAnalytics.TokenHeatHotThreshold;
		WindowLocation = new WindowLocation { Width = 1440, Height = 1024 };
	}

	#endregion

	#region Properties

	/// <summary>
	/// When true, Sessions grid rows tint by total tokens (yellow → red).
	/// </summary>
	public partial bool SessionTokenHeatEnabled { get; set; }

	/// <summary>
	/// Total tokens at or above this use full heat (red). Must be greater than soft.
	/// </summary>
	public partial long SessionTokenHeatHotTokens { get; set; }

	/// <summary>
	/// Total tokens below this stay untinted; at this value heat starts (yellow).
	/// </summary>
	public partial long SessionTokenHeatSoftTokens { get; set; }

	public partial ThemeColor ThemeColor { get; set; }

	/// <summary>
	/// Compact / Normal / Large chrome and list text.
	/// </summary>
	public partial ThemeDensity ThemeDensity { get; set; }

	/// <summary>
	/// Dark / Light / Default (system).
	/// </summary>
	public partial ThemeMode ThemeMode { get; set; }

	/// <summary>
	/// Main window position, size, and maximized state.
	/// </summary>
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
			|| (WindowLocation?.HasChanges() ?? false);
	}

	public override void ResetHasChanges()
	{
		WindowLocation?.ResetHasChanges();
		base.ResetHasChanges();
	}

	/// <summary>
	/// Clamp heat thresholds so soft ≥ 0 and hot &gt; soft.
	/// </summary>
	public void SanitizeSessionTokenHeat()
	{
		if (SessionTokenHeatSoftTokens < 0)
		{
			SessionTokenHeatSoftTokens = 0;
		}

		if (SessionTokenHeatHotTokens <= SessionTokenHeatSoftTokens)
		{
			var minHot = SessionTokenHeatSoftTokens + 1;

			// Prefer restoring the default span when soft is at the default soft threshold.
			SessionTokenHeatHotTokens = SessionTokenHeatSoftTokens == GrokUsageAnalytics.TokenHeatSoftThreshold
				? GrokUsageAnalytics.TokenHeatHotThreshold
				: minHot;
			if (SessionTokenHeatHotTokens <= SessionTokenHeatSoftTokens)
			{
				SessionTokenHeatHotTokens = minHot;
			}
		}
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

		// Missing JSON fields deserialize as false/0 — treat as "use defaults".
		if ((SessionTokenHeatSoftTokens <= 0) && (SessionTokenHeatHotTokens <= 0))
		{
			SessionTokenHeatEnabled = true;
			SessionTokenHeatSoftTokens = GrokUsageAnalytics.TokenHeatSoftThreshold;
			SessionTokenHeatHotTokens = GrokUsageAnalytics.TokenHeatHotThreshold;
		}
		else
		{
			SanitizeSessionTokenHeat();
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

		if ((propertyName == nameof(SessionTokenHeatSoftTokens))
			|| (propertyName == nameof(SessionTokenHeatHotTokens)))
		{
			// Avoid re-entrancy loops: only fix invalid pairs.
			if ((SessionTokenHeatSoftTokens < 0)
				|| (SessionTokenHeatHotTokens <= SessionTokenHeatSoftTokens))
			{
				SanitizeSessionTokenHeat();
			}
		}
	}

	#endregion
}