#region References

using Cornerstone.Data;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Avalonia.Text.Models;

public partial class Caret : Notifiable
{
	#region Fields

	private bool _blink;
	private readonly TextEditorViewModel _viewModel;
	private int _virtualColumn;

	#endregion

	#region Constructors

	public Caret(TextEditorViewModel viewModel)
	{
		_viewModel = viewModel;
		_virtualColumn = 0;
		_blink = false;

		IsVisible = false;
	}

	#endregion

	#region Properties

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool IsVisible { get; set; }

	/// <summary>
	/// Represents the line the caret is on.
	/// </summary>
	public Line Line => _viewModel.Document.Lines.GetLineForOffset(Offset);

	/// <summary>
	/// Represents the document offset the caret represents.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial int Offset { get; set; }

	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial bool OverstrikeMode { get; set; }

	/// <summary>
	/// Represents the selection the caret owns.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial Selection Selection { get; set; }

	/// <summary>
	/// Represents the token where the token is located.
	/// </summary>
	[Notify]
	[UpdateableAction(UpdateableAction.All)]
	public partial Token Token { get; set; }

	#endregion

	#region Methods

	public void Move(int offset)
	{
		Move(offset, true);
	}

	public void MoveDown()
	{
		if (!_viewModel.Document.Lines.TryGetLine(Line.LineNumber + 1, out var line))
		{
			return;
		}

		var newOffset = line.StartOffset + _virtualColumn;
		IntegerExtensions.EnsureRange(ref newOffset, line.StartOffset, line.EndOffset - line.LineEndingLength);
		Move(newOffset, false);
	}

	public void MoveLeft()
	{
		var offset = Offset - 1;
		var buffer = _viewModel.Document.Buffer;

		if (offset < 0)
		{
			return;
		}

		if ((buffer[offset] == '\n')
			&& (offset > 0)
			&& (buffer[offset - 1] == '\r'))
		{
			Move(offset - 1);
		}
		else if ((buffer[offset] == '\r')
				&& ((offset + 1) < buffer.Count)
				&& (buffer[offset + 1] == '\n'))
		{
			Move(offset);
		}
		else
		{
			Move(offset);
		}
	}

	public void MoveRight()
	{
		var offset = Offset;
		var buffer = _viewModel.Document.Buffer;

		if (offset >= buffer.Count)
		{
			return;
		}

		if ((buffer[offset] == '\r')
			&& ((offset + 1) < buffer.Count)
			&& (buffer[offset + 1] == '\n'))
		{
			Move(offset + 2);
		}
		else
		{
			Move(offset + 1);
		}
	}

	public void MoveToLineEnd()
	{
		var line = Line;
		Move(line.EndOffset - line.LineEndingLength);
	}

	public void MoveToLineStart()
	{
		var line = Line;
		Move(line.StartOffset);
	}

	public void MoveUp()
	{
		if (!_viewModel.Document.Lines.TryGetLine(Line.LineNumber - 1, out var line))
		{
			return;
		}

		var newOffset = line.StartOffset + _virtualColumn;
		IntegerExtensions.EnsureRange(ref newOffset, line.StartOffset, line.EndOffset - line.LineEndingLength);
		Move(newOffset, false);
	}

	public void Reset()
	{
		IsVisible = false;
		Move(0);
	}

	public bool ShouldShow(bool toggleBlink)
	{
		_blink = !_blink;
		return IsVisible && _blink;
	}

	protected override void OnPropertyChanged(string propertyName = null)
	{
		switch (propertyName)
		{
			case nameof(Offset):
			{
				OnPropertyChanged(nameof(Line));
				break;
			}
		}
		base.OnPropertyChanged(propertyName);
	}

	private void Move(int offset, bool changeVirtualColumn)
	{
		IntegerExtensions.EnsureRange(ref offset, 0, _viewModel.Document.Length);

		Offset = offset;

		if (changeVirtualColumn)
		{
			_virtualColumn = Offset - Line.StartOffset;
		}

		_viewModel.OnCaretMoved(offset);
	}

	#endregion
}