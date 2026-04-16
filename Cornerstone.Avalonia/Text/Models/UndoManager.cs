#region References

using Cornerstone.Collections;
using Cornerstone.Data;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Avalonia.Text.Models;

[SourceReflection]
public partial class UndoManager : Notifiable
{
	#region Fields

	private readonly TextEditorViewModel _textEditorViewModel;

	#endregion

	#region Constructors

	public UndoManager(TextEditorViewModel textEditorViewModel)
	{
		_textEditorViewModel = textEditorViewModel;

		Enabled = true;
		UndoStack = new SpeedyQueue<TextDocumentChangedArgs[]>(mode: QueueMode.LIFO);
		RedoStack = new SpeedyQueue<TextDocumentChangedArgs[]>(mode: QueueMode.LIFO);
	}

	#endregion

	#region Properties

	[Notify]
	public partial bool Enabled { get; set; }

	public bool IsEmpty => (UndoStack.Count == 0) && (RedoStack.Count == 0);

	[Notify]
	public partial bool IsProcessing { get; internal set; }

	public SpeedyQueue<TextDocumentChangedArgs[]> RedoStack { get; }

	public SpeedyQueue<TextDocumentChangedArgs[]> UndoStack { get; }

	#endregion

	#region Methods

	public void Add(TextDocumentChangedArgs args)
	{
		if (!Enabled || IsProcessing)
		{
			return;
		}

		UndoStack.Enqueue([args]);
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
		if (!RedoStack.TryDequeue(out var changeList) || (changeList.Length == 0))
		{
			return;
		}

		IsProcessing = true;

		try
		{
			_textEditorViewModel.Caret.Selection.Reset();

			foreach (var change in changeList)
			{
				switch (change.Type)
				{
					case TextDocumentChangeType.Add:
					{
						_textEditorViewModel.Insert(change.Offset, change.Text);
						break;
					}
					case TextDocumentChangeType.Remove:
					{
						_textEditorViewModel.Caret.Move(change.Offset);
						_textEditorViewModel.RemoveAt(change.Offset, change.Text.Length);
						break;
					}
				}
			}
		}
		finally
		{
			IsProcessing = false;
			UndoStack.Enqueue(changeList);
		}
	}

	public void Undo()
	{
		if (!UndoStack.TryDequeue(out var changeList) || (changeList.Length == 0))
		{
			return;
		}

		IsProcessing = true;

		try
		{
			_textEditorViewModel.Caret.Selection.Reset();

			for (var i = changeList.Length - 1; i >= 0; i--)
			{
				var change = changeList[i];

				switch (change.Type)
				{
					case TextDocumentChangeType.Add:
					{
						_textEditorViewModel.RemoveAt(change.Offset, change.Text.Length);
						break;
					}
					case TextDocumentChangeType.Remove:
					{
						_textEditorViewModel.Caret.Move(change.Offset);
						_textEditorViewModel.Insert(change.Offset, change.Text);
						break;
					}
				}
			}
		}
		finally
		{
			IsProcessing = false;
			RedoStack.Enqueue(changeList);
		}
	}

	/// <summary>
	/// Adds a group of changes as a single atomic undo/redo unit.
	/// </summary>
	internal void AddCompound(TextDocumentChangedArgs[] changes)
	{
		if (!Enabled || (changes.Length == 0))
		{
			return;
		}

		UndoStack.Enqueue(changes);
		RedoStack.Clear();
	}

	#endregion
}