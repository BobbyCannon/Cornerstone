#region References

using System;
using Cornerstone.Avalonia.Text.Models;
using Cornerstone.Data;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Avalonia.Text;

public partial class TextDocument : Notifiable<TextDocument>
{
	#region Constructors

	public TextDocument()
	{
		Buffer = new StringGapBuffer(16384);
		Lines = new LineManager(this);
		Load(string.Empty);
	}

	#endregion

	#region Properties

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int DocumentWidth { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int Length { get; private set; }

	/// <summary>
	/// The lines of the document.
	/// </summary>
	[UpdateableAction(UpdateableAction.All)]
	public LineManager Lines { get; }

	/// <summary>
	/// The character buffer for the document.
	/// </summary>
	[UpdateableAction(UpdateableAction.All)]
	internal StringGapBuffer Buffer { get; }

	#endregion

	#region Methods

	public int Delete(int offset, bool forward)
	{
		return forward
			? DeleteForward(offset)
			: DeleteBackwards(offset);
	}

	public void Insert(int offset, string value)
	{
		Buffer.Insert(offset, value);
		OnDocumentChanged(offset, value.Length);
	}

	public void Insert(int offset, char value)
	{
		Buffer.Insert(offset, value);
		OnDocumentChanged(offset, 1);
	}

	public void Load(string data)
	{
		Buffer.Reset(data);
		Lines.Rebuild(0);
		OnDocumentChanged(0, data.Length);
	}

	public void RemoveAt(int offset, int length)
	{
		Buffer.RemoveAt(offset, length);
		OnDocumentChanged(offset, length);
	}

	protected virtual void OnDocumentChanged(int offset, int length)
	{
		Lines.Rebuild(offset);
		Length = Buffer.Count;
		DocumentChanged?.Invoke(this, new TextDocumentChangedArgs
		{
			Offset = offset,
			Length = length
		});
	}

	private int DeleteBackwards(int caretOffset)
	{
		if (caretOffset <= 0)
		{
			return 0;
		}

		var offset = caretOffset - 1;

		if ((Buffer[offset] == '\n')
			&& (offset > 0)
			&& (Buffer[offset - 1] == '\r'))
		{
			Buffer.RemoveAt(offset - 1, 2);
			OnDocumentChanged(offset - 1, 2);
			return 2;
		}

		if ((Buffer[offset] == '\r')
			&& ((offset + 1) < Buffer.Count)
			&& (Buffer[offset + 1] == '\n'))
		{
			Buffer.RemoveAt(offset, 2);
			OnDocumentChanged(offset, 2);
			return 2;
		}

		Buffer.RemoveAt(offset, 1);
		OnDocumentChanged(offset, 1);
		return 1;
	}

	private int DeleteForward(int caretOffset)
	{
		if (caretOffset >= Length)
		{
			return 0;
		}

		if ((Buffer[caretOffset] == '\r')
			&& ((caretOffset + 1) < Length)
			&& (Buffer[caretOffset + 1] == '\n'))
		{
			Buffer.RemoveAt(caretOffset, 2);
			OnDocumentChanged(caretOffset, 2);
			return 2;
		}
		Buffer.RemoveAt(caretOffset, 1);
		OnDocumentChanged(caretOffset, 1);
		return 1;
	}

	#endregion

	#region Events

	public event EventHandler<TextDocumentChangedArgs> DocumentChanged;

	#endregion
}