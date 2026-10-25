#region References

using System.IO;
using Cornerstone.VisualStudio.Core.Preview;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests;

[TestClass]
public class XamlProjectPathTests
{
	#region Methods

	[TestMethod]
	public void MapsNestedFileToResourcePath()
	{
		var project = Project("Cornerstone.Editor");
		var file = Path.Combine(project, "SourceControl", "Views", "RepositoryInfoView.cxaml");

		Assert.AreEqual(
			"/SourceControl/Views/RepositoryInfoView.cxaml",
			XamlProjectPath.FromProjectDirectory(project, file));
	}

	[TestMethod]
	public void MapsProjectRootFileToResourcePath()
	{
		var project = Project("Cornerstone.Editor");
		var file = Path.Combine(project, "App.cxaml");

		Assert.AreEqual("/App.cxaml", XamlProjectPath.FromProjectDirectory(project, file));
	}

	[TestMethod]
	public void AcceptsTrailingDirectorySeparator()
	{
		var project = Project("Cornerstone.Editor");
		var file = Path.Combine(project, "SourceControl", "Views", "DecoratorChip.cxaml");

		Assert.AreEqual(
			"/SourceControl/Views/DecoratorChip.cxaml",
			XamlProjectPath.FromProjectDirectory(project + Path.DirectorySeparatorChar, file));
	}

	[TestMethod]
	public void KeepsXamlCasingWhenProjectDirectoryDiffers()
	{
		var project = Project("Cornerstone.Editor");
		var file = Path.Combine(project, "SourceControl", "Views", "RepositoryInfoView.cxaml");

		Assert.AreEqual(
			"/SourceControl/Views/RepositoryInfoView.cxaml",
			XamlProjectPath.FromProjectDirectory(project.ToLowerInvariant(), file));
	}

	[TestMethod]
	public void ReturnsNullWhenFileIsOutsideProject()
	{
		var project = Project("Edit");
		var file = Path.Combine(Project("Editor"), "File.cxaml");

		Assert.IsNull(XamlProjectPath.FromProjectDirectory(project, file));
	}

	[TestMethod]
	public void ReturnsNullWhenPathIsMissing()
	{
		var project = Project("Cornerstone.Editor");

		Assert.IsNull(XamlProjectPath.FromProjectDirectory(null, Path.Combine(project, "App.cxaml")));
		Assert.IsNull(XamlProjectPath.FromProjectDirectory(project, null));
		Assert.IsNull(XamlProjectPath.FromProjectDirectory(project, "  "));
	}

	private static string Project(string name)
	{
		return Path.Combine(Path.GetPathRoot(Directory.GetCurrentDirectory()), "Work", name);
	}

	#endregion
}
