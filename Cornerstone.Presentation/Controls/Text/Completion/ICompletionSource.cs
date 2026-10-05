#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Input;

#endregion

namespace Cornerstone.Presentation.Controls.Text.Completion;

/// <summary>
/// Supplies completion candidates for a <see cref="TextEditorViewModel" />.
/// </summary>
public interface ICompletionSource
{
	#region Properties

	/// <summary>
	/// When true, <see cref="TryGetCompletions" /> may block (IPC, disk) and the manager
	/// runs it on a worker thread using a <see cref="CompletionQueryContext" /> snapshot.
	/// </summary>
	bool QueryOnBackgroundThread { get; }

	#endregion

	#region Methods

	bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent);

	/// <summary>
	/// True when inserting this text should start a session (".", path separators, paste, IME).
	/// Typed triggers belong here so KeyDown is not consumed.
	/// </summary>
	bool ShouldTriggerText(string text, out bool silent)
	{
		silent = false;
		return false;
	}

	/// <summary>
	/// Fill candidates from a UI-thread snapshot. replaceStart/replaceLength are document offsets
	/// of the token that should be replaced when an item is applied.
	/// </summary>
	bool TryGetCompletions(
		CompletionQueryContext context,
		out IReadOnlyList<CompletionItem> items,
		out int replaceStart,
		out int replaceLength);

	#endregion
}
