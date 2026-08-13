#region References

using Cornerstone.Avalonia;
using Cornerstone.GrokMonitor.Keystone;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.GrokMonitor.GrokUsage;

[SourceReflection]
public partial class GrokUsageTabView : CornerstoneUserControl<GrokUsageTabViewModel>
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
	/// Design-time DataContext for the Avalonia previewer (filled usage dashboard).
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

			return GrokUsageTabViewModel.CreateDesignSample(bus, state, GetDispatcher());
		}
		catch
		{
			return null;
		}
	}

	#endregion
}