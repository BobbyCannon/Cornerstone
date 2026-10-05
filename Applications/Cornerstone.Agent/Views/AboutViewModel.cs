#region References

using Cornerstone.Agent.Keystone;
using Cornerstone.Data;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Agent.Views;

[SourceReflection]
[DependencyInjected]
public partial class AboutViewModel : DispatchableViewModel
{
	#region Fields

	public static readonly string AssemblyName;
	private readonly AppState _state;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public AboutViewModel(AppBus bus, AppState state, IAppNavigator navigator)
	{
		_state = state;
		Bus = bus;
		Navigator = navigator;
		RuntimeInformation = new RuntimeInformationData();
	}

	static AboutViewModel()
	{
		AssemblyName = typeof(AboutViewModel).ToAssemblyName();
	}

	#endregion

	#region Properties

	public AppBus Bus { get; }
	public IAppNavigator Navigator { get; }
	public IRuntimeInformation RuntimeInformation { get; }

	#endregion

	#region Methods

	public override void InitializeLifecycle()
	{
		TrackProperties(_state.RuntimeInformation, RuntimeInformation);
		base.InitializeLifecycle();
	}

	#endregion
}
