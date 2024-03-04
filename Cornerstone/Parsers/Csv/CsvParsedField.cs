namespace Cornerstone.Parsers.Csv;

internal sealed class CsvParsedField
{
	#region Fields

	internal bool EmptyOrSpace;
	internal int End;
	internal int EscapedQuotesCount;
	internal bool Quoted;
	internal int Start;
	private string _cachedValue;

	#endregion

	#region Properties

	internal int Length => (End - Start) + 1;

	#endregion

	#region Methods

	internal string GetValue(char[] buf)
	{
		return _cachedValue ??= GetValueInternal(buf);
	}

	internal CsvParsedField Reset(int start)
	{
		Start = start;
		End = start - 1;
		Quoted = false;
		EmptyOrSpace = true;
		EscapedQuotesCount = 0;
		_cachedValue = null;
		return this;
	}

	private string GetString(char[] buf, int start, int len)
	{
		var bufLen = buf.Length;
		start = start < bufLen ? start : start % bufLen;
		var endIdx = (start + len) - 1;
		if (endIdx >= bufLen)
		{
			var prefixLen = buf.Length - start;
			var prefix = new string(buf, start, prefixLen);
			var suffix = new string(buf, 0, len - prefixLen);
			return prefix + suffix;
		}
		return new string(buf, start, len);
	}

	private string GetValueInternal(char[] buf)
	{
		if (!Quoted)
		{
			var len = Length;
			return len > 0 ? GetString(buf, Start, len) : string.Empty;
		}

		var s = Start + 1;
		var lenWithoutQuotes = Length - 2;
		var val = lenWithoutQuotes > 0 ? GetString(buf, s, lenWithoutQuotes) : string.Empty;

		if (EscapedQuotesCount > 0)
		{
			val = val.Replace("\"\"", "\"");
		}

		return val;
	}

	#endregion
}