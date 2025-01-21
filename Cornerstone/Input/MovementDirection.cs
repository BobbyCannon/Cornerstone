namespace Cornerstone.Input;

/// <summary>
/// Exposes constants that represent the move direction.
/// </summary>
public enum MovementDirection
{
	/// <summary>
	/// Represents no direction, for when there is no movement.
	/// </summary>
	None = 0,

	/// <summary>
	/// The movement is upwards.
	/// </summary>
	Up = 1,

	/// <summary>
	/// The movement is downwards.
	/// </summary>
	Down = 2,

	/// <summary>
	/// The movement is to the left.
	/// </summary>
	Left = 3,

	/// <summary>
	/// The movement is to the right.
	/// </summary>
	Right = 4
}