#region References

using System;
using System.Linq;
using Cornerstone.Parsers.Json;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Parsers;

[SourceReflection]
public class CompletionService
{
	#region Methods

	public static CompletionService GetByExtension(string extension)
	{
		var value = extension?.ToLower();
		if (value == null)
		{
			return null;
		}

		if (JsonTokenizer.Extensions.Contains(value))
		{
			return new JsonCompletionService();
		}

		return null;
	}

	/// <summary>
	/// Try to get a completion of the provided input.
	/// </summary>
	/// <param name="input"> The input value. </param>
	/// <param name="completion"> The completion values if found. </param>
	/// <returns> True if the input was completed otherwise false. </returns>
	public virtual bool TryGetCompletion(ReadOnlySpan<char> input, out ReadOnlySpan<char> completion)
	{
		completion = null;
		return false;
	}

	#endregion
}