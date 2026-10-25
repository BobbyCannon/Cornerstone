using Cornerstone.VisualStudio.Core.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cornerstone.VisualStudio.Tests.Parsing;

[TestClass]
public class XamlDocumentTests
{
	[TestMethod]
	[DataRow("<Grid></Grid>")]
	[DataRow("<Grid><Button /></Grid>")]
	[DataRow("<Button Width=\"100\" />")]
	[DataRow("<Grid>\n  <TextBlock Text=\"{Binding CompareChangesCount}\" />\n</Grid>")]
	public void IsWellFormedTrueForCompleteDocument(string xaml)
	{
		Assert.IsTrue(XamlDocument.IsWellFormed(xaml));
	}

	[TestMethod]
	public void IsWellFormedTrueForCompiledBindingOnKnownView()
	{
		var xaml = """
			<UserControl xmlns="https://github.com/BobbyCannon/Cornerstone"
					xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
					x:Class="Cornerstone.Editor.SourceControl.Views.CompareCommitsDetailView">
				<TextBlock Text="{Binding CompareChangesCount}" />
			</UserControl>
			""";

		Assert.IsTrue(XamlDocument.IsWellFormed(xaml));
	}

	[TestMethod]
	public void IsWellFormedFalseForNull()
	{
		Assert.IsFalse(XamlDocument.IsWellFormed(null));
	}

	[TestMethod]
	[DataRow("")]
	[DataRow("   ")]
	[DataRow("<")]
	[DataRow("<Grid>")]
	[DataRow("<Grid><Button /></Grid")]
	[DataRow("<Button Width=\"")]
	[DataRow("<Grid></Button>")]
	public void IsWellFormedFalseForBrokenMarkup(string xaml)
	{
		Assert.IsFalse(XamlDocument.IsWellFormed(xaml));
	}
}
