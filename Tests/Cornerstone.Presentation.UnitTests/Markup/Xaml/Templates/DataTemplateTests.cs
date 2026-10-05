#nullable enable

#region References

using Cornerstone.Presentation.Markup.Xaml.Templates;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Templates;

[TestClass]
public class DataTemplateTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void DataTemplateShouldMatchDataOfDerivedType()
	{
		var target = new DataTemplate { DataType = typeof(Class1) };
		var data = new Class2();

		CornerstoneTest.IsTrue(target.Match(data));
	}

	[PresentationTestMethod]
	public void DataTemplateShouldMatchDataOfType()
	{
		var target = new DataTemplate { DataType = typeof(Class1) };
		var data = new Class1();

		CornerstoneTest.IsTrue(target.Match(data));
	}

	#endregion

	#region Classes

	private class Class1
	{
	}

	private class Class2 : Class1
	{
	}

	#endregion
}