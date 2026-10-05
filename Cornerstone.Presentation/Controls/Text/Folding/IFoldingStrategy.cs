#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Presentation.Controls.Text.Folding;

/// <summary>
/// Discovers fold ranges for a <see cref="TextEditorViewModel" />.
/// Assign to <see cref="FoldingManager.Strategy" /> from the host (file type, language).
/// </summary>
public interface IFoldingStrategy
{
	#region Methods

	IEnumerable<NewFolding> CreateNewFoldings(TextEditorViewModel viewModel);

	#endregion
}
