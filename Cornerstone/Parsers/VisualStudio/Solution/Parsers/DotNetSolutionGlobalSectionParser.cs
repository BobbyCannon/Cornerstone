﻿#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Solution.Parsers;

internal class DotNetSolutionGlobalSectionParser : ISolutionTextParser<IList<DotNetSolutionGlobalSection>>
{
	#region Constants

	private const string GlobalSectionPattern = @"GlobalSection(?<Record>(.|\n|\r)*?)EndGlobalSection";

	#endregion

	#region Methods

	public IList<DotNetSolutionGlobalSection> Parse(string slnText)
	{
		var matches = Regex.Matches(slnText, GlobalSectionPattern, RegexOptions.Multiline);

		var list = new List<DotNetSolutionGlobalSection>();

		foreach (Match match in matches)
		{
			var record = match.Groups["Record"].Value;

			var gc = CreateDotNetSolutionGlobalSection(record);

			list.Add(gc);
		}

		return list;
	}

	private static DotNetSolutionGlobalSection CreateDotNetSolutionGlobalSection(string record)
	{
		var header = record.Substring(0, record.IndexOf('\n')).Trim();

		var gc = new DotNetSolutionGlobalSection
		{
			Name = ExtractName(header),
			Type = ExtractType(header)
		};

		var settingLines = record.Split(['\n'], StringSplitOptions.RemoveEmptyEntries)
			.Skip(1)
			.Select(l => l.Trim())
			.Where(l => l != string.Empty)
			.ToArray();

		foreach (var settingLine in settingLines)
		{
			var nameValue = settingLine.Split([" = "], StringSplitOptions.None).ToArray();

			gc.Settings.Add(nameValue[0], nameValue.Length > 1 ? nameValue[1] : string.Empty);
		}

		return gc;
	}

	private static string ExtractName(string header)
	{
		return Regex.Match(header, @"\((?<Name>.*?)\)").Groups[1].Value;
	}

	private static GlobalSectionType ExtractType(string header)
	{
		var type = Regex.Match(header, @" = (?<Type>.*)$").Groups[1].Value;

		return GlobalSectionTypeConverter.ConvertToType(type);
	}

	#endregion
}