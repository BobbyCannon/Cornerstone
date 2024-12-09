namespace Cornerstone.Text;

/// <summary>
/// Represent the raw text section data. No calculations or dependencies involved.
/// </summary>
public class TextRangeData : ITextRange
{
	#region Properties

	/// <inheritdoc />
	public int EndIndex { get; set; }

	/// <inheritdoc />
	public int Length { get; set; }

	/// <inheritdoc />
	public int Remaining { get; set; }

	/// <inheritdoc />
	public int StartIndex { get; set; }

	#endregion
}