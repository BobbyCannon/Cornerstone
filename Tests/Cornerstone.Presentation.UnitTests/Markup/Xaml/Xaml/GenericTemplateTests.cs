#nullable enable

#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Markup.Xaml.Templates;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

public class SampleTemplatedObject : StyledElement
{
	#region Properties

	[Content]
	public List<SampleTemplatedObject> Content { get; set; } = new();

	public string? Foo { get; set; }

	#endregion
}

public class SampleTemplatedObjectTemplate
{
	#region Properties

	[Content]
	[TemplateContent(TemplateResultType = typeof(SampleTemplatedObject))]
	public object? Content { get; set; }

	#endregion
}

public class SampleTemplatedObjectContainer
{
	#region Properties

	public SampleTemplatedObjectTemplate? Template { get; set; }

	#endregion
}

[TestClass]
public class GenericTemplateTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void DataTemplateCanBeEmpty()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<s:SampleTemplatedObjectContainer xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:sys='clr-namespace:System;assembly=netstandard'
        xmlns:s='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <s:SampleTemplatedObjectContainer.Template>
        <s:SampleTemplatedObjectTemplate>
            <s:SampleTemplatedObject x:Name='root'>
                <s:SampleTemplatedObject x:Name='child1' Foo='foo' />
                <s:SampleTemplatedObject x:Name='child2' Foo='bar' />
            </s:SampleTemplatedObject>
        </s:SampleTemplatedObjectTemplate>
    </s:SampleTemplatedObjectContainer.Template>
</s:SampleTemplatedObjectContainer>";
			var container =
				(SampleTemplatedObjectContainer) CornerstoneRuntimeXamlLoader.Load(xaml,
					typeof(GenericTemplateTests).Assembly);
			var res = TemplateContent.Load<SampleTemplatedObject>(container.Template!.Content)!;
			CornerstoneTest.AreEqual(res.Result, res.NameScope.Find("root"));
			CornerstoneTest.AreEqual(res.Result.Content[0], res.NameScope.Find("child1"));
			CornerstoneTest.AreEqual(res.Result.Content[1], res.NameScope.Find("child2"));
			CornerstoneTest.AreEqual("foo", res.Result.Content[0].Foo);
			CornerstoneTest.AreEqual("bar", res.Result.Content[1].Foo);
		}
	}

	#endregion
}