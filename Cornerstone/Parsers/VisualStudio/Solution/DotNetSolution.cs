#region References

using System;
using System.Collections.Generic;
using System.IO;
using Cornerstone.Extensions;
using Cornerstone.Parsers.VisualStudio.Solution.Parsers;
using Cornerstone.Text;

#endregion

namespace Cornerstone.Parsers.VisualStudio.Solution;

/// <summary>
/// Represents a .NET solution file.
/// </summary>
public class DotNetSolution
{
	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="T:Cornerstone.Parsers.VisualStudio.Solution.DotNetSolution" /> class.
	/// </summary>
	public DotNetSolution()
	{
	}

	#endregion

	#region Properties

	/// <summary>
	/// The directory of the solution.
	/// </summary>
	public string Directory { get; set; }

	/// <summary>
	/// File format version.
	/// </summary>
	public string FormatVersion { get; set; }

	/// <summary>
	/// Collection of global sections.
	/// </summary>
	public IList<DotNetSolutionGlobalSection> GlobalSections { get; private set; }

	/// <summary>
	/// The major version of Visual Studio that (most recently) saved this solution file.
	/// This information controls the version number in the solution icon.
	/// </summary>
	public int MajorVersion { get; set; }

	/// <summary>
	/// The prefix to the major version
	/// </summary>
	public string MajorVersionPrefix { get; private set; }

	/// <summary>
	/// The minimum (oldest) version of Visual Studio that can open this solution file.
	/// </summary>
	public string MinimumVisualStudioVersion { get; set; }

	/// <summary>
	/// The name of the solution.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// Collection of projects referenced by the solution file.
	/// </summary>
	public IList<DotNetSolutionProject> Projects { get; set; }

	/// <summary>
	/// The full version of Visual Studio that (most recently) saved the solution file.
	/// If the solution file is saved by a newer version of Visual Studio that has the same major version,
	/// this value is not updated to lessen churn in the file.
	/// </summary>
	public string VisualStudioVersion { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Create a new dot net solution
	/// </summary>
	/// <param name="name"> The name of the solution. </param>
	/// <returns> The dot net solution. </returns>
	public static DotNetSolution Create(string name)
	{
		/*
		* Microsoft Visual Studio Solution File, Format Version 12.00
		* # Visual Studio Version 17
		* VisualStudioVersion = 17.7.34031.279
		* MinimumVisualStudioVersion = 10.0.40219.1
		*/

		var response = new DotNetSolution
		{
			Directory = string.Empty,
			Name = name,
			FormatVersion = "12.00",
			MajorVersionPrefix = "# Visual Studio Version",
			MajorVersion = 17,
			VisualStudioVersion = "17.7.34031.279",
			MinimumVisualStudioVersion = "10.0.40219.1",
			Projects = new List<DotNetSolutionProject>(),
			GlobalSections = new List<DotNetSolutionGlobalSection>()
		};

		return response;
	}

	/// <summary>
	/// Loads the <see cref="T:Cornerstone.Parsers.VisualStudio.Solution.DotNetSolution" /> from a file path.
	/// </summary>
	/// <param name="filePath"> .NET solution file path. </param>
	/// <returns> New <see cref="T:Cornerstone.Parsers.VisualStudio.Solution.DotNetSolution" /> instance. </returns>
	public static DotNetSolution LoadFile(string filePath)
	{
		var solutionText = File.ReadAllText(filePath);
		return LoadSolution(solutionText, filePath);
	}

	/// <summary>
	/// Loads the <see cref="T:Cornerstone.Parsers.VisualStudio.Solution.DotNetSolution" /> from a file path.
	/// </summary>
	/// <param name="solutionText"> The text for the solution file. </param>
	/// <param name="filePath"> .NET solution file path. </param>
	/// <returns> New <see cref="T:Cornerstone.Parsers.VisualStudio.Solution.DotNetSolution" /> instance. </returns>
	public static DotNetSolution LoadSolution(string solutionText, string filePath)
	{
		var response = new DotNetSolution();
		if (string.IsNullOrEmpty(solutionText))
		{
			throw new ArgumentException("Solution text was null or empty.", nameof(solutionText));
		}

		response.Directory = Path.GetDirectoryName(filePath);
		response.Name = Path.GetFileNameWithoutExtension(filePath);

		response.FormatVersion = new FormatVersionParser().Parse(solutionText);
		(response.MajorVersionPrefix, response.MajorVersion) = new MajorVersionParser().Parse(solutionText);
		response.VisualStudioVersion = new VisualStudioVersionParser().Parse(solutionText);
		response.MinimumVisualStudioVersion = new MinimumVisualStudioVersionParser().Parse(solutionText);
		response.Projects = new DotNetSolutionProjectsParser().Parse(response, solutionText);
		response.GlobalSections = new DotNetSolutionGlobalSectionParser().Parse(solutionText);

		return response;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var builder = new TextBuilder();
		WriteTo(builder);
		return builder.ToString();
	}

	/// <summary>
	/// Write the solution to the provided builder.
	/// </summary>
	/// <param name="builder"> The builder to write to. </param>
	public void WriteTo(ITextBuilder builder)
	{
		builder.NewLine();
		builder.AppendLine(FormatVersionParser.Prefix + FormatVersion);
		builder.AppendLine(MajorVersionPrefix + MajorVersion);
		builder.AppendLine(VisualStudioVersionParser.Prefix + VisualStudioVersion);
		builder.AppendLine(MinimumVisualStudioVersionParser.Prefix + MinimumVisualStudioVersion);

		foreach (var p in Projects)
		{
			var guidString = ProjectTypeIds.ToGuidString(p.Type);
			var line = $"Project(\"{{{guidString}}}\") = \"{p.ProjectName}\", \"{p.RelativePath}\", \"{{{p.Id.ToString().ToUpper()}}}\"";
			builder.AppendLine(line);

			foreach (var s in p.ProjectSections)
			{
				builder.AppendLine($"\tProjectSection({s.Name}) = {s.Type.GetDisplayName()}");

				foreach (var d in s.Dependencies)
				{
					builder.AppendLine($"\t\t{d.Key} = {d.Value}");
				}

				builder.AppendLine("\tEndProjectSection");
			}

			builder.AppendLine("EndProject");
		}

		builder.AppendLine("Global");

		foreach (var g in GlobalSections)
		{
			builder.AppendLine($"\tGlobalSection({g.Name}) = {g.Type.GetDisplayName()}");

			foreach (var s in g.Settings)
			{
				builder.AppendLine($"\t\t{s.Key} = {s.Value}");
			}

			builder.AppendLine("\tEndGlobalSection");
		}

		builder.AppendLine("EndGlobal");
	}

	#endregion
}