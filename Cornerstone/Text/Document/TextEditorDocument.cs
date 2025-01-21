#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading;
using Cornerstone.Collections;
using Cornerstone.Internal;
using Cornerstone.Text.Buffers;

#endregion

namespace Cornerstone.Text.Document;

/// <summary>
/// Represents a text document.
/// </summary>
/// <remarks>
/// <b> Thread safety: </b>
/// <inheritdoc cref="VerifyAccess" />
/// <para> However, there is a single method that is thread-safe: <see cref="CreateSnapshot()" /> (and its overloads). </para>
/// </remarks>
public sealed class TextEditorDocument : ITextEditorDocument, INotifyPropertyChanged
{
	#region Fields

	private readonly TextAnchorTree _anchorTree;
	private int _beginUpdateCount;
	private readonly StringGapBuffer _buffer;
	private WeakReference _cachedText;
	private FileInfo _fileInfo;
	private bool _fireTextChanged;
	private bool _inDocumentChanging;
	private readonly LineManager _lineManager;
	private readonly ObservableCollection<ILineTracker> _lineTrackers;
	private readonly DocumentLineTree _lineTree;
	private readonly object _lockObject;
	private int _oldLineCount;
	private int _oldTextLength;
	private Thread _ownerThread;
	private IServiceProvider _serviceProvider;
	private UndoStack _undoStack;
	private readonly TextSourceVersionProvider _versionProvider;

	#endregion

	#region Constructors

	/// <summary>
	/// Create an empty text document.
	/// </summary>
	public TextEditorDocument() : this([])
	{
	}

	/// <summary>
	/// Create a new text document with the specified initial text.
	/// </summary>
	public TextEditorDocument(IEnumerable<char> initialText)
	{
		if (initialText == null)
		{
			throw new ArgumentNullException(nameof(initialText));
		}

		_lineTrackers = [];
		_lockObject = new();
		_versionProvider = new();
		_ownerThread = Thread.CurrentThread;
		_buffer = new StringGapBuffer(initialText);
		_lineTree = new DocumentLineTree(this);
		_lineManager = new LineManager(_lineTree, this);
		_lineTrackers.CollectionChanged += delegate { _lineManager.UpdateListOfLineTrackers(); };

		_anchorTree = new TextAnchorTree(this);
		_undoStack = new UndoStack();

		FireChangeEvents();
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public FileInfo FileInfo
	{
		get => _fileInfo;
		set
		{
			if (_fileInfo != value)
			{
				_fileInfo = value;
				OnFileNameChanged(EventArgs.Empty);
			}
		}
	}

	/// <summary>
	/// Gets if an update is running.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="BeginUpdate" />
	/// </remarks>
	public bool IsInUpdate
	{
		get
		{
			VerifyAccess();
			return _beginUpdateCount > 0;
		}
	}

	/// <summary>
	/// Gets the total number of lines in the document.
	/// Runtime: O(1).
	/// </summary>
	public int LineCount
	{
		get
		{
			VerifyAccess();
			return _lineTree.LineCount;
		}
	}

	/// <summary>
	/// Gets a read-only list of lines.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="DocumentLine" />
	/// </remarks>
	public IList<DocumentLine> Lines => _lineTree;

	/// <summary>
	/// Gets the list of <see cref="ILineTracker" />s attached to this document.
	/// You can add custom line trackers to this list.
	/// </summary>
	public IList<ILineTracker> LineTrackers
	{
		get
		{
			VerifyAccess();
			return _lineTrackers;
		}
	}

	/// <summary>
	/// Gets/Sets the text of the whole document.
	/// </summary>
	public string Text
	{
		get
		{
			VerifyAccess();
			var completeText = _cachedText?.Target as string;
			if (completeText == null)
			{
				completeText = _buffer.ToString();
				_cachedText = new WeakReference(completeText);
			}
			return completeText;
		}
		set
		{
			VerifyAccess();
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}
			Replace(0, _buffer.Count, value);
		}
	}

	/// <inheritdoc />
	public int TextLength
	{
		get
		{
			VerifyAccess();
			return _buffer.Count;
		}
	}

