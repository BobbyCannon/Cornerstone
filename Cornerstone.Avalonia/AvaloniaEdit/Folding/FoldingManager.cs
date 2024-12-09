#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using Cornerstone.Avalonia.AvaloniaEdit.Editing;
using Cornerstone.Avalonia.AvaloniaEdit.Rendering;
using Cornerstone.Text.Document;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Folding;

/// <summary>
/// Stores a list of foldings for a specific TextView and TextDocument.
/// </summary>
public class FoldingManager
{
	#region Fields

	private readonly TextSegmentCollection<FoldingSection> _foldings;
	private bool _isFirstUpdate = true;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new FoldingManager instance.
	/// </summary>
	public FoldingManager(TextEditorDocument document)
	{
		Document = document ?? throw new ArgumentNullException(nameof(document));
		_foldings = [];
		Dispatcher.UIThread.VerifyAccess();
		TextDocumentWeakEventManager.Changed.AddHandler(document, OnDocumentChanged);
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets all foldings in this manager.
	/// The foldings are returned sorted by start offset;
	/// for multiple foldings at the same offset the order is undefined.
	/// </summary>
	public IEnumerable<FoldingSection> AllFoldings => _foldings;

	internal TextEditorDocument Document { get; }
	internal List<TextView> TextViews { get; } = [];

	#endregion

	#region Methods

	/// <summary>
	/// Removes all folding sections.
	/// </summary>
	public void Clear()
	{
		Dispatcher.UIThread.VerifyAccess();
		foreach (var s in _foldings)
		{
			s.IsFolded = false;
		}
		_foldings.Clear();
		Redraw();
	}

	/// <summary>
	/// Creates a folding for the specified text section.
	/// </summary>
	public FoldingSection CreateFolding(int startOffset, int endOffset)
	{
		if (startOffset >= endOffset)
		{
			throw new ArgumentException("startOffset must be less than endOffset");
		}
		if ((startOffset < 0) || (endOffset > Document.TextLength))
		{
			throw new ArgumentException("Folding must be within document boundary");
		}
		var fs = new FoldingSection(this, startOffset, endOffset);
		_foldings.Add(fs);
		Redraw(fs);
		return fs;
	}

	/// <summary>
	/// Gets all foldings that start exactly at <paramref name="startOffset" />.
	/// </summary>
	public ReadOnlyCollection<FoldingSection> GetFoldingsAt(int startOffset)
	{
		var result = new List<FoldingSection>();
		var fs = _foldings.FindFirstSegmentWithStartAfter(startOffset);
		while ((fs != null) && (fs.StartOffset == startOffset))
		{
			result.Add(fs);
			fs = _foldings.GetNextSegment(fs);
		}
		return new ReadOnlyCollection<FoldingSection>(result);
	}

	/// <summary>
	/// Gets all foldings that contain <paramref name="offset" />.
	/// </summary>
	public ReadOnlyCollection<FoldingSection> GetFoldingsContaining(int offset)
	{
		return _foldings.FindSegmentsContaining(offset);
	}

	/// <summary>
	/// Gets the first offset greater or equal to <paramref name="startOffset" /> where a folded folding starts.
	/// Returns -1 if there are no foldings after <paramref name="startOffset" />.
	/// </summary>
	public int GetNextFoldedFoldingStart(int startOffset)
	{
		var fs = _foldings.FindFirstSegmentWithStartAfter(startOffset);
		while ((fs != null) && !fs.IsFolded)
		{
			fs = _foldings.GetNextSegment(fs);
		}
		return fs?.StartOffset ?? -1;
	}

	/// <summary>
	/// Gets the first folding with a <see cref="TextRange.StartOffset" /> greater or equal to
	/// <paramref name="startOffset" />.
	/// Returns null if there are no foldings after <paramref name="startOffset" />.
	/// </summary>
	public FoldingSection GetNextFolding(int startOffset)
	{
		// TODO: returns the longest folding instead of any folding at the first position after startOffset
		return _foldings.FindFirstSegmentWithStartAfter(startOffset);
	}

	/// <summary>
	/// Adds Folding support to the specified text area.
	/// Warning: The folding manager is only valid for the text area's current document. The folding manager
	/// must be uninstalled before the text area is bound to a different document.
	/// </summary>
	/// <returns> The <see cref="FoldingManager" /> that manages the list of foldings inside the text area. </returns>
	public static FoldingManager Install(TextArea textArea)
	{
		if (textArea == null)
		{
			throw new ArgumentNullException(nameof(textArea));
		}
		return new FoldingManagerInstallation(textArea);
	}

	/// <summary>
	/// Removes a folding section from this manager.
	/// </summary>
	public void RemoveFolding(FoldingSection fs)
	{
		if (fs == null)
		{
			throw new ArgumentNullException(nameof(fs));
		}
		fs.IsFolded = false;
		_foldings.Remove(fs);
		Redraw(fs);
	}

	/// <summary>
	/// Uninstalls the folding manager.
	/// </summary>
	/// <exception cref="ArgumentException"> The specified manager was not created using <see cref="Install" />. </exception>
	public static void Uninstall(FoldingManager manager)
	{
		if (manager == null)
		{
			throw new ArgumentNullException(nameof(manager));
		}
		if (manager is FoldingManagerInstallation installation)
		{
			installation.Uninstall();
		}
		else
		{
			throw new ArgumentException("FoldingManager was not created using FoldingManager.Install");
		}
	}

	/// <summary>
	/// Updates the foldings in this <see cref="FoldingManager" /> using the given new foldings.
	/// This method will try to detect which new foldings correspond to which existing foldings; and will keep the state
	/// (<see cref="FoldingSection.IsFolded" />) for existing foldings.
	/// </summary>
	/// <param name="newFoldings"> The new set of foldings. These must be sorted by starting offset. </param>
	/// <param name="firstErrorOffset">
	/// The first position of a parse error. Existing foldings starting after
	/// this offset will be kept even if they don't appear in <paramref name="newFoldings" />.
	/// Use -1 for this parameter if there were no parse errors.
	/// </param>
	public void UpdateFoldings(IEnumerable<NewFolding> newFoldings, int firstErrorOffset)
	{
		if (newFoldings == null)
		{
			throw new ArgumentNullException(nameof(newFoldings));
		}

		if (firstErrorOffset < 0)
		{
			firstErrorOffset = int.MaxValue;
		}

		var oldFoldings = AllFoldings.ToArray();
		var oldFoldingIndex = 0;
		var previousStartOffset = 0;
		// merge new foldings into old foldings so that sections keep being collapsed
		// both oldFoldings and newFoldings are sorted by start offset
		foreach (var newFolding in newFoldings)
		{
			// ensure newFoldings are sorted correctly
			if (newFolding.StartOffset < previousStartOffset)
			{
				throw new ArgumentException("newFoldings must be sorted by start offset");
			}
			previousStartOffset = newFolding.StartOffset;

			if (newFolding.StartOffset == newFolding.EndIndex)
			{
				continue; // ignore zero-length foldings
			}

			// remove old foldings that were skipped
			while ((oldFoldingIndex < oldFoldings.Length) && (newFolding.StartOffset > oldFoldings[oldFoldingIndex].StartOffset))
			{
				RemoveFolding(oldFoldings[oldFoldingIndex++]);
			}
			FoldingSection section;
			// reuse current folding if its matching:
			if ((oldFoldingIndex < oldFoldings.Length) && (newFolding.StartOffset == oldFoldings[oldFoldingIndex].StartOffset))
			{
				section = oldFoldings[oldFoldingIndex++];
				section.Length = newFolding.EndIndex - newFolding.StartOffset;
			}
			else
			{
				// no matching current folding; create a new one:
				section = CreateFolding(newFolding.StartOffset, newFolding.EndIndex);
				// auto-close #regions only when opening the document
				if (_isFirstUpdate)
				{
					section.IsFolded = newFolding.DefaultClosed;
				}
				section.Tag = newFolding;
			}
			section.Title = newFolding.Name;
		}
		_isFirstUpdate = false;
		// remove all outstanding old foldings:
		while (oldFoldingIndex < oldFoldings.Length)
		{
			var oldSection = oldFoldings[oldFoldingIndex++];
			if (oldSection.StartOffset >= firstErrorOffset)
			{
				break;
			}
			RemoveFolding(oldSection);
		}
	}

	internal void AddToTextView(TextView textView)
	{
		if ((textView == null) || TextViews.Contains(textView))
		{
			throw new ArgumentException();
		}
		TextViews.Add(textView);
		foreach (var fs in _foldings)
		{
			if (fs.CollapsedSections != null)
			{
				Array.Resize(ref fs.CollapsedSections, TextViews.Count);
				fs.ValidateCollapsedLineSections();
			}
		}
	}

	internal void Redraw()
	{
		foreach (var textView in TextViews)
		{
			textView.Redraw();
		}
	}

	internal void Redraw(FoldingSection fs)
	{
		foreach (var textView in TextViews)
		{
			textView.Redraw(fs);
		}
	}

	internal void RemoveFromTextView(TextView textView)
	{
		var pos = TextViews.IndexOf(textView);
		if (pos < 0)
		{
			throw new ArgumentException();
		}
		TextViews.RemoveAt(pos);
		foreach (var fs in _foldings)
		{
			if (fs.CollapsedSections != null)
			{
				var c = new CollapsedLineSection[TextViews.Count];
				Array.Copy(fs.CollapsedSections, 0, c, 0, pos);
				fs.CollapsedSections[pos].Uncollapse();
				Array.Copy(fs.CollapsedSections, pos + 1, c, pos, c.Length - pos);
				fs.CollapsedSections = c;
			}
		}
	}

	private void OnDocumentChanged(object sender, DocumentChangeEventArgs e)
	{
		_foldings.UpdateOffsets(e);
		var newEndOffset = e.Offset + e.InsertionLength;
		// extend end offset to the end of the line (including delimiter)
		var endLine = Document.GetLineByOffset(newEndOffset);
		newEndOffset = endLine.StartIndex + endLine.TotalLength;
		foreach (var affectedFolding in _foldings.FindOverlappingSegments(e.Offset, newEndOffset - e.Offset))
		{
			if (affectedFolding.Length == 0)
			{
				RemoveFolding(affectedFolding);
			}
			else
			{
				affectedFolding.ValidateCollapsedLineSections();
			}
		}
	}

	#endregion

	#region Classes

	private sealed class FoldingManagerInstallation : FoldingManager
	{
		#region Fields

		private FoldingElementGenerator _generator;
		private FoldingMargin _margin;
		private TextArea _textArea;

		#endregion

		#region Constructors

		public FoldingManagerInstallation(TextArea textArea) : base(textArea.Document)
		{
			_textArea = textArea;
			_margin = new FoldingMargin { FoldingManager = this };
			_generator = new FoldingElementGenerator { FoldingManager = this };
			textArea.LeftMargins.Add(_margin);
			textArea.TextView.Services.AddService(typeof(FoldingManager), this);
			// HACK: folding only works correctly when it has highest priority
			textArea.TextView.ElementGenerators.Insert(0, _generator);
			textArea.Caret.PositionChanged += TextArea_Caret_PositionChanged;
		}

		#endregion

		#region Methods

		/*
		void DemoMode()
		{
			foldingGenerator = new FoldingElementGenerator() { FoldingManager = fm };
			foldingMargin = new FoldingMargin { FoldingManager = fm };
			foldingMarginBorder = new Border {
				Child = foldingMargin,
				Background = new LinearGradientBrush(Colors.White, Colors.Transparent, 0)
			};
			foldingMarginBorder.SizeChanged += UpdateTextViewClip;
			textEditor.TextArea.TextView.ElementGenerators.Add(foldingGenerator);
			textEditor.TextArea.LeftMargins.Add(foldingMarginBorder);
		}

		void UpdateTextViewClip(object sender, SizeChangedEventArgs e)
		{
			textEditor.TextArea.TextView.Clip = new RectangleGeometry(
				new Rect(-foldingMarginBorder.ActualWidth,
						0,
						textEditor.TextArea.TextView.ActualWidth + foldingMarginBorder.ActualWidth,
						textEditor.TextArea.TextView.ActualHeight));
		}
		*/

		public void Uninstall()
		{
			Clear();
			if (_textArea != null)
			{
				_textArea.Caret.PositionChanged -= TextArea_Caret_PositionChanged;
				_textArea.LeftMargins.Remove(_margin);
				_textArea.TextView.ElementGenerators.Remove(_generator);
				_textArea.TextView.Services.RemoveService(typeof(FoldingManager));
				_margin = null;
				_generator = null;
				_textArea = null;
			}
		}

		private void TextArea_Caret_PositionChanged(object sender, EventArgs e)
		{
			// Expand Foldings when Caret is moved into them.
			var caretOffset = _textArea.Caret.Offset;
			foreach (var s in GetFoldingsContaining(caretOffset))
			{
				if (s.IsFolded && (s.StartOffset < caretOffset) && (caretOffset < s.EndIndex))
				{
					s.IsFolded = false;
				}
			}
		}

		#endregion
	}

	#endregion
}