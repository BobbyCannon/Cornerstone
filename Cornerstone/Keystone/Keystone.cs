#region References

using Cornerstone.Keystone.Lifecycle;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Keystone;

/// <summary>
/// The keystone manages the lifecycle of all the members.
/// </summary>
/// <typeparam name="TBus"> The bus of the keystone. </typeparam>
/// <typeparam name="TState"> The state of the keystone. </typeparam>
/// <typeparam name="TEngine"> The engine of the keystone. </typeparam>
/// <typeparam name="TViewModel"> The viewmodel of the keystone. </typeparam>
public abstract class Keystone<TBus, TState, TEngine, TViewModel> : Keystone<TBus, TState, TEngine>
	where TBus : KeystoneBus
	where TState : KeystoneState
	where TEngine : KeystoneEngine<TBus, TState>
	where TViewModel : ILifecycle
{
	#region Constructors

	protected Keystone(TBus bus, TState state, TEngine engine, TViewModel viewModel, IDateTimeProvider dateTimeProvider)
		: base(bus, state, engine)
	{
		ViewModel = Track(viewModel);
	}

	#endregion

	#region Properties

	public TViewModel ViewModel { get; }

	#endregion
}

/// <summary>
/// The keystone manages the lifecycle of all the members.
/// </summary>
/// <typeparam name="TBus"> The bus of the keystone. </typeparam>
/// <typeparam name="TState"> The state of the keystone. </typeparam>
/// <typeparam name="TEngine"> The engine of the keystone. </typeparam>
public abstract class Keystone<TBus, TState, TEngine> : LifecycleTracker
	where TBus : KeystoneBus
	where TState : KeystoneState
	where TEngine : KeystoneEngine<TBus, TState>
{
	#region Constructors

	protected Keystone(TBus bus, TState state, TEngine engine)
	{
		// do not reorder these, state should initialize, load, start before bus.
		State = Track(state);
		Bus = Track(bus);
		Engine = Track(engine);
	}

	#endregion

	#region Properties

	public TBus Bus { get; }
	public TEngine Engine { get; }
	public TState State { get; }

	#endregion
}