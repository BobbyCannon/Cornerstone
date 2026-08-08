using Cornerstone.VisualStudio.Core.Parsing;
using Xunit;

namespace Cornerstone.VisualStudio.Tests.Parsing;

public class ResourceKeyScannerTests
{
	[Fact]
	public void FindKeysEmptyReturnsEmpty()
	{
		Assert.Empty(ResourceKeyScanner.FindKeys(null));
		Assert.Empty(ResourceKeyScanner.FindKeys(""));
		Assert.Empty(ResourceKeyScanner.FindKeys("<Grid />"));
	}

	[Fact]
	public void FindKeysDoubleAndSingleQuoted()
	{
		var xaml = """
			<UserControl.Resources>
			  <SolidColorBrush x:Key="MyBrush" Color="Red" />
			  <x:Double x:Key='MyDouble'>1</x:Double>
			</UserControl.Resources>
			""";

		var keys = ResourceKeyScanner.FindKeys(xaml);
		Assert.Equal(["MyBrush", "MyDouble"], keys);
	}

	[Fact]
	public void FindKeysDedupesAndSkipsMarkupExtensionKeys()
	{
		var xaml = """
			<SolidColorBrush x:Key="Same" />
			<SolidColorBrush x:Key="Same" />
			<local:Foo x:Key="{x:Type Button}" />
			""";

		var keys = ResourceKeyScanner.FindKeys(xaml);
		Assert.Equal(["Same"], keys);
	}

	[Fact]
	public void FindKeysWorksOnIncompleteDocument()
	{
		var xaml = """
			<UserControl.Resources>
			  <SolidColorBrush x:Key="Partial
			""";

		// Incomplete attribute value — no closed quote, so no match (resilient, no throw).
		Assert.Empty(ResourceKeyScanner.FindKeys(xaml));

		xaml = """
			<UserControl.Resources>
			  <SolidColorBrush x:Key="Ok" />
			  <Button Background="{StaticResource 
			""";
		Assert.Equal(["Ok"], ResourceKeyScanner.FindKeys(xaml));
	}
}
