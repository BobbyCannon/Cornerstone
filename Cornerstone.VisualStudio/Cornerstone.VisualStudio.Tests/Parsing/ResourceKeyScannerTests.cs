using Cornerstone.VisualStudio.Core.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cornerstone.VisualStudio.Tests.Parsing;

[TestClass]
public class ResourceKeyScannerTests
{
	[TestMethod]
	public void FindKeysEmptyReturnsEmpty()
	{
		Assert.AreEqual(0, System.Linq.Enumerable.Count(ResourceKeyScanner.FindKeys(null)));
		Assert.AreEqual(0, System.Linq.Enumerable.Count(ResourceKeyScanner.FindKeys("")));
		Assert.AreEqual(0, System.Linq.Enumerable.Count(ResourceKeyScanner.FindKeys("<Grid />")));
	}

	[TestMethod]
	public void FindKeysDoubleAndSingleQuoted()
	{
		var xaml = """
			<UserControl.Resources>
			  <SolidColorBrush x:Key="MyBrush" Color="Red" />
			  <x:Double x:Key='MyDouble'>1</x:Double>
			</UserControl.Resources>
			""";

		var keys = ResourceKeyScanner.FindKeys(xaml);
		Assert.AreSequenceEqual(["MyBrush", "MyDouble"], keys);
	}

	[TestMethod]
	public void FindKeysDedupesAndSkipsMarkupExtensionKeys()
	{
		var xaml = """
			<SolidColorBrush x:Key="Same" />
			<SolidColorBrush x:Key="Same" />
			<local:Foo x:Key="{x:Type Button}" />
			""";

		var keys = ResourceKeyScanner.FindKeys(xaml);
		Assert.AreSequenceEqual(["Same"], keys);
	}

	[TestMethod]
	public void FindKeysWorksOnIncompleteDocument()
	{
		var xaml = """
			<UserControl.Resources>
			  <SolidColorBrush x:Key="Partial
			""";

		// Incomplete attribute value — no closed quote, so no match (resilient, no throw).
		Assert.AreEqual(0, System.Linq.Enumerable.Count(ResourceKeyScanner.FindKeys(xaml)));

		xaml = """
			<UserControl.Resources>
			  <SolidColorBrush x:Key="Ok" />
			  <Button Background="{StaticResource 
			""";
		Assert.AreSequenceEqual(["Ok"], ResourceKeyScanner.FindKeys(xaml));
	}
}
