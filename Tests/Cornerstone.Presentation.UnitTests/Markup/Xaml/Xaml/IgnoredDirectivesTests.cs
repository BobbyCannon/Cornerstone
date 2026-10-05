#nullable enable

#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class IgnoredDirectivesTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void IgnoredDirectivesShouldCompile()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			const string xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:sys='clr-namespace:System;assembly=netstandard'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock x:Name='target' x:FieldModifier='Public' Text='Foo'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var target = window.GetControl<TextBlock>("target");

			window.ApplyTemplate();
			target.ApplyTemplate();

			CornerstoneTest.AreEqual("Foo", target.Text);
		}
	}

	#endregion
}