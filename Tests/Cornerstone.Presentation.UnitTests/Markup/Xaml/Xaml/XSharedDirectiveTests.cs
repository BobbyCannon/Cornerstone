#nullable enable

#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class XSharedDirectiveTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldCreateNewInstanceWhereXShareIsFalse()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			const string xaml = """
								<Window xmlns="https://github.com/BobbyCannon/Cornerstone"
								        xmlns:sys="clr-namespace:System;assembly=netstandard"
								        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
								    <Window.Resources>
								        <ColumnDefinitions x:Key="ImplicitSharedResource">
								            <ColumnDefinition Width="150" />
								            <ColumnDefinition Width="10" />
								            <ColumnDefinition Width="Auto" />
								         </ColumnDefinitions>
								         <ColumnDefinitions x:Key="NotSharedResource"
								                            x:Shared="false">
								            <ColumnDefinition Width="150" />
								            <ColumnDefinition Width="10" />
								            <ColumnDefinition Width="Auto" />
								         </ColumnDefinitions>
								    </Window.Resources>
								</Window>
								""";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			window.ApplyTemplate();

			var implicitSharedInstance1 = window.FindResource("ImplicitSharedResource");
			CornerstoneTest.IsNotNull(implicitSharedInstance1);
			var implicitSharedInstance2 = window.FindResource("ImplicitSharedResource");
			CornerstoneTest.IsNotNull(implicitSharedInstance2);

			CornerstoneTest.Same(implicitSharedInstance1, implicitSharedInstance2);

			var notSharedResource1 = window.FindResource("NotSharedResource");
			CornerstoneTest.IsNotNull(notSharedResource1);

			var notSharedResource2 = window.FindResource("NotSharedResource");
			CornerstoneTest.IsNotNull(notSharedResource2);

			CornerstoneTest.NotSame(notSharedResource1, notSharedResource2);

			CornerstoneTest.AreEqual(notSharedResource1.ToString(), notSharedResource2.ToString());
		}
	}

	#endregion
}