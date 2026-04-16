#region References

using System;

#endregion

namespace Cornerstone.Parsers.Json;

public class JsonCompletionService : CompletionService
{
	#region Fields

	private static readonly string _cachedResponses;

	#endregion

	#region Constructors

	static JsonCompletionService()
	{
		//                   01234
		_cachedResponses = @"{}[]""";
	}

	#endregion

	#region Methods

	public override bool TryGetCompletion(ReadOnlySpan<char> input, out ReadOnlySpan<char> completion)
	{
		if (input.Length == 0)
		{
			completion = null;
			return false;
		}

		var lastChar = input[^1];

		// 01234
		// {}[]"

		completion = lastChar switch
		{
			'{' => _cachedResponses.AsSpan(1, 1),
			'[' => _cachedResponses.AsSpan(3, 1),
			'"' => _cachedResponses.AsSpan(4, 1),
			_ => Span<char>.Empty
		};

		return completion != Span<char>.Empty;
	}

	#endregion
}