#region References

using System;
using System.IO;
using Cornerstone.Parsers.VisualStudio.Solution;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Parsers.VisualStudio.Solution;

[TestClass]
public class DotNetSolutionTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void ConstructorShouldInitializeItems()
	{
		var solution = new DotNetSolution();
		Assert.IsNotNull(solution.Items);
		Assert.AreEqual(0, solution.Items.Count);
	}

	[TestMethod]
	public void GetDirectoryDepthShouldHandleTrailingSeparators()
	{
		var depth1 = DotNetSolution.GetDirectoryDepth(@"C:\Folder1\Folder2\");
		Assert.AreEqual(2, depth1);

		var depth2 = DotNetSolution.GetDirectoryDepth(@"Folder1\Folder2\");
		Assert.AreEqual(2, depth2);
	}

	[TestMethod]
	public void GetDirectoryDepthShouldReturnCorrectDepthForAbsolutePaths()
	{
		var depth1 = DotNetSolution.GetDirectoryDepth(@"C:\");
		Assert.AreEqual(0, depth1);

		var depth2 = DotNetSolution.GetDirectoryDepth(@"C:\Folder1");
		Assert.AreEqual(1, depth2);

		var depth3 = DotNetSolution.GetDirectoryDepth(@"C:\Folder1\Folder2");
		Assert.AreEqual(2, depth3);

		var depth4 = DotNetSolution.GetDirectoryDepth(@"C:\Folder1\Folder2\Folder3");
		Assert.AreEqual(3, depth4);
	}

	[TestMethod]
	public void GetDirectoryDepthShouldReturnCorrectDepthForRelativePaths()
	{
		var depth1 = DotNetSolution.GetDirectoryDepth("Folder1");
		Assert.AreEqual(1, depth1);

		var depth2 = DotNetSolution.GetDirectoryDepth(@"Folder1\Folder2");
		Assert.AreEqual(2, depth2);

		var depth3 = DotNetSolution.GetDirectoryDepth(@"Folder1\Folder2\Folder3");
		Assert.AreEqual(3, depth3);
	}

	[TestMethod]
	public void LoadShouldHandleMultipleItems()
	{
		var xmlContent = """
						<Solution>
						    <Project Path="src\App.csproj" />
						    <Folder Name="Tests" Path="tests" />
						    <File Path="README.md" />
						</Solution>
						""";
		var tempPath = CreateTempSolutionFile(xmlContent);

		try
		{
			var solution = DotNetSolution.Load(tempPath);
			Assert.AreEqual(3, solution.Items.Count);

			Assert.AreEqual("src\\App.csproj", solution.Items[0].Name);
			Assert.AreEqual(SolutionItemType.Project, solution.Items[0].ItemType);
			Assert.AreEqual(1, solution.Items[0].Level);

			Assert.AreEqual("tests", solution.Items[1].Name);
			Assert.AreEqual(SolutionItemType.Folder, solution.Items[1].ItemType);
			Assert.AreEqual(1, solution.Items[1].Level);

			Assert.AreEqual("README.md", solution.Items[2].Name);
			Assert.AreEqual(SolutionItemType.File, solution.Items[2].ItemType);
			Assert.AreEqual(0, solution.Items[2].Level);
		}
		finally
		{
			File.Delete(tempPath);
		}
	}

	[TestMethod]
	public void LoadShouldParseFileElementCorrectly()
	{
		var xmlContent = """
						<Solution>
						    <File Path="docs\readme.md" />
						</Solution>
						""";
		var tempPath = CreateTempSolutionFile(xmlContent);

		try
		{
			var solution = DotNetSolution.Load(tempPath);
			Assert.AreEqual(1, solution.Items.Count);

			var item = solution.Items[0];
			Assert.AreEqual("docs\\readme.md", item.Name);
			Assert.AreEqual(SolutionItemType.File, item.ItemType);
			Assert.AreEqual(1, item.Level);
		}
		finally
		{
			File.Delete(tempPath);
		}
	}

	[TestMethod]
	public void LoadShouldParseFolderElementCorrectlyWithoutPath()
	{
		var xmlContent = """
						<Solution>
						    <Folder Name="Nested\Deep\Folder" />
						</Solution>
						""";
		var tempPath = CreateTempSolutionFile(xmlContent);

		try
		{
			var solution = DotNetSolution.Load(tempPath);
			Assert.AreEqual(1, solution.Items.Count);

			var item = solution.Items[0];
			Assert.AreEqual(@"Nested\Deep\Folder", item.Name);
			Assert.AreEqual(SolutionItemType.Folder, item.ItemType);
			Assert.AreEqual(3, item.Level);
		}
		finally
		{
			File.Delete(tempPath);
		}
	}

	[TestMethod]
	public void LoadShouldParseFolderElementCorrectlyWithPath()
	{
		var xmlContent = """
						<Solution>
						    <Folder Name="UI" Path="UI" />
						</Solution>
						""";
		var tempPath = CreateTempSolutionFile(xmlContent);

		try
		{
			var solution = DotNetSolution.Load(tempPath);
			Assert.AreEqual(1, solution.Items.Count);

			var item = solution.Items[0];
			Assert.AreEqual("UI", item.Name);
			Assert.AreEqual(SolutionItemType.Folder, item.ItemType);
			Assert.AreEqual(1, item.Level);
		}
		finally
		{
			File.Delete(tempPath);
		}
	}

	[TestMethod]
	public void LoadShouldParseProjectElementCorrectly()
	{
		var xmlContent = """
						<Solution>
						    <Project Path="Projects\MyApp\MyApp.csproj" />
						</Solution>
						""";
		var tempPath = CreateTempSolutionFile(xmlContent);

		try
		{
			var solution = DotNetSolution.Load(tempPath);
			Assert.AreEqual(1, solution.Items.Count);

			var item = solution.Items[0];
			Assert.AreEqual(@"Projects\MyApp\MyApp.csproj", item.Name);
			Assert.AreEqual(SolutionItemType.Project, item.ItemType);
			Assert.AreEqual(2, item.Level);
			Assert.AreEqual(Path.GetDirectoryName(tempPath), solution.Directory);
			Assert.AreEqual(Path.GetFullPath(tempPath), solution.FilePath);
		}
		finally
		{
			File.Delete(tempPath);
		}
	}

	[TestMethod]
	public void LoadShouldParseUnknownElementCorrectly()
	{
		var xmlContent = """
						<Solution>
						    <CustomTag />
						</Solution>
						""";
		var tempPath = CreateTempSolutionFile(xmlContent);

		try
		{
			var solution = DotNetSolution.Load(tempPath);
			Assert.AreEqual(1, solution.Items.Count);

			var item = solution.Items[0];
			Assert.AreEqual("CustomTag", item.Name);
			Assert.AreEqual(SolutionItemType.Unknown, item.ItemType);
			Assert.AreEqual(0, item.Level);
		}
		finally
		{
			File.Delete(tempPath);
		}
	}

	[TestMethod]
	public void LoadShouldThrowFileNotFoundExceptionWhenFileDoesNotExist()
	{
		var nonExistentPath = Path.Combine(Path.GetTempPath(), "NonExistent.slnx");
		Action act = () => DotNetSolution.Load(nonExistentPath);

		ExpectedException<FileNotFoundException>(act);
	}

	[TestMethod]
	public void LoadShouldThrowInvalidDataExceptionWhenRootIsNotSolution()
	{
		var xmlContent = """
						<Root>
						    <Project Path="proj.csproj" />
						</Root>
						""";
		var tempPath = CreateTempSolutionFile(xmlContent);

		try
		{
			Action act = () => DotNetSolution.Load(tempPath);
			ExpectedException<InvalidDataException>(act);
		}
		finally
		{
			File.Delete(tempPath);
		}
	}

	private static string CreateTempSolutionFile(string xmlContent)
	{
		var tempPath = Path.GetTempFileName();
		File.WriteAllText(tempPath, xmlContent);
		return tempPath;
	}

	#endregion
}