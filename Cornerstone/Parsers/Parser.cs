#region References

using System;
using Cornerstone.Collections;
using Cornerstone.Reflection;
using Cornerstone.Text;
using Cornerstone.Text.CodeGenerators;

#endregion

namespace Cornerstone.Parsers;

[SourceReflection]
public abstract class Parser : TextProcessor<Block>
{
	#region Constructors

	protected Parser(IStringBuffer buffer, IQueue<Block> pool) : base(buffer, pool)
	{
	}

	static Parser()
	{
		CodeBuilder.RegisterPropertyValueProvider(TryGetTokenizerTypeCode);
	}

	#endregion

	#region Methods

	public override Block CreateOrUpdateSection(int type, int startOffset, int endOffset, params int[] offsets)
	{
		if (Pool?.TryDequeue(out var block) == true)
		{
			block.Update(type, startOffset, endOffset, offsets);
			return block;
		}

		return new Block(type, startOffset, endOffset, offsets);
	}

	/// <summary>
	/// Helper method to detect and consume a delimited token: startPattern + content + endPattern.
	/// Supports multi-character start/end patterns and optional state management.
	/// </summary>
	/// <param name="startPattern"> The starting delimiter (e.g. \", *, ```) </param>
	/// <param name="endPattern"> The ending delimiter. </param>
	/// <param name="tokenType"> The token type to assign to the entire delimited section </param>
	/// <param name="block"> The token if it matched and was processed. </param>
	/// <returns> True if a delimited token was successfully processed. </returns>
	protected bool TryProcessDelimitedBlock(string startPattern, string endPattern, int tokenType, out Block block)
	{
		if (!TryMatch(Position, startPattern))
		{
			block = null;
			return false;
		}

		var start = Position;
		var position = start + startPattern.Length;
		var offset1 = position;

		while (position < Buffer.Count)
		{
			// Check for end pattern
			if (TryMatch(position, endPattern))
			{
				var offest2 = position;
				position += endPattern.Length;
				Position = position;

				// Create token for the entire delimited section (including start + content + end)
				block = CreateOrUpdateSection(tokenType, start, position, offset1, offest2);
				return true;
			}

			position++;
		}

		// If we reach here, we hit EOF or newline without finding the end pattern
		block = null;
		return false;
	}

	private static string TryGetTokenizerTypeCode(SourceTypeInfo typeInfo, string propertyName, object value)
	{
		if ((typeInfo.Type != typeof(Block))
			|| value is not int intValue)
		{
			return null;
		}

		var propName = propertyName ?? string.Empty;

		if ((propName.Equals("Type", StringComparison.OrdinalIgnoreCase)
				|| propName.Contains("Type", StringComparison.OrdinalIgnoreCase))
			&& Tokenizer.GetTokenTypeCodeName(propName, intValue, out var name))
		{
			return name;
		}

		return null;
	}

	#endregion
}