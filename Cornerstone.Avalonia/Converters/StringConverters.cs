#region References

using System.Collections.Generic;
using Avalonia.Data.Converters;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class StringConverters
{
	#region Fields

	public static readonly FuncValueConverter<IEnumerable<string>, string, string> Join;
	public static readonly FuncValueConverter<string, string> ToSentenceCase;

	#endregion

	#region Constructors

	static StringConverters()
	{
		Join = new FuncValueConverter<IEnumerable<string>, string, string>((v, p) => string.Join(p ?? ", ", v ?? []));
		ToSentenceCase = new(x => x.ToSentenceCase());
	}

	#endregion
}