#region References

using Cornerstone.Keystone.Lifecycle;

#endregion

namespace Cornerstone.Keystone;

/// <summary>
/// Processors only know about bus and state. Processors should communicate only through
/// the bus because they cannot know about each other.
/// </summary>
public abstract class KeystoneProcessor<TBus, TState> : LifecycleTracker
	where TBus : KeystoneBus
	where TState : KeystoneState
{
	#region Constructors

	protected KeystoneProcessor(TBus bus, TState state)
	{
		Bus = bus;
		State = state;
	}

	#endregion

	#region Properties

	public TBus Bus { get; }
	public TState State { get; }

	#endregion
}