	/// <summary>
	/// Gets the <see cref="UndoStack" /> of the document.
	/// </summary>
	/// <remarks> This property can also be used to set the undo stack, e.g. for sharing a common undo stack between multiple documents. </remarks>
	public UndoStack UndoStack
	{
		get => _undoStack;
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException();
			}
			if (value != _undoStack)
			{
				_undoStack.ClearAll(); // first clear old undo stack, so that it can't be used to perform unexpected changes on this document
				// ClearAll() will also throw an exception when it's not safe to replace the undo stack (e.g. update is currently in progress)
				_undoStack = value;
				OnPropertyChanged("UndoStack");
			}
		}
	}

	/// <inheritdoc />
	public ITextSourceVersion Version => _versionProvider.CurrentVersion;

	internal IServiceProvider ServiceProvider
	{
		get
		{
			VerifyAccess();
			if (_serviceProvider == null)
			{
				var container = new ServiceContainer();
				container.AddService(this);
				container.AddService<ITextEditorDocument>(this);
				_serviceProvider = container;
			}
			return _serviceProvider;
		}
		set
		{
			VerifyAccess();
			_serviceProvider = value ?? throw new ArgumentNullException(nameof(value));
		}
	}

	#endregion

	#region Methods

	/// <summary>
	/// <para> Begins a group of document changes. </para>
	/// <para>
	/// Some events are suspended until EndUpdate is called, and the <see cref="UndoStack" /> will
	/// group all changes into a single action.
	/// </para>
	/// <para>
	/// Calling BeginUpdate several times increments a counter, only after the appropriate number
	/// of EndUpdate calls the events resume their work.
	/// </para>
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="Changing" />
	/// </remarks>
	public void BeginUpdate()
	{
		VerifyAccess();
		if (_inDocumentChanging)
		{
			throw new InvalidOperationException("Cannot change document within another document change.");
		}
		_beginUpdateCount++;
		if (_beginUpdateCount == 1)
		{
			_undoStack.StartUndoGroup();
			UpdateStarted?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <summary>
	/// Creates a new <see cref="TextAnchor" /> at the specified offset.
	/// </summary>
	/// <inheritdoc cref="TextAnchor" select="remarks|example" />
	public TextAnchor CreateAnchor(int offset)
	{
		VerifyAccess();
		if ((offset < 0) || (offset > _buffer.Count))
		{
			throw new ArgumentOutOfRangeException(nameof(offset), offset, "0 <= offset <= " + _buffer.Count.ToString(CultureInfo.InvariantCulture));
		}
		return _anchorTree.CreateAnchor(offset);
	}

	/// <inheritdoc />
	public TextReader CreateReader()
	{
		lock (_lockObject)
		{
			return new StringBufferTextReader(_buffer);
		}
	}

	/// <inheritdoc />
	public TextReader CreateReader(int offset, int length)
	{
		lock (_lockObject)
		{
			return new StringBufferTextReader(_buffer.SubString(offset, length));
		}
	}

	/// <summary>
	/// Creates a snapshot of the current text.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This method returns an immutable snapshot of the document, and may be safely called even when
	/// the document's owner thread is concurrently modifying the document.
	/// </para>
	/// <para>
	/// This special thread-safety guarantee is valid only for TextDocument.CreateSnapshot(), not necessarily for other
	/// classes implementing ITextSource.CreateSnapshot().
	/// </para>
	/// <para>
	/// </para>
	/// </remarks>
	public ITextSource CreateSnapshot()
	{
		lock (_lockObject)
		{
			return new StringGapBufferTextSource(_buffer, _versionProvider.CurrentVersion);
		}
	}

	/// <summary>
	/// Creates a snapshot of a part of the current text.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="CreateSnapshot()" />
	/// </remarks>
	public ITextSource CreateSnapshot(int offset, int length)
	{
		lock (_lockObject)
		{
			return new StringGapBufferTextSource(_buffer.SubString(offset, length));
		}
	}

	public bool EndsWith(char c)
	{
		return _buffer.EndsWith(c);
	}

	/// <summary>
	/// Ends a group of document changes.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="Changing" />
	/// </remarks>
	public void EndUpdate()
	{
		VerifyAccess();
		if (_inDocumentChanging)
		{
			throw new InvalidOperationException("Cannot end update within document change.");
		}
		if (_beginUpdateCount == 0)
		{
			throw new InvalidOperationException("No update is active.");
		}
		if (_beginUpdateCount == 1)
		{
			// fire change events inside the change group - event handlers might add additional
			// document changes to the change group
			FireChangeEvents();
			_undoStack.EndUndoGroup();
			_beginUpdateCount = 0;
			UpdateFinished?.Invoke(this, EventArgs.Empty);
		}
		else
		{
			_beginUpdateCount -= 1;
		}
	}

	/// <inheritdoc />
	public char GetCharAt(int offset)
	{
		DebugVerifyAccess(); // frequently called, so must be fast in release builds
		return _buffer[offset];
	}

	/// <summary>
	/// Gets a line by the line number: O(log n)
	/// </summary>
	public DocumentLine GetLineByNumber(int number)
	{
		VerifyAccess();
		if ((number < 1) || (number > _lineTree.LineCount))
		{
			throw new ArgumentOutOfRangeException(nameof(number), number, "Value must be between 1 and " + _lineTree.LineCount);
		}
		return _lineTree.GetByNumber(number);
	}

	/// <summary>
	/// Gets a document lines by offset.
	/// Runtime: O(log n)
	/// </summary>
	[SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
	public DocumentLine GetLineByOffset(int offset)
	{
		VerifyAccess();
		if ((offset < 0) || (offset > _buffer.Count))
		{
			throw new ArgumentOutOfRangeException(nameof(offset), offset, "0 <= offset <= " + _buffer.Count);
		}
		return _lineTree.GetByOffset(offset);
	}

	/// <summary>
	/// Gets the location from an offset.
	/// </summary>
	/// <seealso cref="GetOffset(TextLocation)" />
	public TextLocation GetLocation(int offset)
	{
		var line = GetLineByOffset(offset);
		return new TextLocation(line.LineNumber, (offset - line.StartIndex) + 1);
	}

	/// <summary>
	/// Gets the offset from a text location.
	/// </summary>
	/// <seealso cref="GetLocation" />
	public int GetOffset(TextLocation location)
	{
		return GetOffset(location.Line, location.Column);
	}

	/// <summary>
	/// Gets the offset from a text location.
	/// </summary>
	/// <seealso cref="GetLocation" />
	public int GetOffset(int line, int column)
	{
		var docLine = GetLineByNumber(line);
		if (column <= 0)
		{
			return docLine.StartIndex;
		}
		if (column > docLine.Length)
		{
			return docLine.EndIndex;
		}
		return (docLine.StartIndex + column) - 1;
	}

	/// <inheritdoc />
	public string GetText(int offset, int length)
	{
		VerifyAccess();
		return _buffer.SubString(offset, length);
	}

	/// <summary>
	/// Retrieves the text for a portion of the document.
	/// </summary>
	public string GetText(IRange range)
	{
		if (range == null)
		{
			throw new ArgumentNullException(nameof(range));
		}
		return GetText(range.StartIndex, range.Length);
	}

	/// <inheritdoc />
	public int IndexOf(char c, int startIndex, int count)
	{
		DebugVerifyAccess();
		return _buffer.IndexOf(c, startIndex, count);
	}

	/// <inheritdoc />
	public int IndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
	{
		DebugVerifyAccess();
		return _buffer.IndexOf(searchText, startIndex, count, comparisonType);
	}

	/// <inheritdoc />
	public int IndexOfAny(char[] anyOf, int startIndex, int count)
	{
		DebugVerifyAccess(); // frequently called (NewLineFinder), so must be fast in release builds
		return _buffer.IndexOfAny(anyOf, startIndex, count);
	}

	/// <summary>
	/// Inserts text.
	/// </summary>
	/// <param name="offset"> The offset at which the text is inserted. </param>
	/// <param name="text"> The new text. </param>
	/// <remarks>
	/// Anchors positioned exactly at the insertion offset will move according to their movement type.
	/// For AnchorMovementType.Default, they will move behind the inserted text.
	/// The caret will also move behind the inserted text.
	/// </remarks>
	public void Insert(int offset, string text)
	{
		Replace(offset, 0, new StringTextSource(text), null);
	}

	/// <summary>
	/// Inserts text.
	/// </summary>
	/// <param name="offset"> The offset at which the text is inserted. </param>
	/// <param name="text"> The new text. </param>
	/// <remarks>
	/// Anchors positioned exactly at the insertion offset will move according to their movement type.
	/// For AnchorMovementType.Default, they will move behind the inserted text.
	/// The caret will also move behind the inserted text.
	/// </remarks>
	public void Insert(int offset, ITextSource text)
	{
		Replace(offset, 0, text, null);
	}

	/// <summary>
	/// Inserts text.
	/// </summary>
	/// <param name="offset"> The offset at which the text is inserted. </param>
	/// <param name="text"> The new text. </param>
	/// <param name="defaultAnchorMovementType">
	/// Anchors positioned exactly at the insertion offset will move according to the anchor's movement type.
	/// For AnchorMovementType.Default, they will move according to the movement type specified by this parameter.
	/// The caret will also move according to the <paramref name="defaultAnchorMovementType" /> parameter.
	/// </param>
	public void Insert(int offset, string text, AnchorMovementType defaultAnchorMovementType)
	{
		if (defaultAnchorMovementType == AnchorMovementType.BeforeInsertion)
		{
			Replace(offset, 0, new StringTextSource(text), OffsetChangeMappingType.KeepAnchorBeforeInsertion);
		}
		else
		{
			Replace(offset, 0, new StringTextSource(text), null);
		}
	}

	/// <summary>
	/// Inserts text.
	/// </summary>
	/// <param name="offset"> The offset at which the text is inserted. </param>
	/// <param name="text"> The new text. </param>
	/// <param name="defaultAnchorMovementType">
	/// Anchors positioned exactly at the insertion offset will move according to the anchor's movement type.
	/// For AnchorMovementType.Default, they will move according to the movement type specified by this parameter.
	/// The caret will also move according to the <paramref name="defaultAnchorMovementType" /> parameter.
	/// </param>
	public void Insert(int offset, ITextSource text, AnchorMovementType defaultAnchorMovementType)
	{
		if (defaultAnchorMovementType == AnchorMovementType.BeforeInsertion)
		{
			Replace(offset, 0, text, OffsetChangeMappingType.KeepAnchorBeforeInsertion);
		}
		else
		{
			Replace(offset, 0, text, null);
		}
	}

	/// <inheritdoc />
	public int LastIndexOf(char c, int startIndex, int count)
	{
		DebugVerifyAccess();
		return _buffer.LastIndexOf(c, startIndex, count);
	}

	/// <inheritdoc />
	public int LastIndexOf(string searchText,  StringComparison comparisonType)
	{
		DebugVerifyAccess();
		return _buffer.LastIndexOf(searchText, 0, _buffer.Count, comparisonType);
	}
	
	/// <inheritdoc />
	public int LastIndexOf(string searchText, int startIndex, int count, StringComparison comparisonType)
	{
		DebugVerifyAccess();
		return _buffer.LastIndexOf(searchText, startIndex, count, comparisonType);
	}

	/// <summary>
	/// Removes text.
	/// </summary>
	public void Remove(IRange range)
	{
		Replace(range, string.Empty);
	}

	/// <summary>
	/// Removes text.
	/// </summary>
	/// <param name="offset"> Starting offset of the text to be removed. </param>
	/// <param name="length"> Length of the text to be removed. </param>
	public void Remove(int offset, int length)
	{
		Replace(offset, length, StringTextSource.Empty);
	}

	/// <summary>
	/// Replaces text.
	/// </summary>
	public void Replace(IRange range, string text)
	{
		if (range == null)
		{
			throw new ArgumentNullException(nameof(range));
		}
		Replace(range.StartIndex, range.Length, new StringTextSource(text), null);
	}

	/// <summary>
	/// Replaces text.
	/// </summary>
	public void Replace(IRange range, ITextSource text)
	{
		if (range == null)
		{
			throw new ArgumentNullException(nameof(range));
		}
		Replace(range.StartIndex, range.Length, text, null);
	}

	/// <summary>
	/// Replaces text.
	/// </summary>
	/// <param name="offset"> The starting offset of the text to be replaced. </param>
	/// <param name="length"> The length of the text to be replaced. </param>
	/// <param name="text"> The new text. </param>
	public void Replace(int offset, int length, string text)
	{
		Replace(offset, length, new StringTextSource(text), null);
	}

	/// <summary>
	/// Replaces text.
	/// </summary>
	/// <param name="offset"> The starting offset of the text to be replaced. </param>
	/// <param name="length"> The length of the text to be replaced. </param>
	/// <param name="text"> The new text. </param>
	public void Replace(int offset, int length, ITextSource text)
	{
		Replace(offset, length, text, null);
	}

	/// <summary>
	/// Replaces text.
	/// </summary>
	/// <param name="offset"> The starting offset of the text to be replaced. </param>
	/// <param name="length"> The length of the text to be replaced. </param>
	/// <param name="text"> The new text. </param>
	/// <param name="offsetChangeMappingType">
	/// The offsetChangeMappingType determines how offsets inside the old text are mapped to the new text.
	/// This affects how the anchors and segments inside the replaced region behave.
	/// </param>
	public void Replace(int offset, int length, string text, OffsetChangeMappingType offsetChangeMappingType)
	{
		Replace(offset, length, new StringTextSource(text), offsetChangeMappingType);
	}

	/// <summary>
	/// Replaces text.
	/// </summary>
	/// <param name="offset"> The starting offset of the text to be replaced. </param>
	/// <param name="length"> The length of the text to be replaced. </param>
	/// <param name="text"> The new text. </param>
	/// <param name="offsetChangeMappingType">
	/// The offsetChangeMappingType determines how offsets inside the old text are mapped to the new text.
	/// This affects how the anchors and segments inside the replaced region behave.
	/// </param>
	public void Replace(int offset, int length, ITextSource text, OffsetChangeMappingType offsetChangeMappingType)
	{
		if (text == null)
		{
			throw new ArgumentNullException(nameof(text));
		}
		// Please see OffsetChangeMappingType XML comments for details on how these modes work.
		switch (offsetChangeMappingType)
		{
			case OffsetChangeMappingType.Normal:
				Replace(offset, length, text, null);
				break;
			case OffsetChangeMappingType.KeepAnchorBeforeInsertion:
				Replace(offset, length, text, OffsetChangeMap.FromSingleElement(
					new OffsetChangeMapEntry(offset, length, text.TextLength, false, true)));
				break;
			case OffsetChangeMappingType.RemoveAndInsert:
				if ((length == 0) || (text.TextLength == 0))
				{
					// only insertion or only removal?
					// OffsetChangeMappingType doesn't matter, just use Normal.
					Replace(offset, length, text, null);
				}
				else
				{
					var map = new OffsetChangeMap(2)
					{
						new(offset, length, 0),
						new(offset, 0, text.TextLength)
					};
					map.Freeze();
					Replace(offset, length, text, map);
				}
				break;
			case OffsetChangeMappingType.CharacterReplace:
				if ((length == 0) || (text.TextLength == 0))
				{
					// only insertion or only removal?
					// OffsetChangeMappingType doesn't matter, just use Normal.
					Replace(offset, length, text, null);
				}
				else if (text.TextLength > length)
				{
					// look at OffsetChangeMappingType.CharacterReplace XML comments on why we need to replace
					// the last character
					var entry = new OffsetChangeMapEntry((offset + length) - 1, 1, (1 + text.TextLength) - length);
					Replace(offset, length, text, OffsetChangeMap.FromSingleElement(entry));
				}
				else if (text.TextLength < length)
				{
					var entry = new OffsetChangeMapEntry(offset + text.TextLength, length - text.TextLength, 0, true, false);
					Replace(offset, length, text, OffsetChangeMap.FromSingleElement(entry));
				}
				else
				{
					Replace(offset, length, text, OffsetChangeMap.Empty);
				}
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(offsetChangeMappingType), offsetChangeMappingType, "Invalid enum value");
		}
	}

	/// <summary>
	/// Replaces text.
	/// </summary>
	/// <param name="offset"> The starting offset of the text to be replaced. </param>
	/// <param name="length"> The length of the text to be replaced. </param>
	/// <param name="text"> The new text. </param>
	/// <param name="offsetChangeMap">
	/// The offsetChangeMap determines how offsets inside the old text are mapped to the new text.
	/// This affects how the anchors and segments inside the replaced region behave.
	/// If you pass null (the default when using one of the other overloads), the offsets are changed as
	/// in OffsetChangeMappingType.Normal mode.
	/// If you pass OffsetChangeMap.Empty, then everything will stay in its old place (OffsetChangeMappingType.CharacterReplace mode).
	/// The offsetChangeMap must be a valid 'explanation' for the document change. See <see cref="OffsetChangeMap.IsValidForDocumentChange" />.
	/// Passing an OffsetChangeMap to the Replace method will automatically freeze it to ensure the thread safety of the resulting
	/// DocumentChangeEventArgs instance.
	/// </param>
	public void Replace(int offset, int length, string text, OffsetChangeMap offsetChangeMap)
	{
		Replace(offset, length, new StringTextSource(text), offsetChangeMap);
	}

	/// <summary>
	/// Replaces text.
	/// </summary>
	/// <param name="offset"> The starting offset of the text to be replaced. </param>
	/// <param name="length"> The length of the text to be replaced. </param>
	/// <param name="text"> The new text. </param>
	/// <param name="offsetChangeMap">
	/// The offsetChangeMap determines how offsets inside the old text are mapped to the new text.
	/// This affects how the anchors and segments inside the replaced region behave.
	/// If you pass null (the default when using one of the other overloads), the offsets are changed as
	/// in OffsetChangeMappingType.Normal mode.
	/// If you pass OffsetChangeMap.Empty, then everything will stay in its old place (OffsetChangeMappingType.CharacterReplace mode).
	/// The offsetChangeMap must be a valid 'explanation' for the document change. See <see cref="OffsetChangeMap.IsValidForDocumentChange" />.
	/// Passing an OffsetChangeMap to the Replace method will automatically freeze it to ensure the thread safety of the resulting
	/// DocumentChangeEventArgs instance.
	/// </param>
	public void Replace(int offset, int length, ITextSource text, OffsetChangeMap offsetChangeMap)
	{
		text = text?.CreateSnapshot() ?? throw new ArgumentNullException(nameof(text));
		offsetChangeMap?.Freeze();

		// Ensure that all changes take place inside an update group.
		// Will also take care of throwing an exception if inDocumentChanging is set.
		BeginUpdate();
		try
		{
			// protect document change against corruption by other changes inside the event handlers
			_inDocumentChanging = true;
			try
			{
				// The range verification must wait until after the BeginUpdate() call because the document
				// might be modified inside the UpdateStarted event.
				ThrowIfRangeInvalid(offset, length);

				DoReplace(offset, length, text, offsetChangeMap);
			}
			finally
			{
				_inDocumentChanging = false;
			}
		}
		finally
		{
			EndUpdate();
		}
	}

	/// <summary>
	/// Immediately calls <see cref="BeginUpdate()" />,
	/// and returns an IDisposable that calls <see cref="EndUpdate()" />.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="BeginUpdate" />
	/// </remarks>
	public IDisposable RunUpdate()
	{
		BeginUpdate();
		return new CallbackOnDispose(EndUpdate);
	}

	/// <summary>
	/// Transfers ownership of the document to another thread.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The owner can be set to null, which means that no thread can access the document. But, if the document
	/// has no owner thread, any thread may take ownership by calling <see cref="SetOwnerThread" />.
	/// </para>
	/// </remarks>
	public void SetOwnerThread(Thread newOwner)
	{
		// We need to lock here to ensure that in the null owner case,
		// only one thread succeeds in taking ownership.
		lock (_lockObject)
		{
			if (_ownerThread != null)
			{
				VerifyAccess();
			}
			_ownerThread = newOwner;
		}
	}

	/// <inheritdoc />
	public void WriteTextTo(TextWriter writer)
	{
		VerifyAccess();
		_buffer.WriteTo(writer, 0, _buffer.Count);
	}

	/// <inheritdoc />
	public void WriteTextTo(TextWriter writer, int offset, int length)
	{
		VerifyAccess();
		_buffer.WriteTo(writer, offset, length);
	}

	[Conditional("DEBUG")]
	internal void DebugVerifyAccess()
	{
		VerifyAccess();
	}

	/// <summary>
	/// Fires TextChanged, TextLengthChanged, LineCountChanged if required.
	/// </summary>
	internal void FireChangeEvents()
	{
		// it may be necessary to fire the event multiple times if the document is changed
		// from inside the event handlers
		while (_fireTextChanged)
		{
			_fireTextChanged = false;
			TextChanged?.Invoke(this, EventArgs.Empty);
			OnPropertyChanged("Text");

			var textLength = _buffer.Count;
			if (textLength != _oldTextLength)
			{
				_oldTextLength = textLength;
				TextLengthChanged?.Invoke(this, EventArgs.Empty);
				OnPropertyChanged("TextLength");
			}
			var lineCount = _lineTree.LineCount;
			if (lineCount != _oldLineCount)
			{
				_oldLineCount = lineCount;
				LineCountChanged?.Invoke(this, EventArgs.Empty);
				OnPropertyChanged("LineCount");
			}
		}
	}

	/// <summary>
	/// Gets the document lines tree in string form.
	/// </summary>
	[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
	[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
	internal string GetLineTreeAsString()
	{
		#if DEBUG
		return _lineTree.GetTreeAsString();
		#else
		return "Not available in release build.";
		#endif
	}

	/// <summary>
	/// Gets the text anchor tree in string form.
	/// </summary>
	[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
	[SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
	internal string GetTextAnchorTreeAsString()
	{
		#if DEBUG
		return _anchorTree.GetTreeAsString();
		#else
		return "Not available in release build.";
		#endif
	}

	ITextAnchor ITextEditorDocument.CreateAnchor(int offset)
	{
		return CreateAnchor(offset);
	}

	private void DoReplace(int offset, int length, ITextSource newText, OffsetChangeMap offsetChangeMap)
	{
		if ((length == 0) && (newText.TextLength == 0))
		{
			return;
		}

		// trying to replace a single character in 'Normal' mode?
		// for single characters, 'CharacterReplace' mode is equivalent, but more performant
		// (we don't have to touch the anchorTree at all in 'CharacterReplace' mode)
		if ((length == 1) && (newText.TextLength == 1) && (offsetChangeMap == null))
		{
			offsetChangeMap = OffsetChangeMap.Empty;
		}

		ITextSource removedText;
		if (length == 0)
		{
			removedText = StringTextSource.Empty;
		}
		else if (length < 100)
		{
			removedText = new StringTextSource(_buffer.SubString(offset, length));
		}
		else
		{
			// use a rope if the removed string is long
			removedText = new StringGapBufferTextSource(_buffer.SubString(offset, length));
		}
		var args = new DocumentChangeEventArgs(offset, removedText, newText, offsetChangeMap);

		// fire DocumentChanging event
		Changing?.Invoke(this, args);
		TextChangingInternal?.Invoke(this, args);

		_undoStack.Push(this, args);

		_cachedText = null; // reset cache of complete document text
		_fireTextChanged = true;
		var delayedEvents = new DelayedEvents();

		lock (_lockObject)
		{
			// create linked list of checkpoints
			_versionProvider.AppendChange(args);

			// now update the textBuffer and lineTree
			if ((offset == 0) && (length == _buffer.Count))
			{
				// optimize replacing the whole document
				_buffer.Clear();
				_buffer.Insert(0, newText.Text);
				_lineManager.Rebuild();
			}
			else
			{
				_buffer.Remove(offset, length);
				_lineManager.Remove(offset, length);
				#if DEBUG
				_lineTree.CheckProperties();
				#endif
				_buffer.Insert(offset, newText.Text);
				_lineManager.Insert(offset, newText);
				#if DEBUG
				_lineTree.CheckProperties();
				#endif
			}
		}

		// update text anchors
		if (offsetChangeMap == null)
		{
			_anchorTree.HandleTextChange(args.CreateSingleChangeMapEntry(), delayedEvents);
		}
		else
		{
			foreach (var entry in offsetChangeMap)
			{
				_anchorTree.HandleTextChange(entry, delayedEvents);
			}
		}

		_lineManager.ChangeComplete(args);

		// raise delayed events after our data structures are consistent again
		delayedEvents.RaiseEvents();

		// fire DocumentChanged event
		Changed?.Invoke(this, args);
		TextChangedInternal?.Invoke(this, args);
	}

	void ITextEditorDocument.EndUndoableAction()
	{
		EndUpdate();
	}

	IDocumentLine ITextEditorDocument.GetLineByNumber(int lineNumber)
	{
		return GetLineByNumber(lineNumber);
	}

	IDocumentLine ITextEditorDocument.GetLineByOffset(int offset)
	{
		return GetLineByOffset(offset);
	}

	object IServiceProvider.GetService(Type serviceType)
	{
		return ServiceProvider.GetService(serviceType);
	}

	// gets the text from a text source, directly retrieving the underlying rope where possible
	private static IEnumerable<char> GetTextFromTextSource(ITextSource textSource)
	{
		if (textSource == null)
		{
			throw new ArgumentNullException(nameof(textSource));
		}

		if (textSource is StringGapBufferTextSource gap)
		{
			return gap.Text;
		}

		if (textSource is TextEditorDocument doc)
		{
			return doc._buffer;
		}

		return textSource.Text.ToCharArray();
	}

	private void OnFileNameChanged(EventArgs e)
	{
		FileNameChanged?.Invoke(this, e);
	}

	private void OnPropertyChanged(string propertyName)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	IDisposable ITextEditorDocument.OpenUndoGroup()
	{
		return RunUpdate();
	}

	void ITextEditorDocument.StartUndoableAction()
	{
		BeginUpdate();
	}

	private void ThrowIfRangeInvalid(int offset, int length)
	{
		if ((offset < 0) || (offset > _buffer.Count))
		{
			throw new ArgumentOutOfRangeException(nameof(offset), offset, "0 <= offset <= " + _buffer.Count.ToString(CultureInfo.InvariantCulture));
		}
		if ((length < 0) || ((offset + length) > _buffer.Count))
		{
			throw new ArgumentOutOfRangeException(nameof(length), length, "0 <= length, offset(" + offset + ")+length <= " + _buffer.Count.ToString(CultureInfo.InvariantCulture));
		}
	}

	private void VerifyAccess()
	{
		if (Thread.CurrentThread != _ownerThread)
		{
			throw new InvalidOperationException("Call from invalid thread.");
		}
	}

	#endregion

	#region Events

	/// <summary>
	/// Is raised after the document has changed.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="Changing" />
	/// </remarks>
	public event EventHandler<DocumentChangeEventArgs> Changed;

	/// <summary>
	/// Is raised before the document changes.
	/// </summary>
	/// <remarks>
	/// <para> Here is the order in which events are raised during a document update: </para>
	/// <list type="bullet">
	/// <item>
	/// <description>
	/// <b>
	/// <see cref="BeginUpdate"> BeginUpdate() </see>
	/// </b>
	/// </description>
	/// <list type="bullet">
	/// <item>
	/// <description> Start of change group (on undo stack) </description>
	/// </item>
	/// <item>
	/// <description> <see cref="UpdateStarted" /> event is raised </description>
	/// </item>
	/// </list>
	/// </item>
	/// <item>
	/// <description>
	/// <b> <see cref="Insert(int,string)"> Insert() </see> / <see cref="Remove(int,int)"> Remove() </see> / <see cref="Replace(int,int,string)"> Replace() </see> </b>
	/// </description>
	/// <list type="bullet">
	/// <item>
	/// <description> <see cref="Changing" /> event is raised </description>
	/// </item>
	/// <item>
	/// <description> The document is changed </description>
	/// </item>
	/// <item>
	/// <description>
	/// <see cref="TextAnchor.Deleted"> TextAnchor.Deleted </see> event is raised if anchors were
	/// in the deleted text portion
	/// </description>
	/// </item>
	/// <item>
	/// <description> <see cref="Changed" /> event is raised </description>
	/// </item>
	/// </list>
	/// </item>
	/// <item>
	/// <description>
	/// <b>
	/// <see cref="EndUpdate"> EndUpdate() </see>
	/// </b>
	/// </description>
	/// <list type="bullet">
	/// <item>
	/// <description> <see cref="TextChanged" /> event is raised </description>
	/// </item>
	/// <item>
	/// <description> <see cref="PropertyChanged" /> event is raised (for the Text, TextLength, LineCount properties, in that order) </description>
	/// </item>
	/// <item>
	/// <description> End of change group (on undo stack) </description>
	/// </item>
	/// <item>
	/// <description> <see cref="UpdateFinished" /> event is raised </description>
	/// </item>
	/// </list>
	/// </item>
	/// </list>
	/// <para>
	/// If the insert/remove/replace methods are called without a call to <c> BeginUpdate() </c>,
	/// they will call <c> BeginUpdate() </c> and <c> EndUpdate() </c> to ensure no change happens outside of <c> UpdateStarted </c>/<c> UpdateFinished </c>.
	/// </para>
	/// <para>
	/// There can be multiple document changes between the <c> BeginUpdate() </c> and <c> EndUpdate() </c> calls.
	/// In this case, the events associated with EndUpdate will be raised only once after the whole document update is done.
	/// </para>
	/// <para>
	/// The <see cref="UndoStack" /> listens to the <c> UpdateStarted </c> and <c> UpdateFinished </c> events to group all changes into a single undo step.
	/// </para>
	/// </remarks>
	public event EventHandler<DocumentChangeEventArgs> Changing;

	/// <inheritdoc />
	public event EventHandler FileNameChanged;

	/// <summary>
	/// Is raised when the LineCount property changes.
	/// </summary>
	public event EventHandler LineCountChanged;

	public event PropertyChangedEventHandler PropertyChanged;

	/// <summary>
	/// This event is called after a group of changes is completed.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="Changing" />
	/// </remarks>
	public event EventHandler TextChanged;

	/// <summary>
	/// Is raised when the TextLength property changes.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="Changing" />
	/// </remarks>
	public event EventHandler TextLengthChanged;

	/// <summary>
	/// Occurs when a document change is finished.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="Changing" />
	/// </remarks>
	public event EventHandler UpdateFinished;

	/// <summary>
	/// Occurs when a document change starts.
	/// </summary>
	/// <remarks>
	/// <inheritdoc cref="Changing" />
	/// </remarks>
	public event EventHandler UpdateStarted;

	event EventHandler ITextEditorDocument.ChangeCompleted
	{
		add => TextChanged += value;
		remove => TextChanged -= value;
	}

	event EventHandler<TextChangeEventArgs> ITextEditorDocument.TextChanged
	{
		add => TextChangedInternal += value;
		remove => TextChangedInternal -= value;
	}

	private event EventHandler<TextChangeEventArgs> TextChangedInternal;

	event EventHandler<TextChangeEventArgs> ITextEditorDocument.TextChanging
	{
		add => TextChangingInternal += value;
		remove => TextChangingInternal -= value;
	}

	// Unfortunately EventHandler<T> is invariant, so we have to use two separate events
	private event EventHandler<TextChangeEventArgs> TextChangingInternal;

	#endregion
}

/// <summary>
/// A document representing a source code file for refactoring.
/// Line and column counting starts at 1.
/// Offset counting starts at 0.
/// </summary>
public interface ITextEditorDocument : ITextSource, IServiceProvider
{
	#region Properties

	/// <summary>
	/// Gets the info of the file the document is stored in.
	/// Could also be a non-existent dummy file name or null if no name has been set.
	/// </summary>
	FileInfo FileInfo { get; }

	/// <summary>
	/// Gets the number of lines in the document.
	/// </summary>
	int LineCount { get; }

	/// <summary>
	/// Gets/Sets the text of the whole document.
	/// </summary>
	new string Text { get; set; } // hides ITextSource.Text to add the setter

	#endregion

	#region Methods

	/// <summary>
	/// Creates a new <see cref="ITextAnchor" /> at the specified offset.
	/// </summary>
	/// <inheritdoc cref="ITextAnchor" select="remarks|example" />
	ITextAnchor CreateAnchor(int offset);

	/// <summary>
	/// Ends the undoable action started with <see cref="StartUndoableAction" />.
	/// </summary>
	void EndUndoableAction();

	/// <summary>
	/// Gets the document line with the specified number.
	/// </summary>
	/// <param name="lineNumber"> The number of the line to retrieve. The first line has number 1. </param>
	IDocumentLine GetLineByNumber(int lineNumber);

	/// <summary>
	/// Gets the document line that contains the specified offset.
	/// </summary>
	IDocumentLine GetLineByOffset(int offset);

	/// <summary>
	/// Gets the location from an offset.
	/// </summary>
	/// <seealso cref="GetOffset(TextLocation)" />
	TextLocation GetLocation(int offset);

	/// <summary>
	/// Gets the offset from a text location.
	/// </summary>
	/// <seealso cref="GetLocation" />
	int GetOffset(int line, int column);

	/// <summary>
	/// Gets the offset from a text location.
	/// </summary>
	/// <seealso cref="GetLocation" />
	int GetOffset(TextLocation location);

	/// <summary>
	/// Inserts text.
	/// </summary>
	/// <param name="offset"> The offset at which the text is inserted. </param>
	/// <param name="text"> The new text. </param>
	/// <remarks>
	/// Anchors positioned exactly at the insertion offset will move according to their movement type.
	/// For AnchorMovementType.Default, they will move behind the inserted text.
	/// The caret will also move behind the inserted text.
	/// </remarks>
	void Insert(int offset, string text);

	/// <summary>
	/// Inserts text.
	/// </summary>
	/// <param name="offset"> The offset at which the text is inserted. </param>
	/// <param name="text"> The new text. </param>
	/// <remarks>
	/// Anchors positioned exactly at the insertion offset will move according to their movement type.
	/// For AnchorMovementType.Default, they will move behind the inserted text.
	/// The caret will also move behind the inserted text.
	/// </remarks>
	void Insert(int offset, ITextSource text);

	/// <summary>
	/// Inserts text.
	/// </summary>
	/// <param name="offset"> The offset at which the text is inserted. </param>
	/// <param name="text"> The new text. </param>
	/// <param name="defaultAnchorMovementType">
	/// Anchors positioned exactly at the insertion offset will move according to the anchor's movement type.
	/// For AnchorMovementType.Default, they will move according to the movement type specified by this parameter.
	/// The caret will also move according to the <paramref name="defaultAnchorMovementType" /> parameter.
	/// </param>
	void Insert(int offset, string text, AnchorMovementType defaultAnchorMovementType);

	/// <summary>
	/// Inserts text.
	/// </summary>
	/// <param name="offset"> The offset at which the text is inserted. </param>
	/// <param name="text"> The new text. </param>
	/// <param name="defaultAnchorMovementType">
	/// Anchors positioned exactly at the insertion offset will move according to the anchor's movement type.
	/// For AnchorMovementType.Default, they will move according to the movement type specified by this parameter.
	/// The caret will also move according to the <paramref name="defaultAnchorMovementType" /> parameter.
	/// </param>
	void Insert(int offset, ITextSource text, AnchorMovementType defaultAnchorMovementType);

	/// <summary>
	/// Creates an undo group. Dispose the returned value to close the undo group.
	/// </summary>
	/// <returns> An object that closes the undo group when Dispose() is called. </returns>
	IDisposable OpenUndoGroup();

	/// <summary>
	/// Removes text.
	/// </summary>
	/// <param name="offset"> Starting offset of the text to be removed. </param>
	/// <param name="length"> Length of the text to be removed. </param>
	void Remove(int offset, int length);

	/// <summary>
	/// Replaces text.
	/// </summary>
	/// <param name="offset"> The starting offset of the text to be replaced. </param>
	/// <param name="length"> The length of the text to be replaced. </param>
	/// <param name="newText"> The new text. </param>
	void Replace(int offset, int length, string newText);

	/// <summary>
	/// Replaces text.
	/// </summary>
	/// <param name="offset"> The starting offset of the text to be replaced. </param>
	/// <param name="length"> The length of the text to be replaced. </param>
	/// <param name="newText"> The new text. </param>
	void Replace(int offset, int length, ITextSource newText);

	/// <summary>
	/// Make the document combine the following actions into a single
	/// action for undo purposes.
	/// </summary>
	void StartUndoableAction();

	#endregion

	#region Events

	/// <summary>
	/// This event is called after a group of changes is completed.
	/// </summary>
	/// <seealso cref="EndUndoableAction" />
	event EventHandler ChangeCompleted;

	/// <summary>
	/// Fired when the file name of the document changes.
	/// </summary>
	event EventHandler FileNameChanged;

	/// <summary>
	/// This event is called directly after a change is applied to the document.
	/// </summary>
	/// <remarks>
	/// It is invalid to modify the document within this event handler.
	/// Aborting the event handler (by throwing an exception) is likely to cause corruption of data structures
	/// that listen to the Changing and Changed events.
	/// </remarks>
	event EventHandler<TextChangeEventArgs> TextChanged;

	/// <summary>
	/// This event is called directly before a change is applied to the document.
	/// </summary>
	/// <remarks>
	/// It is invalid to modify the document within this event handler.
	/// Aborting the change (by throwing an exception) is likely to cause corruption of data structures
	/// that listen to the Changing and Changed events.
	/// </remarks>
	event EventHandler<TextChangeEventArgs> TextChanging;

	#endregion
}