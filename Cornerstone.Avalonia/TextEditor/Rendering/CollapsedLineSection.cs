#region References

using System.Diagnostics.CodeAnalysis;
using Cornerstone.Avalonia.TextEditor.Document;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Rendering;

/// <summary>
/// Represents a collapsed line section.
/// Use the Uncollapse() method to uncollapse the section.
/// </summary>
public sealed class CollapsedLineSection
{
	private readonly HeightTree _heightTree;

	#if DEBUG
	internal string Id;
	private static int _nextId;
	#else
	const string Id = "";
	#endif

	internal CollapsedLineSection(HeightTree heightTree, DocumentLine start, DocumentLine end)
	{
		_heightTree = heightTree;
		Start = start;
		End = end;
		#if DEBUG
		unchecked
		{
			Id = " #" + _nextId++;
		}
		#endif
	}

	/// <summary>
	/// Gets if the document line is collapsed.
	/// This property initially is true and turns to false when uncollapsing the section.
	/// </summary>
	public bool IsCollapsed => Start != null;

	/// <summary>
	/// Gets the start line of the section.
	/// When the section is uncollapsed or the text containing it is deleted,
	/// this property returns null.
	/// </summary>
	public DocumentLine Start { get; internal set; }

	/// <summary>
	/// Gets the end line of the section.
	/// When the section is uncollapsed or the text containing it is deleted,
	/// this property returns null.
	/// </summary>
	public DocumentLine End { get; internal set; }

	/// <summary>
	/// Uncollapses the section.
	/// This causes the Start and End properties to be set to null!
	/// Does nothing if the section is already uncollapsed.
	/// </summary>
	public void Uncollapse()
	{
		if (Start == null)
		{
			return;
		}

		if (!_heightTree.IsDisposed)
		{
			_heightTree.Uncollapse(this);
			#if DEBUG
			_heightTree.CheckProperties();
			#endif
		}

		Start = null;
		End = null;
	}

	/// <summary>
	/// Gets a string representation of the collapsed section.
	/// </summary>
	[SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
	public override string ToString()
	{
		return "[CollapsedSection" + Id + " Start=" + (Start != null ? Start.LineNumber.ToString() : "null")
			+ " End=" + (End != null ? End.LineNumber.ToString() : "null") + "]";
	}
}