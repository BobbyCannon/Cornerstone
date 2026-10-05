#region References

using System;
using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Presentation.Controls.Text.Models;

[SourceReflection]
public partial class UndoManager : CornerstoneObject
{
	#region Fields

	private CaretSnapshot[] _compoundAfter;
	private CaretSnapshot[] _compoundBefore;
	private List<TextDocumentChangedArgs> _compoundChanges;
	private int _compoundDepth;
	private List<int> _compoundMarks;
	private readonly TextEditorViewModel _textEditorViewModel;

	#endregion

	#region Constructors

	public UndoManager(TextEditorViewModel textEditorViewModel)
	{
		_textEditorViewModel = textEditorViewModel;

		Enabled = true;
		UndoStack = new SpeedyQueue<UndoUnit>(mode: QueueMode.LIFO);
		RedoStack = new SpeedyQueue<UndoUnit>(mode: QueueMode.LIFO);
	}

	#endregion

	#region Properties

	[Notify]
	public partial bool Enabled { get; set; }

	public bool IsEmpty => (UndoStack.Count == 0) && (RedoStack.Count == 0);

	[Notify]
	public partial bool IsProcessing { get; internal set; }

	public SpeedyQueue<UndoUnit> RedoStack { get; }

	public SpeedyQueue<UndoUnit> UndoStack { get; }

	#endregion

	#region Methods

	public void Add(TextDocumentChangedArgs args)
	{
		if (!Enabled || IsProcessing)
		{
			return;
		}

		if (_compoundDepth > 0)
		{
			_compoundChanges.Add(args);
			return;
		}

		UndoStack.Enqueue(new UndoUnit([args], null, null));
		RedoStack.Clear();
	}

	public bool CanRedo()
	{
		return Enabled && (RedoStack.Count > 0);
	}

	public bool CanUndo()
	{
		return Enabled && (UndoStack.Count > 0);
	}

	public void Clear()
	{
		UndoStack.Clear();
		RedoStack.Clear();
	}

	public void Redo()
	{
		if (!RedoStack.TryDequeue(out var unit) || (unit.Changes.Length == 0))
		{
			return;
		}

		ApplyUnit(unit, undo: false);
	}

	public void Undo()
	{
		if (!UndoStack.TryDequeue(out var unit) || (unit.Changes.Length == 0))
		{
			return;
		}

		ApplyUnit(unit, undo: true);
	}

	/// <summary>
	/// Adds a group of changes as a single atomic undo/redo unit.
	/// </summary>
	internal void AddCompound(TextDocumentChangedArgs[] changes)
	{
		if (!Enabled || (changes.Length == 0) || IsProcessing)
		{
			return;
		}

		if (_compoundDepth > 0)
		{
			_compoundChanges.AddRange(changes);
			return;
		}

		UndoStack.Enqueue(new UndoUnit(changes, _textEditorViewModel.Carets.Capture(), _textEditorViewModel.Carets.Capture()));
		RedoStack.Clear();
	}

	internal void BeginCompound()
	{
		if (!Enabled || IsProcessing)
		{
			return;
		}

		if (_compoundDepth == 0)
		{
			_compoundBefore = _textEditorViewModel.Carets.Capture();
			_compoundChanges = [];
			_compoundMarks = [];
		}

		_compoundMarks.Add(_compoundChanges.Count);
		_compoundDepth++;
	}

	internal void EndCompound()
	{
		if (_compoundDepth <= 0)
		{
			return;
		}

		_compoundDepth--;
		PopCompoundMark();
		if (_compoundDepth > 0)
		{
			return;
		}

		var changes = _compoundChanges;
		_compoundChanges = null;
		_compoundMarks = null;
		_compoundAfter = _textEditorViewModel.Carets.Capture();
		var before = _compoundBefore;
		_compoundBefore = null;

		if (!Enabled || (changes == null) || (changes.Count == 0))
		{
			return;
		}

		UndoStack.Enqueue(new UndoUnit(changes.ToArray(), before, _compoundAfter));
		_compoundAfter = null;
		RedoStack.Clear();
	}

