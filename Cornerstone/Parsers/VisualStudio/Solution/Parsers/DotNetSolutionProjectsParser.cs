#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Solution.Parsers;

internal class DotNetSolutionProjectsParser
{
	#region Constants

	private const string _projectPattern = "^Project\\(\"{(?<TypeId>[A-F0-9-]+)}\"\\) = \"(?<Name>.*?)\", \"(?<Path>.*?)\", \"{(?<Id>[A-F0-9-]+)}\"(?<Sections>(.|\\n|\\r)*?)EndProject(\\n|\\r)";
	private const string _projectSectionPattern = @"ProjectSection(?<Record>(.|\n|\r)*?)EndProjectSection";

	#endregion

	#region Methods

	public IList<DotNetSolutionProject> Parse(DotNetSolution solution, string slnText)
	{
		var matches = Regex.Matches(slnText, _projectPattern, RegexOptions.Multiline);
		var list = new List<DotNetSolutionProject>();

		foreach (Match match in matches)
		{
			list.Add(LoadSolutionProject(solution, match));
		}

		return list;
	}

	private static DotNetSolutionProjectSection CreateProjectSection(string record)
	{
		var header = record.Substring(0, record.IndexOf('\n')).Trim();

		var ps = new DotNetSolutionProjectSection
		{
			Name = ExtractProjectSectionName(header),
			Type = ExtractProjectSectionType(header)
		};

		var dependencyLines = record
			.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
			.Skip(1)
			.Select(l => l.Trim())
			.Where(l => l != string.Empty)
			.ToArray();

		foreach (var line in dependencyLines)
		{
			var nameValue = line.Split(new[] { " = " }, StringSplitOptions.None).ToArray();

			ps.Dependencies.Add(nameValue[0], nameValue[1]);
		}

		return ps;
	}

	private static IList<DotNetSolutionProjectSection> CreateProjectSections(Match match)
	{
		var sections = match.Groups["Sections"].Value.Trim();
		var projectSections = new List<DotNetSolutionProjectSection>();

		if (string.IsNullOrEmpty(sections))
		{
			return projectSections;
		}

		var psMatches = Regex.Matches(sections, _projectSectionPattern, RegexOptions.Multiline);

		foreach (Match psMatch in psMatches)
		{
			projectSections.Add(CreateProjectSection(psMatch.Groups["Record"].Value));
		}

		return projectSections;
	}

	private static string ExtractProjectSectionName(string header)
	{
		return Regex.Match(header, @"\((?<Name>.*?)\)").Groups[1].Value;
	}

	private static ProjectSectionType ExtractProjectSectionType(string header)
	{
		var type = Regex.Match(header, @" = (?<Type>.*)$").Groups[1].Value;

		return ProjectSectionTypeConverter.ConvertToType(type);
	}

	private DotNetSolutionProject LoadSolutionProject(DotNetSolution solution, Match match)
	{
		var projectTypeId = new Guid(match.Groups["TypeId"].Value);
		var project = new DotNetSolutionProject(solution, match.Groups["Name"].Value)
		{
			Type = ProjectTypeIds.ToEnum(projectTypeId),
			TypeId = projectTypeId,
			RelativePath = match.Groups["Path"].Value,
			Id = new Guid(match.Groups["Id"].Value),
			ProjectSections = CreateProjectSections(match)
		};
		project.Load();
		return project;
	}

	#endregion
}