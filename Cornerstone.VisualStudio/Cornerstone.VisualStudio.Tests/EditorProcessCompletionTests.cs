#region References

using System.Linq;
using System.Threading.Tasks;
using Cornerstone.VisualStudio.Core;
using Cornerstone.VisualStudio.Core.AssemblyMetadata;
using CompletionMetadata = Cornerstone.VisualStudio.Core.AssemblyMetadata.Metadata;
using Cornerstone.VisualStudio.Protocol;
using Cornerstone.VisualStudio.EditorHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.VisualStudio.Tests;

[TestClass]
public class EditorProcessCompletionTests
{
	[TestMethod]
	public async Task GetCompletionsReturnsThemeVariantHintsAcrossProcess()
	{
		var xaml = "<CornerstoneApplication xmlns=\"" + Utils.CornerstoneNamespace + "\" RequestedThemeVariant=\"";
		using (var server = new EditorServer(CreateThemeVariantMetadata()))
		{
			server.Start();
			using (var client = await EditorConnection.ConnectAsync(server.Port))
			{
				var ide = new VisualStudioInterface(client);
				ide.Text = xaml;
				ide.Caret = xaml.Length;
				var response = await ide.GetCompletionsAsync();
				Assert.AreEqual(string.Empty, response.Error);
				CollectionAssert.AreEquivalent(
					new[] { "Default", "Light", "Dark" },
					response.DisplayTexts.ToArray());
				Assert.IsNotNull(response.Items);
				Assert.AreEqual(3, response.Items.Count);
				Assert.IsTrue(response.Items.Any(item => item.InsertText == "Dark"));
			}
		}
	}

	private static CompletionMetadata CreateThemeVariantMetadata()
	{
		var themeVariant = new MetadataType("ThemeVariant")
		{
			FullName = "Cornerstone.Presentation.Styling.ThemeVariant",
			HasHintValues = true,
			HintValues = ["Default", "Light", "Dark"]
		};
		var application = new MetadataType("Application")
		{
			FullName = "Cornerstone.Presentation.Application",
			IsAvaloniaObjectType = true,
			HasSetProperties = true,
			Properties =
			[
				new MetadataProperty("RequestedThemeVariant", themeVariant, null, false, false, true, true),
				new MetadataProperty("Name", null, null, false, false, true, true)
			]
		};
		var metadata = new CompletionMetadata();
		metadata.AddType(Utils.CornerstoneNamespace, application);
		metadata.AddType(Utils.CornerstoneNamespace, themeVariant);
		return metadata;
	}
}
