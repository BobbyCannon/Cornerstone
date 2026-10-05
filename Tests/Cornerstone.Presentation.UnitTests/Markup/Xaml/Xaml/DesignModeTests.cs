#nullable enable

#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.DesignTime;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Markup.Xaml.Templates;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class DesignModeTests : XamlTestBase
{
	#region Properties

	public static object? SomeStaticProperty { get; set; }

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void DesignModeDataContextShouldBeSet()
	{
		SomeStaticProperty = "123";

		var loaded = (UserControl) CornerstoneRuntimeXamlLoader
			.Load(@"
<UserControl 
    xmlns='https://github.com/BobbyCannon/Cornerstone'
    xmlns:d='http://schemas.microsoft.com/expression/blend/2008'
    xmlns:tests='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml'
    d:DataContext='{x:Static tests:DesignModeTests.SomeStaticProperty}'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'/>", typeof(XamlIlTests).Assembly,
				designMode: true);
		CornerstoneTest.AreEqual(Design.GetDataContext(loaded), SomeStaticProperty);
	}

	[PresentationTestMethod]
	public void DesignModePreviewWithReturnsOriginalControl()
	{
		using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
		{
			var obj = (Control) CornerstoneRuntimeXamlLoader.Load(@"
<Button xmlns='https://github.com/BobbyCannon/Cornerstone'>
    <Design.PreviewWith>
        <Border />
    </Design.PreviewWith>
</Button>", designMode: true);
			var preview = Design.CreatePreviewWithControl(obj);

			// Should return the original control, not the preview, as this is not supported to avoid stack overflows.
			CornerstoneTest.Same(obj, preview);
		}
	}

	[PresentationTestMethod]
	public void DesignModePreviewWithShouldBeIgnoredWithoutDesignMode()
	{
		using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
		{
			var obj = (Control) CornerstoneRuntimeXamlLoader.Load(@"
<Button xmlns='https://github.com/BobbyCannon/Cornerstone'>
    <Design.PreviewWith>
        <Template>
            <Border />
        </Template>
    </Design.PreviewWith>
</Button>", designMode: false);
			var preview = Design.CreatePreviewWithControl(obj);

			// Should return the original control, not the preview.
			CornerstoneTest.IsType<Button>(preview);
		}
	}

	[PresentationTestMethod]
	public void DesignModePreviewWithWorksWithIDataTemplate()
	{
		using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
		{
			var obj = (DataTemplate) CornerstoneRuntimeXamlLoader.Load(@"
<DataTemplate xmlns='https://github.com/BobbyCannon/Cornerstone'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              x:DataType='SolidColorBrush'>
    <Design.PreviewWith>
        <ContentControl>
            <ContentControl.Content>
                <SolidColorBrush Color='Red'/>
            </ContentControl.Content>
        </ContentControl>
    </Design.PreviewWith>
    <Border Background='{Binding}' />
</DataTemplate>", designMode: true);
			var preview = Design.CreatePreviewWithControl(obj);
			var previewContentControl = CornerstoneTest.IsType<ContentControl>(preview);
			previewContentControl.ApplyTemplate();
			previewContentControl.Presenter!.UpdateChild();
			var border = previewContentControl.FindDescendantOfType<Border>();
			CornerstoneTest.IsNotNull(border);
			CornerstoneTest.AreEqual(Colors.Red, (border.Background as ISolidColorBrush)?.Color);
		}
	}

	[PresentationTestMethod]
	public void DesignModePreviewWithWorksWithResourceDictionary()
	{
		using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
		{
			var obj = (ResourceDictionary) CornerstoneRuntimeXamlLoader.Load(@"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Design.PreviewWith>
        <Border Background='{DynamicResource PreviewBackground}' />
    </Design.PreviewWith>
    <SolidColorBrush x:Key='PreviewBackground' Color='Red'/>
</ResourceDictionary>", designMode: true);
			var preview = Design.CreatePreviewWithControl(obj);
			var previewBorder = CornerstoneTest.IsType<Border>(preview);
			CornerstoneTest.AreEqual(Colors.Red, (previewBorder.Background as ISolidColorBrush)?.Color);
		}
	}

	[PresentationTestMethod]
	public void DesignModePreviewWithWorksWithStyle()
	{
		using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
		{
			var obj = (Style) CornerstoneRuntimeXamlLoader.Load(@"
<Style xmlns='https://github.com/BobbyCannon/Cornerstone'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
       Selector='Border.preview-border' >
    <Design.PreviewWith>
        <Border Classes='preview-border' />
    </Design.PreviewWith>
    <Setter Property='Background' Value='Red'/>
</Style>", designMode: true);
			var preview = Design.CreatePreviewWithControl(obj);
			var previewBorder = CornerstoneTest.IsType<Border>(preview);
			previewBorder.ApplyStyling();
			CornerstoneTest.AreEqual(Colors.Red, (previewBorder.Background as ISolidColorBrush)?.Color);
		}
	}

	[PresentationTestMethod]
	public void DesignModePropertiesShouldBeIgnoredAtRuntimeAndSetInDesignMode()
	{
		using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
		{
			foreach (var designMode in new[] { true, false })
			{
				var obj = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone' 
        xmlns:d='http://schemas.microsoft.com/expression/blend/2008'
        xmlns:mc='http://schemas.openxmlformats.org/markup-compatibility/2006'
        mc:Ignorable='d'
        d:DataContext='data-context'
        d:DesignWidth='123'
        d:DesignHeight='321'>
</Window>", designMode: designMode);
				var context = Design.GetDataContext(obj);
				var width = Design.GetWidth(obj);
				var height = Design.GetHeight(obj);
				if (designMode)
				{
					CornerstoneTest.AreEqual("data-context", context);
					CornerstoneTest.AreEqual(123, width);
					CornerstoneTest.AreEqual(321, height);
				}
				else
				{
					CornerstoneTest.IsFalse(obj.IsSet(Design.DataContextProperty));
					CornerstoneTest.IsFalse(obj.IsSet(Design.WidthProperty));
					CornerstoneTest.IsFalse(obj.IsSet(Design.HeightProperty));
				}
			}
		}
	}

	// https://github.com/AvaloniaUI/Avalonia/issues/2570
	[PresentationTestMethod]
	public void DesignModeThrowsOnInvalidStaticPropertyReference()
	{
		SomeStaticProperty = "123";
		var ex = CornerstoneTest.Throws<Exception>(() => CornerstoneRuntimeXamlLoader
			.Load(@"
<UserControl 
    xmlns='https://github.com/BobbyCannon/Cornerstone'
    xmlns:d='http://schemas.microsoft.com/expression/blend/2008'
    xmlns:tests='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml'
    d:DataContext='{x:Static tests:DesignModeTests.SomeStaticPropery}'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'/>", typeof(XamlIlTests).Assembly,
				designMode: true));
		CornerstoneTest.Contains(ex.Message, "Unable to resolve ");
		CornerstoneTest.Contains(ex.Message, " as static field, property, constant or enum value");
	}

	#endregion
}