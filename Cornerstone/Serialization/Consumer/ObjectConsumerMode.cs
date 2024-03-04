namespace Cornerstone.Serialization.Consumer;

/// <summary>
/// The mode of the consumer.
/// </summary>
public enum ObjectConsumerMode
{
	/// <summary>
	/// Unknown state.
	/// </summary>
	Unknown = 0,

	/// <summary>
	/// Represents object mode.
	/// </summary>
	Object = 1,

	/// <summary>
	/// Represents array mode.
	/// </summary>
	Array = 2
}