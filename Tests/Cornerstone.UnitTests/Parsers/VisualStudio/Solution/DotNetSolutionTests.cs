#region References

using System;
using System.Collections.Generic;
using System.IO;
using Cornerstone.Compare;
using Cornerstone.Parsers.VisualStudio.Solution;
using Cornerstone.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.VisualStudio.Solution;

[TestClass]
public class DotNetSolutionTests : CornerstoneUnitTest
{
	#region Constants

	public const string SolutionText = "\r\nMicrosoft Visual Studio Solution File, Format Version 12.00\r\n# Visual Studio Version 17\r\nVisualStudioVersion = 17.8.34330.188\r\nMinimumVisualStudioVersion = 10.0.40219.1\r\nProject(\"{9A19103F-16F7-4668-BE54-9A1E7A4F7556}\") = \"Sample.ClassLibrary\", \"Sample.ClassLibrary\\Sample.ClassLibrary.csproj\", \"{FD9616F8-7CCC-413E-BC1D-29894CCB8CDB}\"\r\nEndProject\r\nProject(\"{2150E333-8FDC-42A3-9474-1A3956D46DE8}\") = \"Tests\", \"Tests\", \"{F4A33F46-1947-4686-9E5B-56DA02373874}\"\r\nEndProject\r\nProject(\"{2150E333-8FDC-42A3-9474-1A3956D46DE8}\") = \"Solution Items\", \"Solution Items\", \"{A254F615-0B7A-4BE7-9621-B3E354230ABD}\"\r\n\tProjectSection(SolutionItems) = preProject\r\n\t\tScripts\\Build.ps1 = Scripts\\Build.ps1\r\n\t\treadme.md = readme.md\r\n\tEndProjectSection\r\nEndProject\r\nProject(\"{9A19103F-16F7-4668-BE54-9A1E7A4F7556}\") = \"Sample.UnitTests\", \"Tests\\Sample.UnitTests\\Sample.UnitTests.csproj\", \"{29F8DF66-D09F-43DA-BCEB-81ECC72C3B26}\"\r\nEndProject\r\nProject(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"Sample.Website\", \"Sample.Website\\Sample.Website.csproj\", \"{3AC0922A-D0A1-438A-A3CB-37BBA987367A}\"\r\nEndProject\r\nGlobal\r\n\tGlobalSection(SolutionConfigurationPlatforms) = preSolution\r\n\t\tDebug|Any CPU = Debug|Any CPU\r\n\t\tRelease|Any CPU = Release|Any CPU\r\n\tEndGlobalSection\r\n\tGlobalSection(ProjectConfigurationPlatforms) = postSolution\r\n\t\t{FD9616F8-7CCC-413E-BC1D-29894CCB8CDB}.Debug|Any CPU.ActiveCfg = Debug|Any CPU\r\n\t\t{FD9616F8-7CCC-413E-BC1D-29894CCB8CDB}.Debug|Any CPU.Build.0 = Debug|Any CPU\r\n\t\t{FD9616F8-7CCC-413E-BC1D-29894CCB8CDB}.Release|Any CPU.ActiveCfg = Release|Any CPU\r\n\t\t{FD9616F8-7CCC-413E-BC1D-29894CCB8CDB}.Release|Any CPU.Build.0 = Release|Any CPU\r\n\t\t{29F8DF66-D09F-43DA-BCEB-81ECC72C3B26}.Debug|Any CPU.ActiveCfg = Debug|Any CPU\r\n\t\t{29F8DF66-D09F-43DA-BCEB-81ECC72C3B26}.Debug|Any CPU.Build.0 = Debug|Any CPU\r\n\t\t{29F8DF66-D09F-43DA-BCEB-81ECC72C3B26}.Release|Any CPU.ActiveCfg = Release|Any CPU\r\n\t\t{29F8DF66-D09F-43DA-BCEB-81ECC72C3B26}.Release|Any CPU.Build.0 = Release|Any CPU\r\n\t\t{3AC0922A-D0A1-438A-A3CB-37BBA987367A}.Debug|Any CPU.ActiveCfg = Debug|Any CPU\r\n\t\t{3AC0922A-D0A1-438A-A3CB-37BBA987367A}.Debug|Any CPU.Build.0 = Debug|Any CPU\r\n\t\t{3AC0922A-D0A1-438A-A3CB-37BBA987367A}.Release|Any CPU.ActiveCfg = Release|Any CPU\r\n\t\t{3AC0922A-D0A1-438A-A3CB-37BBA987367A}.Release|Any CPU.Build.0 = Release|Any CPU\r\n\tEndGlobalSection\r\n\tGlobalSection(SolutionProperties) = preSolution\r\n\t\tHideSolutionNode = FALSE\r\n\tEndGlobalSection\r\n\tGlobalSection(NestedProjects) = preSolution\r\n\t\t{29F8DF66-D09F-43DA-BCEB-81ECC72C3B26} = {F4A33F46-1947-4686-9E5B-56DA02373874}\r\n\tEndGlobalSection\r\n\tGlobalSection(ExtensibilityGlobals) = postSolution\r\n\t\tSolutionGuid = {FC0BAFFB-EA94-46DD-A968-292766DA2D17}\r\n\tEndGlobalSection\r\nEndGlobal\r\n";

