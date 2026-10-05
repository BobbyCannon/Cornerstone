#region References

using System;

#endregion

namespace Cornerstone.Text.Parsing.VisualStudio.Solution.Classic.Parsers;

internal static class ParserExThrower
{
	#region Methods

	public static void ThrowSlnTextNullOrEmpty(string paramName)
	{
		throw new ArgumentException("Solution text is null or empty.", paramName);
	}

	#endregion
}