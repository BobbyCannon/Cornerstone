#region References

using Cornerstone.Collections;

#endregion

namespace Cornerstone.Presentation.Controls.Text.Folding;

/// <summary>
/// A proposed fold range produced by a strategy, used with UpdateFoldings.
/// </summary>
public class NewFolding : IRange
{
	#region Constructors

	public NewFolding() : this(0, 0)
	{
	}

	public NewFolding(int startOffset, int endOffset)
	{
		StartOffset = startOffset;
		EndOffset = endOffset;
	}

	#endregion

	#region Properties

	public bool DefaultClosed { get; set; }

	public int EndOffset { get; set; }

	public int Length => EndOffset - StartOffset;

	public string Name { get; set; }

	public int StartOffset { get; set; }

	#endregion
}
