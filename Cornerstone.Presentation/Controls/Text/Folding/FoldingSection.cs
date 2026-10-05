#region References

using Cornerstone.Text;

#endregion

namespace Cornerstone.Presentation.Controls.Text.Folding;

/// <summary>
/// A document range that can be collapsed. The buffer is not changed.
/// </summary>
public partial class FoldingSection : TextRange
{
	#region Fields

	private bool _isFolded;
	private readonly FoldingManager _manager;

	#endregion

	#region Constructors

	internal FoldingSection(FoldingManager manager, int startOffset, int endOffset)
	{
		_manager = manager;
		StartOffset = startOffset;
		EndOffset = endOffset;
	}

	#endregion

	#region Properties

	public bool IsFolded
	{
		get => _isFolded;
		set
		{
			if (!SetFoldedCore(value))
			{
				return;
			}

			_manager?.NotifyFoldingChanged();
		}
	}

	public string Title { get; set; }

	#endregion

	#region Methods

	internal bool SetFoldedCore(bool value)
	{
		if (_isFolded == value)
		{
			return false;
		}

		_isFolded = value;
		return true;
	}

	#endregion
}
