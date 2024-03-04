#region References

using System;
using System.Text.RegularExpressions;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Solution.Parsers;

internal class MajorVersionParser : ISolutionTextParser<(string, int)>
{
	#region Methods

	public (string, int) Parse(string slnText)
	{
		if (string.IsNullOrEmpty(slnText))
		{
			ParserExThrower.ThrowSlnTextNullOrEmpty(nameof(slnText));
		}

		try
		{
			// Match: "# Visual Studio 15" or "# Visual Studio Version 16"
			var match = Regex.Match(slnText, "(?<n># Visual Studio?( Version|) )(?<v>[0-9]+)");

			return (match.Groups["n"].Value, System.Convert.ToInt32(match.Groups["v"].Value));
		}
		catch (Exception ex)
		{
			throw new InvalidDotNetSolutionException("A valid Major Version could not be found in the sln text.", ex);
		}
	}

	#endregion
}