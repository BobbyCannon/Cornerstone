#region References

using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Theme.Theming;
using Cornerstone.Reflection;
using Cornerstone.Sample.Keystone.State;

#endregion

namespace Cornerstone.Sample;

[SourceReflection]
[Notifiable(["*"])]
public partial class ThemeSettingsViewModel : DispatchableViewModel
{
	#region Fields

	private readonly AppSettings _settings;

	#endregion

	#region Constructors

	public ThemeSettingsViewModel(AppSettings settings)
	{
		_settings = settings;
	}

	#endregion

	#region Properties

	[Notify]
	public partial ThemeDensity ThemeDensity { get; set; }

	#endregion

	#region Methods

	public override void InitializeLifecycle()
	{
		TrackProperties(_settings)
			.MapTwoWay(nameof(AppSettings.ThemeDensity), nameof(ThemeDensity));
		base.InitializeLifecycle();
	}

	#endregion
}