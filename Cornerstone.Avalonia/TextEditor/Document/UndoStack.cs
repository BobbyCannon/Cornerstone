﻿#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Cornerstone.Collections;
using Cornerstone.Internal;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Document;

/// <summary>
/// Undo stack implementation.
/// </summary>
[SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
public sealed class UndoStack : INotifyPropertyChanged
{
	#region Constants

	/// undo stack is listening for changes
	internal const int StateListen = 0;

	/// undo stack is reverting/repeating a set of changes
	internal const int StatePlayback = 1;

	// undo stack is reverting/repeating a set of changes and modifies the document to do this
	internal const int StatePlaybackModifyDocument = 2;

	#endregion

	#region Fields

	private int _actionCountInUndoGroup;

	private List<TextEditorDocument> _affectedDocuments;

	private bool _allowContinue;
	// implements feature request SD2-784 - File still considered dirty after undoing all changes

	/// <summary>
	/// Number of times undo must be executed until the original state is reached.
	/// Negative: number of times redo must be executed until the original state is reached.
	/// Special case: int.MinValue == original state is unreachable
	/// </summary>
	private int _elementsOnUndoUntilOriginalFile;

	private int _optionalActionCount;
	private readonly Deque<IUndoableOperation> _redostack = [];
	private int _sizeLimit = int.MaxValue;

	private int _undoGroupDepth;

	private readonly Deque<IUndoableOperation> _undostack = [];

	#endregion

	#region Properties

	/// <summary>
	/// Gets if the undo stack currently accepts changes.
	/// Is false while an undo action is running.
	/// </summary>
	public bool AcceptChanges => State == StateListen;

	/// <summary>
	/// Gets if there are actions on the redo stack.
	/// Use the PropertyChanged event to listen to changes of this property.
	/// </summary>
	public bool CanRedo => _redostack.Count > 0;

	/// <summary>
	/// Gets if there are actions on the undo stack.
	/// Use the PropertyChanged event to listen to changes of this property.
	/// </summary>
	public bool CanUndo => _undostack.Count > 0;

	/// <summary>
	/// Gets whether the document is currently in its original state (no modifications).
	/// </summary>
	public bool IsOriginalFile { get; private set; } = true;

	/// <summary>
	/// If an undo group is open, gets the group descriptor of the current top-level
	/// undo group.
	/// If no undo group is open, gets the group descriptor from the previous undo group.
	/// </summary>
	/// <remarks>
	/// The group descriptor can be used to join adjacent undo groups:
	/// use a group descriptor to mark your changes, and on the second action,
	/// compare LastGroupDescriptor and use <see cref="StartContinuedUndoGroup" /> if you
	/// want to join the undo groups.
	/// </remarks>
	public object LastGroupDescriptor { get; private set; }

	/// <summary>
	/// Gets/Sets the limit on the number of items on the undo stack.
	/// </summary>
	/// <remarks>
	/// The size limit is enforced only on the number of stored top-level undo groups.
	/// Elements within undo groups do not count towards the size limit.
	/// </remarks>
	public int SizeLimit
	{
		get => _sizeLimit;
		set
		{
			if (value < 0)
			{
				ThrowUtil.CheckNotNegative(value, "value");
			}
			if (_sizeLimit != value)
			{
				_sizeLimit = value;
				NotifyPropertyChanged("SizeLimit");
				if (_undoGroupDepth == 0)
				{
					EnforceSizeLimit();
				}
			}
		}
	}

	/// state is used for checking that noone but the UndoStack performs changes
	/// during Undo events
	internal int State { get; set; } = StateListen;

	#endregion

	#region Methods

	/// <summary>
	/// Clears both the undo and redo stack.
	/// </summary>
	public void ClearAll()
	{
		ThrowIfUndoGroupOpen();
		_actionCountInUndoGroup = 0;
		_optionalActionCount = 0;
		if (_undostack.Count != 0)
		{
			LastGroupDescriptor = null;
			_allowContinue = false;
			_undostack.Clear();
			NotifyPropertyChanged("CanUndo");
		}
		ClearRedoStack();
	}

	/// <summary>
	/// Call this method, if you want to clear the redo stack
	/// </summary>
	public void ClearRedoStack()
	{
		if (_redostack.Count != 0)
		{
			_redostack.Clear();
			NotifyPropertyChanged("CanRedo");
			// if the "original file" marker is on the redo stack: remove it
			if (_elementsOnUndoUntilOriginalFile < 0)
			{
				_elementsOnUndoUntilOriginalFile = int.MinValue;
			}
		}
	}

	/// <summary>
	/// Discards the current "original" marker.
	/// </summary>
	public void DiscardOriginalFileMarker()
	{
		_elementsOnUndoUntilOriginalFile = int.MinValue;
		RecalcIsOriginalFile();
	}

	/// <summary>
	/// Stops grouping changes.
	/// </summary>
	public void EndUndoGroup()
	{
		if (_undoGroupDepth == 0)
		{
			throw new InvalidOperationException("There are no open undo groups");
		}
		_undoGroupDepth--;
		//Util.LoggingService.Debug("Close undo group (new depth=" + undoGroupDepth + ")");
		if (_undoGroupDepth == 0)
		{
			Debug.Assert((State == StateListen) || (_actionCountInUndoGroup == 0));
			_allowContinue = true;
			if (_actionCountInUndoGroup == _optionalActionCount)
			{
				// only optional actions: don't store them
				for (var i = 0; i < _optionalActionCount; i++)
				{
					_undostack.PopEnd();
				}
				_allowContinue = false;
			}
			else if (_actionCountInUndoGroup > 1)
			{
				// combine all actions within the group into a single grouped action
				_undostack.PushEnd(new UndoOperationGroup(_undostack, _actionCountInUndoGroup));
				FileModified(-_actionCountInUndoGroup + 1 + _optionalActionCount);
			}
			//if (state == StateListen) {
			EnforceSizeLimit();
			RecalcIsOriginalFile(); // can raise event
			//}
		}
	}

	/// <summary>
	/// Marks the current state as original. Discards any previous "original" markers.
	/// </summary>
	public void MarkAsOriginalFile()
	{
		_elementsOnUndoUntilOriginalFile = 0;
		RecalcIsOriginalFile();
	}

	/// <summary>
	/// Call this method to push an UndoableOperation on the undostack.
	/// The redostack will be cleared if you use this method.
	/// </summary>
	public void Push(IUndoableOperation operation)
	{
		Push(operation, false);
	}

	/// <summary>
	/// Call this method to push an UndoableOperation on the undostack.
	/// However, the operation will be only stored if the undo group contains a
	/// non-optional operation.
	/// Use this method to store the caret position/selection on the undo stack to
	/// prevent having only actions that affect only the caret and not the document.
	/// </summary>
	public void PushOptional(IUndoableOperation operation)
	{
		if (_undoGroupDepth == 0)
		{
			throw new InvalidOperationException("Cannot use PushOptional outside of undo group");
		}
		Push(operation, true);
	}

	/// <summary>
	/// Call this method to redo the last undone operation
	/// </summary>
	public void Redo()
	{
		ThrowIfUndoGroupOpen();
		if (_redostack.Count > 0)
		{
			LastGroupDescriptor = null;
			_allowContinue = false;
			var uedit = _redostack.PopEnd();
			_undostack.PushEnd(uedit);
			State = StatePlayback;
			try
			{
				RunRedo(uedit);
			}
			finally
			{
				State = StateListen;
				FileModified(1);
				CallEndUpdateOnAffectedDocuments();
			}
			RecalcIsOriginalFile();
			if (_redostack.Count == 0)
			{
				NotifyPropertyChanged("CanRedo");
			}
			if (_undostack.Count == 1)
			{
				NotifyPropertyChanged("CanUndo");
			}
		}
	}

	/// <summary>
	/// Starts grouping changes, continuing with the previously closed undo group if possible.
	/// Maintains a counter so that nested calls are possible.
	/// If the call to StartContinuedUndoGroup is a nested call, it behaves exactly
	/// as <see cref="StartUndoGroup()" />, only top-level calls can continue existing undo groups.
	/// </summary>
	/// <param name="groupDescriptor">
	/// An object that is stored with the undo group.
	/// If this is not a top-level undo group, the parameter is ignored.
	/// </param>
	public void StartContinuedUndoGroup(object groupDescriptor = null)
	{
		if (_undoGroupDepth == 0)
		{
			_actionCountInUndoGroup = _allowContinue && (_undostack.Count > 0) ? 1 : 0;
			_optionalActionCount = 0;
			LastGroupDescriptor = groupDescriptor;
		}
		_undoGroupDepth++;
		//Util.LoggingService.Debug("Continue undo group (new depth=" + undoGroupDepth + ")");
	}

	/// <summary>
	/// Starts grouping changes.
	/// Maintains a counter so that nested calls are possible.
	/// </summary>
	public void StartUndoGroup()
	{
		StartUndoGroup(null);
	}

	/// <summary>
	/// Starts grouping changes.
	/// Maintains a counter so that nested calls are possible.
	/// </summary>
	/// <param name="groupDescriptor">
	/// An object that is stored with the undo group.
	/// If this is not a top-level undo group, the parameter is ignored.
	/// </param>
	public void StartUndoGroup(object groupDescriptor)
	{
		if (_undoGroupDepth == 0)
		{
			_actionCountInUndoGroup = 0;
			_optionalActionCount = 0;
			LastGroupDescriptor = groupDescriptor;
		}
		_undoGroupDepth++;
		//Util.LoggingService.Debug("Open undo group (new depth=" + undoGroupDepth + ")");
	}

	/// <summary>
	/// Call this method to undo the last operation on the stack
	/// </summary>
	public void Undo()
	{
		ThrowIfUndoGroupOpen();
		if (_undostack.Count > 0)
		{
			// disallow continuing undo groups after undo operation
			LastGroupDescriptor = null;
			_allowContinue = false;
			// fetch operation to undo and move it to redo stack
			var uedit = _undostack.PopEnd();
			_redostack.PushEnd(uedit);
			State = StatePlayback;
			try
			{
				RunUndo(uedit);
			}
			finally
			{
				State = StateListen;
				FileModified(-1);
				CallEndUpdateOnAffectedDocuments();
			}
			RecalcIsOriginalFile();
			if (_undostack.Count == 0)
			{
				NotifyPropertyChanged("CanUndo");
			}
			if (_redostack.Count == 1)
			{
				NotifyPropertyChanged("CanRedo");
			}
		}
	}

	internal void Push(TextEditorDocument document, DocumentChangeEventArgs e)
	{
		if (State == StatePlayback)
		{
			throw new InvalidOperationException("Document changes during undo/redo operations are not allowed.");
		}
		if (State == StatePlaybackModifyDocument)
		{
			State = StatePlayback; // allow only 1 change per expected modification
		}
		else
		{
			Push(new DocumentChangeOperation(document, e));
		}
	}

	internal void RegisterAffectedDocument(TextEditorDocument document)
	{
		if (_affectedDocuments == null)
		{
			_affectedDocuments = [];
		}
		if (!_affectedDocuments.Contains(document))
		{
			_affectedDocuments.Add(document);
			document.BeginUpdate();
		}
	}

	internal void RunRedo(IUndoableOperation op)
	{
		if (op is IUndoableOperationWithContext opWithCtx)
		{
			opWithCtx.Redo(this);
		}
		else
		{
			op.Redo();
		}
	}

	internal void RunUndo(IUndoableOperation op)
	{
		if (op is IUndoableOperationWithContext opWithCtx)
		{
			opWithCtx.Undo(this);
		}
		else
		{
			op.Undo();
		}
	}

	private void CallEndUpdateOnAffectedDocuments()
	{
		if (_affectedDocuments != null)
		{
			foreach (var doc in _affectedDocuments)
			{
				doc.EndUpdate();
			}
			_affectedDocuments = null;
		}
	}

	private void EnforceSizeLimit()
	{
		Debug.Assert(_undoGroupDepth == 0);
		while (_undostack.Count > _sizeLimit)
		{
			_undostack.PopFront();
		}
		while (_redostack.Count > _sizeLimit)
		{
			_redostack.PopFront();
		}
	}

	private void FileModified(int newElementsOnUndoStack)
	{
		if (_elementsOnUndoUntilOriginalFile == int.MinValue)
		{
			return;
		}

		_elementsOnUndoUntilOriginalFile += newElementsOnUndoStack;
		if (_elementsOnUndoUntilOriginalFile > _undostack.Count)
		{
			_elementsOnUndoUntilOriginalFile = int.MinValue;
		}

		// don't call RecalcIsOriginalFile(): wait until end of undo group
	}

	private void NotifyPropertyChanged(string propertyName)
	{
		var args = new PropertyChangedEventArgs(propertyName);
		PropertyChanged?.Invoke(this, args);
	}

	private void Push(IUndoableOperation operation, bool isOptional)
	{
		if (operation == null)
		{
			throw new ArgumentNullException(nameof(operation));
		}

		if ((State == StateListen) && (_sizeLimit > 0))
		{
			var wasEmpty = _undostack.Count == 0;

			var needsUndoGroup = _undoGroupDepth == 0;
			if (needsUndoGroup)
			{
				StartUndoGroup();
			}
			_undostack.PushEnd(operation);
			_actionCountInUndoGroup++;
			if (isOptional)
			{
				_optionalActionCount++;
			}
			else
			{
				FileModified(1);
			}
			if (needsUndoGroup)
			{
				EndUndoGroup();
			}
			if (wasEmpty)
			{
				NotifyPropertyChanged("CanUndo");
			}
			ClearRedoStack();
		}
	}

	private void RecalcIsOriginalFile()
	{
		var newIsOriginalFile = _elementsOnUndoUntilOriginalFile == 0;
		if (newIsOriginalFile != IsOriginalFile)
		{
			IsOriginalFile = newIsOriginalFile;
			NotifyPropertyChanged("IsOriginalFile");
		}
	}

	/// <summary>
	/// Throws an InvalidOperationException if an undo group is current open.
	/// </summary>
	private void ThrowIfUndoGroupOpen()
	{
		if (_undoGroupDepth != 0)
		{
			_undoGroupDepth = 0;
			throw new InvalidOperationException("No undo group should be open at this point");
		}
		if (State != StateListen)
		{
			throw new InvalidOperationException("This method cannot be called while an undo operation is being performed");
		}
	}

	#endregion

	#region Events

	/// <summary>
	/// Is raised when a property (CanUndo, CanRedo) changed.
	/// </summary>
	public event PropertyChangedEventHandler PropertyChanged;

	#endregion
}