	#endregion

	#region Methods

	[TestMethod]
	public void CreateSolution()
	{
		var solution = DotNetSolution.Create("Sample");
		var actual = solution.ToString();
		var expected = @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version17
VisualStudioVersion = 17.7.34031.279
MinimumVisualStudioVersion = 10.0.40219.1
Global
EndGlobal
";
		AreEqual(expected, actual);
	}

	[TestMethod]
	public void LoadFile()
	{
		var path = @$"{SolutionDirectory}\Samples\ParserSolution\Sample.sln";
		var solution = DotNetSolution.LoadFile(path);

		AreEqual($@"{SolutionDirectory}\Samples\ParserSolution", solution.Directory);
		AreEqual("Sample", solution.Name);

		var projects = new[]
		{
			new DotNetSolutionProject(solution, "Sample.ClassLibrary")
			{
				Id = Guid.Parse("fd9616f8-7ccc-413e-bc1d-29894ccb8cdb"),
				ProjectSections = new List<DotNetSolutionProjectSection>(),
				RelativePath = "Sample.ClassLibrary\\Sample.ClassLibrary.csproj",
				Type = DotNetProjectType.CSharpDotNetCore,
				TypeId = Guid.Parse("9a19103f-16f7-4668-be54-9a1e7a4f7556")
			},
			new DotNetSolutionProject(solution, "Tests")
			{
				Id = Guid.Parse("f4a33f46-1947-4686-9e5b-56da02373874"),
				ProjectSections = new List<DotNetSolutionProjectSection>(),
				RelativePath = "Tests",
				Type = DotNetProjectType.SolutionFolder,
				TypeId = Guid.Parse("2150e333-8fdc-42a3-9474-1a3956d46de8")
			},
			new DotNetSolutionProject(solution, "Solution Items")
			{
				Id = Guid.Parse("a254f615-0b7a-4be7-9621-b3e354230abd"),
				ProjectSections = new List<DotNetSolutionProjectSection>
				{
					new()
					{
						Dependencies = new Dictionary<string, string>
						{
							{ "Scripts\\Build.ps1", "Scripts\\Build.ps1" },
							{ "readme.md", "readme.md" }
						},
						Name = "SolutionItems",
						Type = ProjectSectionType.PreProject
					}
				},
				RelativePath = "Solution Items",
				Type = DotNetProjectType.SolutionFolder,
				TypeId = Guid.Parse("2150e333-8fdc-42a3-9474-1a3956d46de8")
			},
			new DotNetSolutionProject(solution, "Sample.UnitTests")
			{
				Id = Guid.Parse("29f8df66-d09f-43da-bceb-81ecc72c3b26"),
				ProjectSections = new List<DotNetSolutionProjectSection>(),
				RelativePath = "Tests\\Sample.UnitTests\\Sample.UnitTests.csproj",
				Type = DotNetProjectType.CSharpDotNetCore,
				TypeId = Guid.Parse("9a19103f-16f7-4668-be54-9a1e7a4f7556")
			},
			new DotNetSolutionProject(solution, "Sample.Website")
			{
				Id = Guid.Parse("3ac0922a-d0a1-438a-a3cb-37bba987367a"),
				ProjectSections = new List<DotNetSolutionProjectSection>(),
				RelativePath = "Sample.Website\\Sample.Website.csproj",
				Type = DotNetProjectType.CSharp,
				TypeId = Guid.Parse("fae04ec0-301f-11d3-bf4b-00c04f79efbc")
			}
		};

		//CSharpCodeWriter.GenerateCode(solution.Projects, CodeWriterMode.Instance, new CodeWriterSettings { TextFormat = TextFormat.Indented }).Dump();

		AreEqual(projects, solution.Projects, null,
			new ComparerOptions
			{
				PropertiesToIgnore = new Dictionary<Type, string[]>
				{
					{
						typeof(DotNetSolutionProject),
						[
							nameof(DotNetSolutionProject.Attributes),
							nameof(DotNetSolutionProject.Elements),
							nameof(DotNetSolutionProject.ItemGroups),
							nameof(DotNetSolutionProject.TargetFrameworks)
						]
					}
				}
			}
		);
	}

	[TestMethod]
	public void WriteTo()
	{
		var path = @$"{SolutionDirectory}\Samples\ParserSolution\Sample.sln";
		var rawData = File.ReadAllText(path);
		var solution = DotNetSolution.LoadFile(path);
		var data = new TextBuilder();
		solution.WriteTo(data);
		AreEqual(rawData, data.ToString());
	}

	#endregion
}