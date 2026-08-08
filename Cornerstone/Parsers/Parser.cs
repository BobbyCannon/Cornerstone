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

	public override Block CreateOrUpdateSection(int type, int startOffset, int endOffset, uint? foreground = null, uint? background = null,
		bool? bold = null, bool? italic = null, bool? strikethrough = null, params int[] offsets)
	{
		if (Pool?.TryDequeue(out var block) == true)
		{
			block.Update(type, startOffset, endOffset, offsets);
			return block;
		}

		return new Block(type, startOffset, endOffset, offsets);
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
			&& GetTokenTypeCodeName(propName, intValue, out var name))
		{
			return name;
		}

		return null;
	}

	#endregion
}