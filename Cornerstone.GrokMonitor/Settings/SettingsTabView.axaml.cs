#region References

using Cornerstone.Avalonia;
using Cornerstone.GrokMonitor.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.GrokMonitor.Settings;

[SourceReflection]
public partial class SettingsTabView : CornerstoneUserControl<SettingsTabViewModel>
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
	/// Design-time DataContext for the Avalonia previewer.
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