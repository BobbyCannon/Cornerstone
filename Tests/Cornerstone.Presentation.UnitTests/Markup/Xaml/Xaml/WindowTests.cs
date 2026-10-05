#nullable enable

#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Chrome;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class WindowTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void CanSpecifyTransparencyLevelHint()
	{
		using var app = UnitTestApplication.Start(TestServices.MockWindowingPlatform);
		var xaml = @"<Window xmlns='https://github.com/BobbyCannon/Cornerstone' TransparencyLevelHint='Blur,Transparent,None'/>";

		var target = CornerstoneRuntimeXamlLoader.Parse<Window>(xaml);

		CornerstoneTest.AreEqual(new[]
		{
			WindowTransparencyLevel.Blur,
			WindowTransparencyLevel.Transparent,
			WindowTransparencyLevel.None
		}, target.TransparencyLevelHint);
	}

	#endregion
}