#region References

using Cornerstone.Keystone.Lifecycle;

#endregion

namespace Cornerstone.Keystone;

/// <summary>
/// The engine is for managing a set of processors. It also owns the lifecycles of the bus and state
/// </summary>
/// <typeparam name="TBus"> The bus of the keystone. </typeparam>
/// <typeparam name="TState"> The state of the keystone. </typeparam>
public abstract class KeystoneEngine<TBus, TState> : LifecycleTracker
	where TBus : KeystoneBus
	where TState : KeystoneState
{
	#region Constructors

	protected KeystoneEngine(TBus bus, TState state)
	{
		State = state;
		Bus = bus;
	}

	#endregion

	#region Properties

	public TBus Bus { get; }
	public TState State { get; }

	#endregion
}