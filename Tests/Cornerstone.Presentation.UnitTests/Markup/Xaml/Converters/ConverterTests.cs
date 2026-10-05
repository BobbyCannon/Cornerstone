#nullable enable

#region References

using System;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Converters;

[TestClass]
public class ConverterTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void Bug2228RelativeUrisShouldBeCorrectlyParsed()
	{
		var testClass = typeof(TestClassWithUri);
		var parsed = CornerstoneRuntimeXamlLoader.Parse<TestClassWithUri>(
			$"<{testClass.Name} xmlns='clr-namespace:{testClass.Namespace}' Uri='/test'/>", testClass.Assembly);

		CornerstoneTest.IsNotNull(parsed.Uri);
		CornerstoneTest.IsFalse(parsed.Uri.IsAbsoluteUri);
	}

	#endregion
}

public class TestClassWithUri
{
	#region Properties

	public Uri? Uri { get; set; }

	#endregion
}