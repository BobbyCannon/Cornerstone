#region References

using System;
using System.Text.RegularExpressions;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Solution.Parsers;

internal class MinimumVisualStudioVersionParser : ISolutionTextParser<string>
{
	#region Constants

	public const string Error = "A valid Minimum Visual Studio Version could not be found in the sln text.";

	public const string Prefix = "MinimumVisualStudioVersion = ";

	#endregion

	#region Methods

	public string Parse(string slnText)
	{
		if (string.IsNullOrEmpty(slnText))
		{
			ParserExThrower.ThrowSlnTextNullOrEmpty(nameof(slnText));
		}

		try
		{
			var match = Regex.Match(slnText, $"^{Prefix}([0-9.]+)", RegexOptions.Multiline);

			var version = match.Groups[1].Value;

			if (version == string.Empty)
			{
				throw new InvalidDotNetSolutionException(Error);
			}

			return version;
		}
		catch (Exception ex)
		{
			throw new InvalidDotNetSolutionException(Error, ex);
		}
	}

	#endregion
}