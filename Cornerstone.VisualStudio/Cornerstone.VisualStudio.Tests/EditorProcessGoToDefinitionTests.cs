#region References

using System.IO;
using System.Threading.Tasks;
using Cornerstone.VisualStudio.Core.AssemblyMetadata;
using Cornerstone.VisualStudio.Core.Completion;
using Cornerstone.VisualStudio.Core.DnlibMetadataProvider;
using Cornerstone.VisualStudio.EditorHost;
using Cornerstone.VisualStudio.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests;

[TestClass]
public class EditorProcessGoToDefinitionTests
{
	[TestMethod]
	public async Task GoToDefinitionReturnsButtonTypeAcrossProcess()
	{
		var presentation = Path.GetFullPath(Path.Combine(
			typeof(EditorProcessGoToDefinitionTests).Assembly.Location,
			"..", "..", "..", "..", "..", "..",
			"Cornerstone.Presentation", "bin", "Debug", "net10.0", "Cornerstone.Presentation.dll"));
		Assert.IsTrue(File.Exists(presentation), presentation);
		var metadata = new MetadataReader(new DnlibMetadataProvider())
			.GetForTargetAssembly(new FolderAssemblyProvider(presentation));
		var xaml = "<Button xmlns=\"https://github.com/BobbyCannon/Cornerstone\" />";
		var caret = xaml.IndexOf("Button") + 2;
		using (var server = new EditorServer(metadata))
		{
			server.Start();
			using (var connection = await EditorConnection.ConnectAsync(server.Port))
			{
				var response = await connection.GoToDefinitionAsync(xaml, caret, "Cornerstone.Presentation", null);
				Assert.AreEqual(string.Empty, response.Error);
				Assert.AreEqual((int) XamlGoToDefinitionKind.Type, response.Kind);
				StringAssert.Contains(response.TypeFullName, "Button");
			}
		}
	}
}
