#nullable enable

#region References

using System.Linq;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Markup.Xaml.Templates;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class TreeDataTemplateTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void BindingShouldBeAssignedToItemsSourceInsteadOfBound()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
		{
			var xaml = "<DataTemplates xmlns='https://github.com/BobbyCannon/Cornerstone'><TreeDataTemplate DataType='Control' ItemsSource='{Binding}'/></DataTemplates>";
			var templates = (DataTemplates) CornerstoneRuntimeXamlLoader.Load(xaml);
			var template = (TreeDataTemplate) templates.First();

			CornerstoneTest.IsType<ReflectionBinding>(template.ItemsSource);
		}
	}

	[PresentationTestMethod]
	public void XDataTypeShouldBeAssignedToClrProperty()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
		{
			var xaml = @"
<DataTemplates xmlns='https://github.com/BobbyCannon/Cornerstone'
               xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TreeDataTemplate x:DataType='x:String' />
</DataTemplates>";
			var templates = (DataTemplates) CornerstoneRuntimeXamlLoader.Load(xaml);
			var template = (TreeDataTemplate) templates.First();

			CornerstoneTest.AreEqual(typeof(string), template.DataType);
		}
	}

	#endregion
}