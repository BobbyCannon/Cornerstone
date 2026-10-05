#region References

extern alias buildtasks;
using System.IO;
using System.Linq;
using System.Reflection;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompileCornerstoneXamlTask = buildtasks::Cornerstone.Presentation.Build.Tasks.CompileCornerstoneXamlTask;

#endregion

namespace Cornerstone.Presentation.UnitTests.BuildTasks;

[TestClass]
public class CompileCornerstoneXamlTaskTest
{
	#region Methods

	[PresentationTestMethod]
	public void DoesNotFailWhenCodebehindContainsDllImport()
	{
		using var engine = UnitTestBuildEngine.Start();
		var basePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Assets");
		var assembly = new TaskItem(Path.Combine(basePath, "PInvoke.dll"));
		assembly.SetMetadata(CompileCornerstoneXamlTask.CornerstoneCompileOutputMetadataName, Path.Combine(basePath, "Cornerstone", Path.GetFileName(assembly.ItemSpec)));
		var references = File.ReadAllLines(Path.Combine(basePath, "PInvoke.dll.refs")).Select(p => new TaskItem(p)).ToArray();

		CornerstoneTest.IsTrue(File.Exists(assembly.ItemSpec), $"The original {assembly.ItemSpec} don't exist.");

		new CompileCornerstoneXamlTask
		{
			AssemblyFile = assembly,
			References = references,
			RefAssemblyFile = null,
			BuildEngine = engine,
			ProjectDirectory = Directory.GetCurrentDirectory(),
			VerifyIl = true
		}.Execute();
		CornerstoneTest.Empty(engine.Errors);
	}

	#endregion
}