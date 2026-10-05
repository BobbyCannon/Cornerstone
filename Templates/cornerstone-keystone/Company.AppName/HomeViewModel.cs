#region References

using Company.AppName.Keystone;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Company.AppName;

/// <summary>
/// Projects <see cref="AppState.Greeting" /> for the home view and publishes the greet message.
/// </summary>
[SourceReflection]
public partial class HomeViewModel : DispatchableViewModel
{
	#region Fields

	private readonly AppBus _bus;
	private readonly AppState _state;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public HomeViewModel(AppBus bus, AppState state)
	{
		_bus = bus;
		_state = state;
	}

	#endregion

	#region Properties

	[Notify]
	public partial string Greeting { get; private set; }

	#endregion

	#region Methods

	[RelayCommand]
	public void Greet()
	{
		_bus.Greeting.Greet();
	}

	public override void InitializeLifecycle()
	{
		TrackProperties(_state).MapOneWay(nameof(AppState.Greeting));
		base.InitializeLifecycle();
	}

	#endregion
}
