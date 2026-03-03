namespace Cornerstone.Text.CodeGenerators;

/// <summary>
/// The mode of the consumer.
/// </summary>
public enum CodeBuilderMode
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
	Array = 2,

	/// <summary>
	/// Represents property mode.
	/// </summary>
	Property = 3
}