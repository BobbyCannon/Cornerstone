#region References

using Microsoft.VisualStudio.Text;
using AvaloniaTextChange = Cornerstone.VisualStudio.Core.ITextChange;

#endregion

namespace Cornerstone.VisualStudio.IntelliSense;

public class TextChangeAdapter : Core.ITextChange
{
	#region Fields

	private readonly ITextChange _textChange;

	#endregion

	#region Constructors

	public TextChangeAdapter(ITextChange textChange)
	{
		_textChange = textChange;
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public int NewPosition => _textChange.NewPosition;

	/// <inheritdoc />
	public string NewText => _textChange.NewText;

	/// <inheritdoc />
	public int OldPosition => _textChange.OldPosition;

	/// <inheritdoc />
	public string OldText => _textChange.OldText;

	#endregion
}