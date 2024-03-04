#region References

using System;
using System.Text.RegularExpressions;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Solution.Parsers;

internal class FormatVersionParser : ISolutionTextParser<string>
{
	#region Constants

	public const string Prefix = "Microsoft Visual Studio Solution File, Format Version ";

	private const string ExMessage = "A valid Format Version could not be found in the sln text.";

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

			var value = match.Groups[1].Value;

			if (string.IsNullOrEmpty(value))
			{
				throw new InvalidDotNetSolutionException(ExMessage);
			}

			return value;
		}
		catch (Exception ex)
		{
			throw new InvalidDotNetSolutionException(ExMessage, ex);
		}
	}

	#endregion
}