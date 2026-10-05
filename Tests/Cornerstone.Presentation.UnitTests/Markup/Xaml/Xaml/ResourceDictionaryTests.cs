#nullable enable

#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class ResourceDictionaryTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ClosestResourceShouldBeReferenced()
	{
		using (StyledWindow())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='Red' Color='Red' />
        <StaticResource x:Key='Red2' ResourceKey='Red' />
    </Window.Resources>
    <Button>
        <Button.Resources>
            <SolidColorBrush x:Key='Red' Color='Blue' />
        </Button.Resources>
    </Button>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var windowResources = (ResourceDictionary) window.Resources;
			var buttonResources = (ResourceDictionary) ((Button) window.Content!).Resources;

			var brush = CornerstoneTest.IsType<SolidColorBrush>(windowResources["Red2"]);
			CornerstoneTest.AreEqual(Colors.Red, brush.Color);

			CornerstoneTest.IsFalse(windowResources.ContainsDeferredKey("Red"));
			CornerstoneTest.IsFalse(windowResources.ContainsDeferredKey("Red2"));

			CornerstoneTest.IsTrue(buttonResources.ContainsDeferredKey("Red"));
		}
	}

	[PresentationTestMethod]
	public void DynamicResourceFindsResourceInParentDictionary()
	{
		using (StyledWindow())
		{
			var documents = new[]
			{
				new RuntimeXamlLoaderDocument(new Uri("csres://Cornerstone.Presentation.UnitTests/dict.xaml"), @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <SolidColorBrush x:Key='RedBrush' Color='{DynamicResource Red}'/>
</ResourceDictionary>"),
				new RuntimeXamlLoaderDocument(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source='csres://Cornerstone.Presentation.UnitTests/dict.xaml'/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
        <Color x:Key='Red'>Red</Color>
    </Window.Resources>
    <Button Name='button' Background='{DynamicResource RedBrush}'/>
</Window>")
			};

			var loaded = CornerstoneXamlIlRuntimeLoader.LoadGroup(documents);
			var window = CornerstoneTest.IsType<Window>(loaded[1]);
			var button = window.GetControl<Button>("button");

			var brush = CornerstoneTest.IsType<SolidColorBrush>(button.Background);
			CornerstoneTest.AreEqual(Colors.Red, brush.Color);

			window.Resources["Red"] = Colors.Green;

			CornerstoneTest.AreEqual(Colors.Green, brush.Color);
		}
	}

	[PresentationTestMethod]
	public void DynamicallyChangingReferencedResourcesWorksWithDynamicResource()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <UserControl.Resources>
    <Color x:Key='color'>Red</Color>
    <SolidColorBrush x:Key='brush' Color='{DynamicResource color}' />
  </UserControl.Resources>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);

		CornerstoneTest.AreEqual(Colors.Red, ((ISolidColorBrush) userControl.FindResource("brush")!).Color);

		userControl.Resources.Remove("color");
		CornerstoneTest.AreEqual(default, ((ISolidColorBrush) userControl.FindResource("brush")!).Color);

		userControl.Resources.Add("color", Colors.Blue);
		CornerstoneTest.AreEqual(Colors.Blue, ((ISolidColorBrush) userControl.FindResource("brush")!).Color);
	}

	[PresentationTestMethod]
	public void ItemAddedToResourceDictionaryIsUnDeferredOnRead()
	{
		using (StyledWindow())
		{
			var xaml = @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='Red' Color='Red' />
</ResourceDictionary>";
			var resources = (ResourceDictionary) CornerstoneRuntimeXamlLoader.Load(xaml);

			CornerstoneTest.IsTrue(resources.ContainsDeferredKey("Red"));

			CornerstoneTest.IsType<SolidColorBrush>(resources["Red"]);

			CornerstoneTest.IsFalse(resources.ContainsDeferredKey("Red"));
		}
	}

	[PresentationTestMethod]
	public void ItemCanBeStaticReferencedAsDeferred()
	{
		using (StyledWindow())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='Red' Color='Red' />
    </Window.Resources>
    <Button>
        <Button.Resources>
            <StaticResource x:Key='Red2' ResourceKey='Red' />
        </Button.Resources>
    </Button>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var windowResources = (ResourceDictionary) window.Resources;
			var buttonResources = (ResourceDictionary) ((Button) window.Content!).Resources;

			CornerstoneTest.IsTrue(windowResources.ContainsDeferredKey("Red"));
			CornerstoneTest.IsTrue(buttonResources.ContainsDeferredKey("Red2"));
		}
	}

	[PresentationTestMethod]
	public void ItemIsAddedToResourceDictionaryAsDeferred()
	{
		using (StyledWindow())
		{
			var xaml = @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='Red' Color='Red' />
</ResourceDictionary>";
			var resources = (ResourceDictionary) CornerstoneRuntimeXamlLoader.Load(xaml);

			CornerstoneTest.IsTrue(resources.ContainsDeferredKey("Red"));
		}
	}

	[PresentationTestMethod]
	public void ItemIsAddedToStyleResourcesAsDeferred()
	{
		using (StyledWindow())
		{
			var xaml = @"
<Style xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style.Resources>
        <SolidColorBrush x:Key='Red' Color='Red' />
    </Style.Resources>
</Style>";
			var style = (Style) CornerstoneRuntimeXamlLoader.Load(xaml);
			var resources = (ResourceDictionary) style.Resources;

			CornerstoneTest.IsTrue(resources.ContainsDeferredKey("Red"));
		}
	}

	[PresentationTestMethod]
	public void ItemIsAddedToStylesResourcesAsDeferred()
	{
		using (StyledWindow())
		{
			var xaml = @"
<Styles xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Styles.Resources>
        <SolidColorBrush x:Key='Red' Color='Red' />
    </Styles.Resources>
</Styles>";
			var style = (Styles) CornerstoneRuntimeXamlLoader.Load(xaml);
			var resources = (ResourceDictionary) style.Resources;

			CornerstoneTest.IsTrue(resources.ContainsDeferredKey("Red"));
		}
	}

	[PresentationTestMethod]
	public void ItemIsAddedToWindowMergedDictionariesAsDeferred()
	{
		using (StyledWindow())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary>
                    <SolidColorBrush x:Key='Red' Color='Red' />
                </ResourceDictionary>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Window.Resources>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var resources = (ResourceDictionary) window.Resources.MergedDictionaries[0];

			CornerstoneTest.IsTrue(resources.ContainsDeferredKey("Red"));
		}
	}

	[PresentationTestMethod]
	public void ItemIsAddedToWindowResourcesAsDeferred()
	{
		using (StyledWindow())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='Red' Color='Red' />
    </Window.Resources>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var resources = (ResourceDictionary) window.Resources;

			CornerstoneTest.IsTrue(resources.ContainsDeferredKey("Red"));
		}
	}

	[PresentationTestMethod]
	public void ItemStaticReferencedIsUnDeferredOnRead()
	{
		using (StyledWindow())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='Red' Color='Red' />
    </Window.Resources>
    <Button>
        <Button.Resources>
            <StaticResource x:Key='Red2' ResourceKey='Red' />
        </Button.Resources>
    </Button>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var windowResources = (ResourceDictionary) window.Resources;
			var buttonResources = (ResourceDictionary) ((Button) window.Content!).Resources;

			CornerstoneTest.IsType<SolidColorBrush>(buttonResources["Red2"]);

			CornerstoneTest.IsFalse(windowResources.ContainsDeferredKey("Red"));
			CornerstoneTest.IsFalse(buttonResources.ContainsDeferredKey("Red2"));
		}
	}

	[PresentationTestMethod]
	public void NamedItemIsAddedToResourcesShouldNotBeDeferred()
	{
		// Since Named items can be accessed through the NameScope, we cannot delay their initialization.
		using (StyledWindow())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <Panel x:Name='MyPanel' x:Key='MyPanel' />
    </Window.Resources>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var resources = (ResourceDictionary) window.Resources;

			CornerstoneTest.IsFalse(resources.ContainsDeferredKey("MyPanel"));
			CornerstoneTest.IsTrue(resources.ContainsKey("MyPanel"));
			CornerstoneTest.IsType<Panel>(window.Find<Panel>("MyPanel"));
		}
	}

	[PresentationTestMethod]
	public void ResourceDictionaryCanBePutInsideOfResourceDictionary()
	{
		using (StyledWindow())
		{
			var xaml = @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary x:Key='NotAThemeVariantKey' />
</ResourceDictionary>";
			var resources = (ResourceDictionary) CornerstoneRuntimeXamlLoader.Load(xaml);
			var nested = (ResourceDictionary?) resources["NotAThemeVariantKey"];

			CornerstoneTest.IsNotNull(nested);
		}
	}

	[PresentationTestMethod]
	public void ShouldBePossibleToRedefineReferencedResource()
	{
		using (StyledWindow())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <Color x:Key='SystemAccentColor'>#aaa</Color>
    </Window.Resources>
    <UserControl>
        <UserControl.Resources>
            <StaticResource x:Key='SystemAccentColor' ResourceKey='SystemAccentColor' />
        </UserControl.Resources>
    </UserControl>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var windowResources = (ResourceDictionary) window.Resources;
			var innerResources = (ResourceDictionary) ((UserControl) window.Content!).Resources;

			var winButtonTheme = CornerstoneTest.IsType<Color>(windowResources["SystemAccentColor"]);
			var innerButtonTheme = CornerstoneTest.IsType<Color>(innerResources["SystemAccentColor"]);
			CornerstoneTest.AreEqual(winButtonTheme, innerButtonTheme);
		}
	}

	[PresentationTestMethod]
	public void ShouldBePossibleToRedefineReferencedResourceControlTheme()
	{
		using (StyledWindow())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <ControlTheme x:Key='{x:Type Button}' TargetType='Button' />
    </Window.Resources>
    <UserControl>
        <UserControl.Resources>
            <ControlTheme x:Key='{x:Type Button}' TargetType='Button' BasedOn='{StaticResource {x:Type Button}}' />
        </UserControl.Resources>
    </UserControl>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var windowResources = (ResourceDictionary) window.Resources;
			var innerResources = (ResourceDictionary) ((UserControl) window.Content!).Resources;

			var winButtonTheme = CornerstoneTest.IsType<ControlTheme>(windowResources[typeof(Button)]);
			var innerButtonTheme = CornerstoneTest.IsType<ControlTheme>(innerResources[typeof(Button)]);
			CornerstoneTest.AreEqual(winButtonTheme, innerButtonTheme.BasedOn);
		}
	}

	[PresentationTestMethod]
	public void StaticResourceWorksInResourceDictionary()
	{
		using (StyledWindow())
		{
			var xaml = @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <Color x:Key='Red'>Red</Color>
  <SolidColorBrush x:Key='RedBrush' Color='{StaticResource Red}'/>
</ResourceDictionary>";
			var resources = (ResourceDictionary) CornerstoneRuntimeXamlLoader.Load(xaml);
			var brush = (SolidColorBrush) resources["RedBrush"]!;

			CornerstoneTest.AreEqual(Colors.Red, brush.Color);
		}
	}

	[PresentationTestMethod]
	public void ValueTypeWithCtorConverterShouldNotBeDeferred()
	{
		using (StyledWindow())
		{
			var xaml = @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Thickness x:Key='Margin'>1 1 1 1</Thickness>
</ResourceDictionary>";
			var resources = (ResourceDictionary) CornerstoneRuntimeXamlLoader.Load(xaml);

			CornerstoneTest.IsFalse(resources.ContainsDeferredKey("Margin"));
			CornerstoneTest.IsType<Thickness>(resources["Margin"]);
		}
	}

	[PresentationTestMethod]
	public void ValueTypeWithParseConverterShouldNotBeDeferred()
	{
		using (StyledWindow())
		{
			var xaml = @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Color x:Key='Red'>Red</Color>
</ResourceDictionary>";
			var resources = (ResourceDictionary) CornerstoneRuntimeXamlLoader.Load(xaml);

			CornerstoneTest.IsFalse(resources.ContainsDeferredKey("Red"));
			CornerstoneTest.IsType<Color>(resources["Red"]);
		}
	}

	private IDisposable StyledWindow(params (string, string)[] assets)
	{
		var services = TestServices.StyledWindow.With(
			theme: () => new Styles
			{
				WindowStyle()
			});

		return UnitTestApplication.Start(services);
	}

	private Style WindowStyle()
	{
		return new Style(x => x.OfType<Window>())
		{
			Setters =
			{
				new Setter(
					Window.TemplateProperty,
					new FuncControlTemplate<Window>((x, scope) =>
						new ContentPresenter
						{
							Name = "PART_ContentPresenter",
							[!ContentPresenter.ContentProperty] = x[!Window.ContentProperty]
						}.RegisterInNameScope(scope)))
			}
		};
	}

	#endregion
}