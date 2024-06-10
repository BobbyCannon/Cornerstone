#region References

using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using Cornerstone.Extensions;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.CodeCompletion;

public abstract class CompletionProvider : ICompletionProvider
{
	#region Constructors

	protected CompletionProvider(params CompletionTrigger[] keys)
	{
		Keys = keys;
	}

	#endregion

	#region Properties

	public CompletionTrigger[] Keys { get; }

	#endregion

	#region Methods

	public static string GetCompletionPrefix(string input, string completionText)
	{
		if (input.EndsWithStartOf(completionText, out var match, true))
		{
			return match;
		}

		if (TryGetCompletionPrefix(input, "::", false, out var value))
		{
			return value;
		}

		if (TryGetCompletionPrefix(input, ".\\", true, out value))
		{
			return value;
		}

		if (TryGetCompletionPrefixOfAny(input, [' ', '\r', '\n', '.'], false, out value))
		{
			return value;
		}

		if (TryGetCompletionPrefixOfAny(input, ['['], true, out value))
		{
			return value;
		}

		return input;
	}

	/// <inheritdoc />
	public virtual bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent)
	{
		var found = Keys
			.FirstOrDefault(x =>
				(x.Key == key) &&
				(x.Modifiers == modifiers)
			);

		silent = found.Silent;
		return found.Key != Key.None;
	}

	/// <inheritdoc />
	public abstract bool TryGetAutoComplete(string input, out string prefix, out ICompletionData[] data);

	private static bool TryGetCompletionPrefix(string input, IEnumerable<string> terminators, bool includeTerminator, out string value)
	{
		foreach (var terminator in terminators)
		{
			if (TryGetCompletionPrefix(input, terminator, includeTerminator, out value))
			{
				return true;
			}
		}

		value = null;
		return false;
	}

	private static bool TryGetCompletionPrefix(string input, string terminator, bool includeTerminator, out string value)
	{
		var inputOffset = input.Length - 1;
		var response = input;
		var index = response.LastIndexOf(terminator, inputOffset);

		if (index < 0)
		{
			value = null;
			return false;
		}

		var length = input.Length - index;
		if ((length == terminator.Length) && !includeTerminator)
		{
			value = string.Empty;
			return true;
		}

		value = includeTerminator
			? response.Substring(index, length)
			: response.Substring(index + terminator.Length, length - terminator.Length);

		return true;
	}

	private static bool TryGetCompletionPrefixOfAny(string input, char[] terminators, bool includeTerminator, out string value)
	{
		var inputOffset = input.Length - 1;
		var response = input;
		var index = response.IndexOfAnyReverse(terminators, inputOffset);

		if (index < 0)
		{
			value = null;
			return false;
		}

		var length = inputOffset - index;
		value = response.Substring(includeTerminator ? index : index + 1, length);
		return true;
	}

	#endregion
}

public interface ICompletionProvider
{
	#region Methods

	bool ShouldTrigger(Key key, KeyModifiers modifiers, out bool silent);

	public bool TryGetAutoComplete(string input, out string prefix, out ICompletionData[] data);

	#endregion
}