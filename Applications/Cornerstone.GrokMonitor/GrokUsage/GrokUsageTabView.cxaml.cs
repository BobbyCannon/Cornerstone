#region References

using Cornerstone.GrokMonitor.Keystone;
using Cornerstone.Presentation.Controls;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage;

[SourceReflection]
public partial class GrokUsageTabView : UserControl<GrokUsageTabViewModel>
{
	#region Constructors

	[DependencyInjectionConstructor]
	public GrokUsageTabView()
	{
		InitializeComponent();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Design-time DataContext for the Cornerstone previewer (filled usage dashboard).
	/// </summary>
	protected override GrokUsageTabViewModel CreateDesignData()
	{
		try
		{
			var bus = GetInstance<AppBus>();
			var state = GetInstance<AppState>();
			if ((bus == null) || (state == null))
			{
				return null;
			}

			return GrokUsageTabViewModel.CreateDesignSample(bus, state, Dispatcher);
		}
		catch
		{
			return null;
		}
	}

	#endregion
}