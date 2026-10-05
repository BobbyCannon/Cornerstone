#region References

using Cornerstone.VisualStudio.Core;
using Cornerstone.VisualStudio.Core.AssemblyMetadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompletionMetadata = Cornerstone.VisualStudio.Core.AssemblyMetadata.Metadata;

#endregion

namespace Cornerstone.VisualStudio.Tests.Metadata;

[TestClass]
public class MetadataDefaultXmlnsTests
{
	#region Methods

	[TestMethod]
	public void ContainsTypeInXmlnsIsFalseWhenNamespaceMissing()
	{
		var metadata = new CompletionMetadata();
		Assert.IsFalse(metadata.ContainsTypeInXmlns(Utils.AvaloniaNamespace, "Button"));
	}

	[TestMethod]
	public void ContainsTypeInXmlnsIsFalseWhenTypeMissing()
	{
		var metadata = new CompletionMetadata();
		metadata.AddType(Utils.CornerstoneNamespace, new MetadataType("Button") { FullName = "Cornerstone.Controls.Button" });
		Assert.IsFalse(metadata.ContainsTypeInXmlns(Utils.CornerstoneNamespace, "TextBlock"));
	}

	[TestMethod]
	public void ContainsTypeInXmlnsIsTrueWhenTypeExists()
	{
		var metadata = new CompletionMetadata();
		metadata.AddType(Utils.CornerstoneNamespace, new MetadataType("Button") { FullName = "Cornerstone.Controls.Button" });
		Assert.IsTrue(metadata.ContainsTypeInXmlns(Utils.CornerstoneNamespace, "Button"));
	}

	[TestMethod]
	public void IsTypeInDefaultXmlnsIsFalseWhenNeitherDefaultNamespacePresent()
	{
		var metadata = new CompletionMetadata();
		metadata.AddType("clr-namespace:Local;assembly=App", new MetadataType("PageNavigator") { FullName = "App.PageNavigator" });
		Assert.IsFalse(metadata.IsTypeInDefaultXmlns("PageNavigator"));
	}

	[TestMethod]
	public void IsTypeInDefaultXmlnsRecognizesCornerstoneWithoutAvaloniaNamespace()
	{
		var metadata = new CompletionMetadata();
		metadata.AddType(Utils.CornerstoneNamespace, new MetadataType("Button") { FullName = "Cornerstone.Controls.Button" });
		Assert.IsTrue(metadata.IsTypeInDefaultXmlns("Button"));
		Assert.IsFalse(metadata.Namespaces.ContainsKey(Utils.AvaloniaNamespace));
	}

	#endregion
}
