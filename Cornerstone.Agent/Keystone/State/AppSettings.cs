#region References

using System.Text.Json;
using Cornerstone.Avalonia.Themes;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Serialization;
using Cornerstone.Settings;

#endregion

namespace Cornerstone.Agent.Keystone.State;

[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"], false)]
[DependencyInjected]
public partial class AppSettings : SettingsFile<AppSettings>, IAppSettings, IUpdateable<IAppSettings>
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
		: base("AppSettings.json", runtimeInformation)
	{
		RecurseModelDirectory = true;
		ThemeColor = ThemeColor.Red;
		UseDarkMode = true;
	}

	#endregion

	#region Properties

	public partial PresentationList<string> AllowedDirectories { get; set; }
	public partial string ModelDirectory { get; set; }
	public partial bool RecurseModelDirectory { get; set; }
	public partial string SelectedModel { get; set; }
	public partial ThemeColor ThemeColor { get; set; }
	public partial bool UseDarkMode { get; set; }
	public partial WindowLocation WindowLocation { get; set; }

	#endregion

	#region Methods

	public override JsonSerializerOptions GetSerializationSettings()
	{
		return Serializer.SerializationOptions;
	}

	public override bool HasChanges(IncludeExcludeSettings settings)
	{
		return base.HasChanges(settings)
			|| AllowedDirectories.HasChanges()
			|| WindowLocation.HasChanges();
	}

	public override void ResetHasChanges()
	{
		AllowedDirectories?.ResetHasChanges();
		WindowLocation?.ResetHasChanges();
		base.ResetHasChanges();
	}

	protected override void FinalizeLoad()
	{
		AllowedDirectories ??= new PresentationList<string>();
		WindowLocation ??= new WindowLocation();
		base.FinalizeLoad();
	}

	#endregion
}

public interface IAppSettings
{
	#region Properties

	PresentationList<string> AllowedDirectories { get; }
	string ModelDirectory { get; }
	bool RecurseModelDirectory { get; }
	string SelectedModel { get; }
	ThemeColor ThemeColor { get; }
	bool UseDarkMode { get; }
	WindowLocation WindowLocation { get; }

	#endregion
}