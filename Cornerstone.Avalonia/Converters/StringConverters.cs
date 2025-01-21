#region References

using Avalonia.Data.Converters;
using Cornerstone.Text.Human;

#endregion

namespace Cornerstone.Avalonia.Converters;

public static class StringConverters
{
	#region Fields

	public static readonly FuncValueConverter<string, string> ToSentenceCase;

	#endregion

	#region Constructors

	static StringConverters()
	{
		ToSentenceCase = new(x => x.ToSentenceCase());
	}

	#endregion
}