#region References

using Cornerstone.Agent.Keystone;
using Cornerstone.Agent.Views;
using Cornerstone.Extensions;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Agent;

[SourceReflection]
[DependencyInjected]
[DependencyInjected(typeof(IAppDispatcher))]
[DependencyInjected(typeof(IAppNavigator))]
public partial class AppViewModel : ApplicationViewModel
{
	#region Constructors

	[DependencyInjectionConstructor]
	public AppViewModel(
		AppState state, AppBus bus,
		IDependencyProvider dependencyProvider,
		IDispatcher dispatcher
	) : base(dependencyProvider, dispatcher, 120)
	{
		State = state;
		Bus = bus;

		RegisterViewModel<AboutViewModel>();
		RegisterViewModel<AgentViewModel>();
		RegisterViewModel<SettingsViewModel>();
	}

	#endregion

	#region Properties

	public AppBus Bus { get; }

	public AppState State { get; }

	#endregion

	#region Methods

	public override void StartLifecycle()
	{
		TryToSelectViewByModel(typeof(AgentViewModel).ToAssemblyName());
		base.StartLifecycle();
	}

	#endregion
}