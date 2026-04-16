#region References

using Cornerstone.Avalonia.Themes;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Serialization;
using Cornerstone.Settings;

#endregion

namespace Cornerstone.Sample;

[SourceReflection]
public partial class ApplicationSettings : SettingsFile<ApplicationSettings>
{
	#region Constructors

	/// <summary>
	/// Serialization use only
	/// </summary>
	public ApplicationSettings()
	{
	}

	[DependencyInjectionConstructor]
	public ApplicationSettings(IRuntimeInformation runtimeInformation)
		: base("ApplicationSettings.bson", runtimeInformation)
	{
		MainWindowLocation = new WindowLocation();
		ThemeColor = ThemeColor.Blue;
		UseDarkMode = true;
	}

	#endregion

	#region Properties

	[Notify]
	[Pack(1, 1)]
	[UpdateableAction(UpdateableAction.All)]
	public partial WindowLocation MainWindowLocation { get; set; }

	[Notify]
	[Pack(1, 4)]
	[UpdateableAction(UpdateableAction.All)]
	public partial string SelectedTab { get; set; }

	[Notify]
	[Pack(1, 2)]
	[UpdateableAction(UpdateableAction.All)]
	public partial ThemeColor ThemeColor { get; set; }

	[Notify]
	[Pack(1, 3)]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool UseDarkMode { get; set; }

	#endregion

	#region Methods

	public override bool HasNotifiableChanges(IncludeExcludeSettings settings)
	{
		return base.HasNotifiableChanges(settings)
			|| MainWindowLocation.HasNotifiableChanges();
	}

	public override void ResetHasChanges()
	{
		MainWindowLocation?.ResetHasChanges();
		base.ResetHasChanges();
	}

	#endregion
}