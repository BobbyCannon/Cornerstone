#region References

using System;

#endregion

namespace Cornerstone.Text.Document;

/// <summary>
/// Allows registering a line tracker on a TextDocument using a weak reference from the document to the line tracker.
/// </summary>
public sealed class WeakLineTracker : ILineTracker
{
	#region Fields

	private readonly WeakReference _targetObject;
	private TextDocument _textDocument;

	#endregion

	#region Constructors

	private WeakLineTracker(TextDocument textDocument, ILineTracker targetTracker)
	{
		_textDocument = textDocument;
		_targetObject = new WeakReference(targetTracker);
	}

	#endregion

	#region Methods

	/// <summary>
	/// Deregisters the weak line tracker.
	/// </summary>
	public void Deregister()
	{
		if (_textDocument != null)
		{
			_textDocument.LineTrackers.Remove(this);
			_textDocument = null;
		}
	}

	/// <summary>
	/// Registers the <paramref name="targetTracker" /> as line tracker for the <paramref name="textDocument" />.
	/// A weak reference to the target tracker will be used, and the WeakLineTracker will deregister itself
	/// when the target tracker is garbage collected.
	/// </summary>
	public static WeakLineTracker Register(TextDocument textDocument, ILineTracker targetTracker)
	{
		if (textDocument == null)
		{
			throw new ArgumentNullException(nameof(textDocument));
		}
		if (targetTracker == null)
		{
			throw new ArgumentNullException(nameof(targetTracker));
		}
		var wlt = new WeakLineTracker(textDocument, targetTracker);
		textDocument.LineTrackers.Add(wlt);
		return wlt;
	}

	void ILineTracker.BeforeRemoveLine(DocumentLine line)
	{
		if (_targetObject.Target is ILineTracker targetTracker)
		{
			targetTracker.BeforeRemoveLine(line);
		}
		else
		{
			Deregister();
		}
	}

	void ILineTracker.ChangeComplete(DocumentChangeEventArgs e)
	{
		if (_targetObject.Target is ILineTracker targetTracker)
		{
			targetTracker.ChangeComplete(e);
		}
		else
		{
			Deregister();
		}
	}

	void ILineTracker.LineInserted(DocumentLine insertionPos, DocumentLine newLine)
	{
		if (_targetObject.Target is ILineTracker targetTracker)
		{
			targetTracker.LineInserted(insertionPos, newLine);
		}
		else
		{
			Deregister();
		}
	}

	void ILineTracker.RebuildDocument()
	{
		if (_targetObject.Target is ILineTracker targetTracker)
		{
			targetTracker.RebuildDocument();
		}
		else
		{
			Deregister();
		}
	}

	void ILineTracker.SetLineLength(DocumentLine line, int newTotalLength)
	{
		if (_targetObject.Target is ILineTracker targetTracker)
		{
			targetTracker.SetLineLength(line, newTotalLength);
		}
		else
		{
			Deregister();
		}
	}

	#endregion
}