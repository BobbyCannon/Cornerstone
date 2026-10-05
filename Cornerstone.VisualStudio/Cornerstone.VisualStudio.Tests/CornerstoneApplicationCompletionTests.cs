#region References

using System.Linq;
using Cornerstone.VisualStudio.Core;
using Cornerstone.VisualStudio.Core.AssemblyMetadata;
using Cornerstone.VisualStudio.Core.Completion;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompletionMetadata = Cornerstone.VisualStudio.Core.AssemblyMetadata.Metadata;

#endregion

namespace Cornerstone.VisualStudio.Tests;

[TestClass]
public class CornerstoneApplicationCompletionTests
{
	#region Methods

	[TestMethod]
	public void CornerstoneApplicationOffersRequestedThemeVariantProperty()
	{
		var set = GetCompletions("<CornerstoneApplication xmlns=\"" + Utils.CornerstoneNamespace + "\" ");
		Assert.IsNotNull(set);
		Assert.IsTrue(set.Completions.Any(c => c.DisplayText == "RequestedThemeVariant"),
			string.Join(", ", set.Completions.Select(c => c.DisplayText)));
	}

	[TestMethod]
	public void CornerstoneApplicationRequestedThemeVariantOffersDarkLightDefault()
	{
		var set = GetCompletions(
			"<CornerstoneApplication xmlns=\"" + Utils.CornerstoneNamespace + "\" RequestedThemeVariant=\"");
		Assert.IsNotNull(set);
		CollectionAssert.AreEquivalent(
			new[] { "Default", "Light", "Dark" },
			set.Completions.Select(c => c.DisplayText).ToArray());
	}

	[TestMethod]
	public void ApplicationTagStillOffersRequestedThemeVariant()
	{
		var set = GetCompletions("<Application xmlns=\"" + Utils.CornerstoneNamespace + "\" ");
		Assert.IsNotNull(set);
		Assert.IsTrue(set.Completions.Any(c => c.DisplayText == "RequestedThemeVariant"));
	}

	#endregion

	#region Helpers

	private static CompletionSet GetCompletions(string xaml)
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

		return new CompletionEngine().GetCompletions(metadata, xaml, xaml.Length);
	}

	#endregion
}
