#region References

using System.Diagnostics;
using Cornerstone.Avalonia.AvaloniaEdit.Rendering;
using Cornerstone.Avalonia.AvaloniaEdit.Utils;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Folding;

/// <summary>
/// A section that can be folded.
/// </summary>
public sealed class FoldingSection : TextRange
{
	#region Fields

	internal CollapsedLineSection[] CollapsedSections;
	private bool _isFolded;
	private readonly FoldingManager _manager;
	private string _title;

	#endregion

	#region Constructors

	internal FoldingSection(FoldingManager manager, int startOffset, int endOffset)
	{
		Debug.Assert(manager != null);
		_manager = manager;
		StartOffset = startOffset;
		Length = endOffset - startOffset;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets/sets if the section is folded.
	/// </summary>
	public bool IsFolded
	{
		get => _isFolded;
		set
		{
			if (_isFolded != value)
			{
				_isFolded = value;
				ValidateCollapsedLineSections(); // create/destroy CollapsedLineSection
				_manager.Redraw(this);
			}
		}
	}

	/// <summary>
	/// Gets/Sets an additional object associated with this folding section.
	/// </summary>
	public object Tag { get; set; }

	/// <summary>
	/// Gets the content of the collapsed lines as text.
	/// </summary>
	public string TextContent => _manager.Document.GetText(StartOffset, EndIndex - StartOffset);

	/// <summary>
	/// Gets/Sets the text used to display the collapsed version of the folding section.
	/// </summary>
	public string Title
	{
		get => _title;
		set
		{
			if (_title != value)
			{
				_title = value;
				if (IsFolded)
				{
					_manager.Redraw(this);
				}
			}
		}
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void OnSegmentChanged()
	{
		ValidateCollapsedLineSections();
		base.OnSegmentChanged();
		// don't redraw if the FoldingSection wasn't added to the FoldingManager's collection yet
		if (IsConnectedToCollection)
		{
			_manager.Redraw(this);
		}
	}

	internal void ValidateCollapsedLineSections()
	{
		if (!_isFolded)
		{
			RemoveCollapsedLineSection();
			return;
		}
		// It is possible that StartOffset/EndOffset get set to invalid values via the property setters in TextSegment,
		// so we coerce those values into the valid range.
		var startLine = _manager.Document.GetLineByOffset(StartOffset.CoerceValue(0, _manager.Document.TextLength));
		var endLine = _manager.Document.GetLineByOffset(EndIndex.CoerceValue(0, _manager.Document.TextLength));
		if (startLine == endLine)
		{
			RemoveCollapsedLineSection();
		}
		else
		{
			if (CollapsedSections == null)
			{
				CollapsedSections = new CollapsedLineSection[_manager.TextViews.Count];
			}
			// Validate collapsed line sections
			var startLinePlusOne = startLine.NextLine;
			for (var i = 0; i < CollapsedSections.Length; i++)
			{
				var collapsedSection = CollapsedSections[i];
				if ((collapsedSection == null) || (collapsedSection.Start != startLinePlusOne) || (collapsedSection.End != endLine))
				{
					// recreate this collapsed section
					if (collapsedSection != null)
					{
						//Debug.WriteLine("CollapsedLineSection validation - recreate collapsed section from " + startLinePlusOne + " to " + endLine);
						collapsedSection.Uncollapse();
					}
					CollapsedSections[i] = _manager.TextViews[i].CollapseLines(startLinePlusOne, endLine);
				}
			}
		}
	}

	private void RemoveCollapsedLineSection()
	{
		if (CollapsedSections != null)
		{
			foreach (var collapsedSection in CollapsedSections)
			{
				if (collapsedSection?.Start != null)
				{
					collapsedSection.Uncollapse();
				}
			}
			CollapsedSections = null;
		}
	}

	#endregion
}