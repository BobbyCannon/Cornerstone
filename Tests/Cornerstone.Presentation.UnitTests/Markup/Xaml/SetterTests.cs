#nullable enable

#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml;

[TestClass]
public class SetterTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void SetterTargetTypeShouldUnderstandTypeFromXmlns()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<av:Animation xmlns:av='https://github.com/BobbyCannon/Cornerstone' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:SetterTargetType='av:ContentControl'>
    <av:KeyFrame>
        <av:Setter Property='Content' Value='{av:Binding}'/>
    </av:KeyFrame>
    <av:KeyFrame>
        <av:Setter Property='Content' Value='{av:Binding}'/>
    </av:KeyFrame> 
</av:Animation>";
			var animation = (Animation.Animation) CornerstoneRuntimeXamlLoader.Load(xaml);
			var setter = (Setter) animation.Children[0].Setters[0];

			CornerstoneTest.IsNotNull(setter.Property);
			CornerstoneTest.AreEqual(typeof(ContentControl), setter.Property.OwnerType);
		}
	}

	[PresentationTestMethod]
	public void SetterTargetTypeShouldUnderstandxTypeExtensions()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Animation xmlns='https://github.com/BobbyCannon/Cornerstone' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:SetterTargetType='{x:Type ContentControl}'>
    <KeyFrame>
        <Setter Property='Content' Value='{Binding}'/>
    </KeyFrame>
    <KeyFrame>
        <Setter Property='Content' Value='{Binding}'/>
    </KeyFrame> 
</Animation>";
			var animation = (Animation.Animation) CornerstoneRuntimeXamlLoader.Load(xaml);
			var setter = (Setter) animation.Children[0].Setters[0];

			CornerstoneTest.IsNotNull(setter.Property);
			CornerstoneTest.AreEqual(typeof(ContentControl), setter.Property.OwnerType);
		}
	}

	#endregion
}