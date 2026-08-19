#region References

using System.Collections.Generic;
using Avalonia.Input;

#endregion

namespace Cornerstone.Avalonia.Text.Completion;

/// <summary>
/// In-process source used by tests and as a template for language sources.
/// </summary>
public class StaticCompletionSource : ICompletionSource
{
	#region Fields

	private readonly CompletionItem[] _items;
	private readonly CompletionTrigger[] _triggers;

	#endregion

	#region Constructors

	public StaticCompletionSource(IEnumerable<CompletionItem> items, params CompletionTrigger[] triggers)
	{
		_items = items == null ? [] : [..items];
		_triggers = triggers ?? [];
	}

	#endregion

	#region Properties

	public bool QueryOnBackgroundThread => false;

	#endregion

	#region Methods

	public bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent)
	{
		for (var i = 0; i < _triggers.Length; i++)
		{
			if (!_triggers[i].Matches(key, modifiers))
			{
				continue;
			}

			silent = _triggers[i].Silent;
			return true;
		}

		silent = false;
		return false;
	}

	public bool TryGetCompletions(
		CompletionQueryContext context,
		out IReadOnlyList<CompletionItem> items,
		out int replaceStart,
		out int replaceLength)
	{
		items = _items;
		replaceStart = context?.CaretOffset ?? 0;
		replaceLength = 0;
		return _items.Length > 0;
	}

	#endregion
}
