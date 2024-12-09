namespace Cornerstone.Collections;

public interface IRange
{
	#region Properties

	/// <summary>
	/// The exclusive end index (StartIndex + Length).
	/// </summary>
	int EndIndex { get; }

	/// <summary>
	/// The length of the section.
	/// </summary>
	int Length { get; }

	/// <summary>
	/// The inclusive start index.
	/// </summary>
	int StartIndex { get; }

	#endregion
}