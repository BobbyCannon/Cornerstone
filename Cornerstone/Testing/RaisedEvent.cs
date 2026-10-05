namespace Cornerstone.Testing;

/// <summary>
/// The sender and arguments from an event that was expected to be raised.
/// </summary>
public sealed class RaisedEvent<T>
{
	#region Constructors

	public RaisedEvent(object sender, T arguments)
	{
		Sender = sender;
		Arguments = arguments;
	}

	#endregion

	#region Properties

	public T Arguments { get; }

	public object Sender { get; }

	#endregion
}
