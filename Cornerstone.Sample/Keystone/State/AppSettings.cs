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
		UseDarkMode = true;
	}

	#endregion

	#region Properties

	public partial string SelectedTab { get; set; }
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
		base.FinalizeLoad();
	}

	#endregion
}