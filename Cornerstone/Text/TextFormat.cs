namespace Cornerstone.Text;

/// <summary>
/// Represents formatting options.
/// </summary>
public enum TextFormat
{
	/// <summary>
	/// No special formatting is applied. This is the default.
	/// </summary>
	None = 0,

	/// <summary>
	/// Formatting where content will contain new lines.
	/// </summary>
	Indented = 1,

	/// <summary>
	/// Formatting where new lines will be replaced with a space.
	/// </summary>
	Spaced = 2
}