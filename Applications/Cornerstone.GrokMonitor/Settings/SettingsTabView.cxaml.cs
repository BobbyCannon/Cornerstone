#region References

using Cornerstone.Presentation.Theme;
using Cornerstone.GrokMonitor.Keystone;
using Cornerstone.Presentation.Controls;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.GrokMonitor.Settings;

[SourceReflection]
public partial class SettingsTabView : UserControl<SettingsTabViewModel>
{
	#region Constructors

	[DependencyInjectionConstructor]
	public SettingsTabView()
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Design-time DataContext for the Cornerstone previewer.
	/// </summary>
	protected override SettingsTabViewModel CreateDesignData()
	{
		try
		{
			var state = GetInstance<AppState>();
			if (state == null)
			{
				return null;
			}

			return new SettingsTabViewModel(state.Settings);
		}
		catch
		{
			return null;
		}
	}

	#endregion
}