	internal void CancelCompound()
	{
		if (_compoundDepth <= 0)
		{
			return;
		}

		_compoundDepth--;
		var mark = PopCompoundMark();
		if ((_compoundChanges != null) && (mark >= 0) && (_compoundChanges.Count > mark))
		{
			_compoundChanges.RemoveRange(mark, _compoundChanges.Count - mark);
		}

		if (_compoundDepth > 0)
		{
			return;
		}

		_compoundChanges = null;
		_compoundMarks = null;
		_compoundBefore = null;
		_compoundAfter = null;
	}

	private int PopCompoundMark()
	{
		if ((_compoundMarks == null) || (_compoundMarks.Count == 0))
		{
			return -1;
		}

		var index = _compoundMarks.Count - 1;
		var mark = _compoundMarks[index];
		_compoundMarks.RemoveAt(index);
		return mark;
	}

	private void ApplyUnit(UndoUnit unit, bool undo)
	{
		IsProcessing = true;
		var applied = false;
		var snapshot = _textEditorViewModel.Buffer.ToString();
		var carets = _textEditorViewModel.Carets.Capture();

		try
		{
			_textEditorViewModel.Caret.Selection.Reset();

			if (undo)
			{
				for (var i = unit.Changes.Length - 1; i >= 0; i--)
				{
					if (!TryApplyChange(unit.Changes[i], reverse: true))
					{
						return;
					}
				}

				if (unit.Before != null)
				{
					_textEditorViewModel.Carets.Restore(unit.Before);
				}
			}
			else
			{
				foreach (var change in unit.Changes)
				{
					if (!TryApplyChange(change, reverse: false))
					{
						return;
					}
				}

				if (unit.After != null)
				{
					_textEditorViewModel.Carets.Restore(unit.After);
				}
			}

			applied = true;
		}
		catch
		{
		}
		finally
		{
			if (!applied)
			{
				try
				{
					_textEditorViewModel.ReplaceBufferSilent(snapshot);
					_textEditorViewModel.Carets.Restore(carets);
				}
				catch
				{
					_textEditorViewModel.Carets.EnsureAllInDocument();
				}
			}

			IsProcessing = false;
			if (applied)
			{
				if (undo)
				{
					RedoStack.Enqueue(unit);
				}
				else
				{
					UndoStack.Enqueue(unit);
				}
			}
			else if (undo)
			{
				UndoStack.Enqueue(unit);
			}
			else
			{
				RedoStack.Enqueue(unit);
			}
		}
	}

	private bool TryApplyChange(TextDocumentChangedArgs change, bool reverse)
	{
		var text = change.Text ?? string.Empty;
		if (text.Length == 0)
		{
			return true;
		}

		var add = change.Type == TextDocumentChangeType.Add;
		if (reverse)
		{
			add = !add;
		}

		var before = _textEditorViewModel.DocumentLength;
		if (add)
		{
			_textEditorViewModel.Insert(change.Offset, text);
			_textEditorViewModel.Caret.Move(change.Offset + text.Length);
			return _textEditorViewModel.DocumentLength == (before + text.Length);
		}

		_textEditorViewModel.Caret.Move(change.Offset);
		_textEditorViewModel.RemoveAt(change.Offset, text.Length);
		return _textEditorViewModel.DocumentLength == (before - text.Length);
	}

	#endregion
}

public sealed class UndoUnit
{
	#region Constructors

	public UndoUnit(TextDocumentChangedArgs[] changes, CaretSnapshot[] before, CaretSnapshot[] after)
	{
		Changes = changes;
		Before = before;
		After = after;
	}

	#endregion

	#region Properties

	public CaretSnapshot[] After { get; }

	public CaretSnapshot[] Before { get; }

	public TextDocumentChangedArgs[] Changes { get; }

	#endregion
}
