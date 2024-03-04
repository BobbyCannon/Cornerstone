#region References

using System.IO;
using System.Text;
using Cornerstone.PowerShell;
using Cornerstone.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.IntegrationTests.PowerShell;

[TestClass]
public class ModuleGenerationTests : CornerstoneUnitTest
{
	#region Methods

	[TestMethod]
	public void UpdateDocumentation()
	{
		if (!EnableFileUpdates && !IsDebugging)
		{
			return;
		}

		// C:\Workspaces\EpicSolution\Cornerstone\Cornerstone.PowerShell\Cornerstone.PowerShell-Help.xml
		var path = Path.Join(SolutionDirectory, "Cornerstone.PowerShell", "Cornerstone.PowerShell-Help.xml");
		IsTrue(File.Exists(path), $"Not Found: {path}");

		var assembly = typeof(CmdletHelpGenerator).Assembly;
		var xml = CmdletHelpGenerator.GenerateXml(assembly);
		File.WriteAllText(path, xml, Encoding.UTF8);
	}

	#endregion
}