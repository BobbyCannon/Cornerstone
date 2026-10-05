#nullable enable

#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;

[TestClass]
public class OnFormFactorExtensionTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldResolveDefaultValue()
	{
		using (PresentationLocator.EnterScope())
		{
			PresentationLocator.CurrentMutable.Bind<IRuntimePlatform>()
				.ToConstant(new TestRuntimePlatform(false, false));

			var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock Text='{OnFormFactor Default=""Hello World""}'/>
</UserControl>";

			var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = (TextBlock) userControl.Content!;

			CornerstoneTest.AreEqual("Hello World", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	[DataRow(false, true, "Im Mobile")]
	[DataRow(true, false, "Im Desktop")]
	[DataRow(false, false, "Default value")]
	public void ShouldResolveExpectedValuePerPlatform(bool isDesktop, bool isMobile, string expectedResult)
	{
		using (PresentationLocator.EnterScope())
		{
			PresentationLocator.CurrentMutable.Bind<IRuntimePlatform>()
				.ToConstant(new TestRuntimePlatform(isDesktop, isMobile));

			var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock Text='{OnFormFactor ""Default value"",
                                 Mobile=""Im Mobile"", Desktop=""Im Desktop""}'/>
</UserControl>";

			var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = (TextBlock) userControl.Content!;

			CornerstoneTest.AreEqual(expectedResult, textBlock.Text);
		}
	}

	#endregion

	#region Classes

	private class TestRuntimePlatform : StandardRuntimePlatform
	{
		#region Fields

		private readonly bool _isDesktop;
		private readonly bool _isMobile;

		#endregion

		#region Constructors

		public TestRuntimePlatform(bool isDesktop, bool isMobile)
		{
			_isDesktop = isDesktop;
			_isMobile = isMobile;
		}

		#endregion

		#region Methods

		public override RuntimePlatformInfo GetRuntimeInfo()
		{
			return new RuntimePlatformInfo { IsDesktop = _isDesktop, IsMobile = _isMobile };
		}

		#endregion
	}

	#endregion
}