#nullable enable

#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Documents;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Markup;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Markup.Xaml.MarkupExtensions;
using Cornerstone.Presentation.Markup.Xaml.Templates;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class ThemeDictionariesTests : XamlTestBase
{
	#region Properties

	public static ThemeVariant Custom { get; } = new(nameof(Custom), ThemeVariant.Light);

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void CustomThemeCanBeDefinedInThemeDictionaries()
	{
		var themeVariantScope = (ThemeVariantScope) CornerstoneRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/BobbyCannon/Cornerstone'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'
              RequestedThemeVariant='Light'>
    <ThemeVariantScope.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Dark'>
                    <SolidColorBrush x:Key='DemoBackground'>Black</SolidColorBrush>
                </ResourceDictionary>
                <ResourceDictionary x:Key='Light'>
                    <SolidColorBrush x:Key='DemoBackground'>White</SolidColorBrush>
                </ResourceDictionary>
                <ResourceDictionary x:Key='{x:Static local:ThemeDictionariesTests.Custom}'>
                    <SolidColorBrush x:Key='DemoBackground'>Pink</SolidColorBrush>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </ThemeVariantScope.Resources>

    <Border Name='border' Background='{DynamicResource DemoBackground}'/>
</ThemeVariantScope>");
		var border = (Border) themeVariantScope.Child!;

		themeVariantScope.RequestedThemeVariant = Custom;

		CornerstoneTest.AreEqual(Colors.Pink, ((ISolidColorBrush) border.Background!).Color);
	}

	[PresentationTestMethod]
	public void CustomThemeFallbacksToInheritThemeDynamicResource()
	{
		var themeVariantScope = (ThemeVariantScope) CornerstoneRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/BobbyCannon/Cornerstone'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              RequestedThemeVariant='Light'>
   <ThemeVariantScope.Resources>
        <ResourceDictionary>                
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Dark'>
                    <SolidColorBrush x:Key='DemoBackground'>Black</SolidColorBrush>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </ThemeVariantScope.Resources> 
    <Border Background='{DynamicResource DemoBackground}' />
</ThemeVariantScope>");
		var border = (Border) themeVariantScope.Child!;

		themeVariantScope.RequestedThemeVariant = new ThemeVariant("Custom", ThemeVariant.Dark);

		CornerstoneTest.AreEqual(Colors.Black, ((ISolidColorBrush) border.Background!).Color);
	}

	[PresentationTestMethod]
	public void CustomThemeFallbacksToInheritThemeStaticResource()
	{
		var themeVariantScope = (ThemeVariantScope) CornerstoneRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/BobbyCannon/Cornerstone'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ThemeVariantScope.RequestedThemeVariant>
        <ThemeVariant>
            <x:Arguments>
                <x:String>Custom</x:String>
                <ThemeVariant>Dark</ThemeVariant>
            </x:Arguments>
        </ThemeVariant>
    </ThemeVariantScope.RequestedThemeVariant>
   <ThemeVariantScope.Resources>
        <ResourceDictionary>                
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Dark'>
                    <SolidColorBrush x:Key='DemoBackground'>Black</SolidColorBrush>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </ThemeVariantScope.Resources> 

    <Border Background='{StaticResource DemoBackground}' />
</ThemeVariantScope>");
		var border = (Border) themeVariantScope.Child!;

		CornerstoneTest.AreEqual(Colors.Black, ((ISolidColorBrush) border.Background!).Color);
	}

	[PresentationTestMethod]
	public void DynamicResourceCanAccessResourcesOutsideOfThemeDictionaries()
	{
		var themeVariantScope = (ThemeVariantScope) CornerstoneRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/BobbyCannon/Cornerstone'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              RequestedThemeVariant='Light'>
    <ThemeVariantScope.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Dark'>
                    <SolidColorBrush x:Key='DemoBackground' Color='{DynamicResource TestColor1}' />
                </ResourceDictionary>
                <ResourceDictionary x:Key='Light'>
                    <SolidColorBrush x:Key='DemoBackground' Color='{DynamicResource TestColor2}' />
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
            <Color x:Key='TestColor1'>Black</Color>
            <Color x:Key='TestColor2'>White</Color>
        </ResourceDictionary>
    </ThemeVariantScope.Resources>

    <Border Name='border' Background='{DynamicResource DemoBackground}' />
</ThemeVariantScope>");
		var border = (Border) themeVariantScope.Child!;

		CornerstoneTest.AreEqual(Colors.White, ((ISolidColorBrush) border.Background!).Color);

		themeVariantScope.RequestedThemeVariant = ThemeVariant.Dark;

		CornerstoneTest.AreEqual(Colors.Black, ((ISolidColorBrush) border.Background!).Color);
	}

	[PresentationTestMethod]
	public void DynamicResourceInResourceProviderUpdatedWhenControlThemeChanged()
	{
		var themeVariantScope = new ThemeVariantScope
		{
			RequestedThemeVariant = ThemeVariant.Light,
			Resources = new ResourceDictionary
			{
				ThemeDictionaries =
				{
					[ThemeVariant.Dark] = new ResourceDictionary { ["DemoBackground"] = Brushes.Black },
					[ThemeVariant.Light] = new ResourceDictionary { ["DemoBackground"] = Brushes.White }
				}
			},
			Child = new Border()
		};

		var resources = (ResourceDictionary) CornerstoneRuntimeXamlLoader.Load(@"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <GeometryDrawing x:Key='Geo' Brush='{DynamicResource DemoBackground}' />
</ResourceDictionary>");

		themeVariantScope.Resources.MergedDictionaries.Add(resources);
		var geo = (GeometryDrawing?) themeVariantScope.FindResource("Geo");

		CornerstoneTest.IsNotNull(geo);
		CornerstoneTest.AreEqual(Colors.White, ((ISolidColorBrush) geo.Brush!).Color);

		themeVariantScope.RequestedThemeVariant = ThemeVariant.Dark;

		CornerstoneTest.AreEqual(Colors.Black, ((ISolidColorBrush) geo.Brush!).Color);
	}

	[PresentationTestMethod]
	public void DynamicResourceInsideControlInsideOfThemeDictionariesShouldUseControlThemeVariant()
	{
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(@"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.ThemeDictionaries>
        <ResourceDictionary x:Key='Light'>
            <Color x:Key='ResourceKey'>Green</Color>
            <Template x:Key='Template'>
                <ThemeVariantScope RequestedThemeVariant='Dark' TextElement.Foreground='{DynamicResource ResourceKey}' />
            </Template>
        </ResourceDictionary>
        <ResourceDictionary x:Key='Dark'>
            <Color x:Key='ResourceKey'>White</Color>
            <Template x:Key='Template'>
                <ThemeVariantScope RequestedThemeVariant='Light' TextElement.Foreground='{DynamicResource ResourceKey}' />
            </Template>
        </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>")
		};

		var parsed = CornerstoneXamlIlRuntimeLoader.LoadGroup(documents);
		var dictionary = (ResourceDictionary) parsed[0]!;

		dictionary.TryGetResource("Template", ThemeVariant.Dark, out var resource);
		var control = CornerstoneTest.IsType<ThemeVariantScope>((resource as Template)?.Build());
		control.Resources.MergedDictionaries.Add(dictionary);
		CornerstoneTest.AreEqual(Colors.Green, ((ISolidColorBrush) control[TextElement.ForegroundProperty]!).Color);
		control.Resources.MergedDictionaries.Remove(dictionary);

		dictionary.TryGetResource("Template", ThemeVariant.Light, out resource);
		control = CornerstoneTest.IsType<ThemeVariantScope>((resource as Template)?.Build());
		control.Resources.MergedDictionaries.Add(dictionary);
		CornerstoneTest.AreEqual(Colors.White, ((ISolidColorBrush) control[TextElement.ForegroundProperty]!).Color);
	}

	[PresentationTestMethod]
	public void DynamicResourceInsideOfThemeDictionariesShouldUseSameThemeKeyFromInnerFile()
	{
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Inner.xaml"), @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='InnerKey' Color='{DynamicResource OuterKey}' />
</ResourceDictionary>"),
			new RuntimeXamlLoaderDocument(@"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.ThemeDictionaries>
        <ResourceDictionary x:Key='Default'>
            <Color x:Key='OuterKey'>Green</Color>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source='csres://Tests/Inner.xaml'/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
        <ResourceDictionary x:Key='Dark'>
            <Color x:Key='OuterKey'>White</Color>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source='csres://Tests/Inner.xaml'/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>")
		};

		var parsed = CornerstoneXamlIlRuntimeLoader.LoadGroup(documents);
		var dictionary1 = (ResourceDictionary) parsed[0]!;
		var dictionary2 = (ResourceDictionary) parsed[1]!;
		var ownerApp = new Application(); // DynamicResource needs an owner to work
		ownerApp.RequestedThemeVariant = new ThemeVariant("FakeOne", null);
		ownerApp.Resources.MergedDictionaries.Add(dictionary1);
		ownerApp.Resources.MergedDictionaries.Add(dictionary2);

		dictionary2.TryGetResource("InnerKey", ThemeVariant.Dark, out var resource);
		var colorResource = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(resource);
		CornerstoneTest.AreEqual(Colors.White, colorResource.Color);

		dictionary2.TryGetResource("InnerKey", ThemeVariant.Light, out resource);
		colorResource = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(resource);
		CornerstoneTest.AreEqual(Colors.Green, colorResource.Color);
	}

	[PresentationTestMethod]
	public void DynamicResourceUpdatedWhenControlThemeChanged()
	{
		var themeVariantScope = (ThemeVariantScope) CornerstoneRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/BobbyCannon/Cornerstone'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              RequestedThemeVariant='Light'>
    <ThemeVariantScope.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Dark'>
                    <SolidColorBrush x:Key='DemoBackground'>Black</SolidColorBrush>
                </ResourceDictionary>
                <ResourceDictionary x:Key='Light'>
                    <SolidColorBrush x:Key='DemoBackground'>White</SolidColorBrush>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </ThemeVariantScope.Resources>

    <Border Name='border' Background='{DynamicResource DemoBackground}'/>
</ThemeVariantScope>");
		var border = (Border) themeVariantScope.Child!;

		CornerstoneTest.AreEqual(Colors.White, ((ISolidColorBrush) border.Background!).Color);

		var themeVariantKey = new string(['D', 'a', 'r', 'k']); // Ensure that a non-interned string works
		themeVariantScope.RequestedThemeVariant = new ThemeVariant(themeVariantKey, null);

		CornerstoneTest.AreEqual(Colors.Black, ((ISolidColorBrush) border.Background!).Color);
	}

	[PresentationTestMethod]
	public void DynamicResourceUpdatedWhenControlThemeChangedNoXaml()
	{
		var themeVariantScope = new ThemeVariantScope
		{
			RequestedThemeVariant = ThemeVariant.Light,
			Resources = new ResourceDictionary
			{
				ThemeDictionaries =
				{
					[ThemeVariant.Dark] = new ResourceDictionary { ["DemoBackground"] = Brushes.Black },
					[ThemeVariant.Light] = new ResourceDictionary { ["DemoBackground"] = Brushes.White }
				}
			},
			Child = new Border()
		};
		var border = (Border) themeVariantScope.Child!;
		border[!Border.BackgroundProperty] = new DynamicResourceExtension("DemoBackground");

		DelayedBinding.ApplyBindings(border);

		CornerstoneTest.AreEqual(Colors.White, ((ISolidColorBrush) border.Background!).Color);

		themeVariantScope.RequestedThemeVariant = ThemeVariant.Dark;

		CornerstoneTest.AreEqual(Colors.Black, ((ISolidColorBrush) border.Background!).Color);
	}

	[PresentationTestMethod]
	public void InnerDictionaryDoesNotAffectParentResources()
	{
		// It might be a nice feature, but neither the upstream UI framework nor UWP supports it.
		// Better to expect this limitation with a unit test. 
		var themeVariantScope = (ThemeVariantScope) CornerstoneRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/BobbyCannon/Cornerstone'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              RequestedThemeVariant='Light'>
    <ThemeVariantScope.Resources>
        <ResourceDictionary>
            <Color x:Key='TestColor'>Red</Color>
            <SolidColorBrush x:Key='DemoBackground' Color='{DynamicResource TestColor}' />
        </ResourceDictionary>
    </ThemeVariantScope.Resources>

    <Border Name='border' Background='{DynamicResource DemoBackground}'>
        <Border.Resources>
            <ResourceDictionary>
                <ResourceDictionary.ThemeDictionaries>
                    <ResourceDictionary x:Key='Dark'>
                        <Color x:Key='TestColor'>Black</Color>
                    </ResourceDictionary>
                    <ResourceDictionary x:Key='Light'>
                        <Color x:Key='TestColor'>White</Color>
                    </ResourceDictionary>
                </ResourceDictionary.ThemeDictionaries>
            </ResourceDictionary>
        </Border.Resources>
    </Border>
</ThemeVariantScope>");
		var border = (Border) themeVariantScope.Child!;

		CornerstoneTest.AreEqual(Colors.Red, ((ISolidColorBrush) border.Background!).Color);

		themeVariantScope.RequestedThemeVariant = ThemeVariant.Dark;

		CornerstoneTest.AreEqual(Colors.Red, ((ISolidColorBrush) border.Background!).Color);
	}

	[PresentationTestMethod]
	public void InnerResourceCanReferenceParentThemeDictionaries()
	{
		var themeVariantScope = (ThemeVariantScope) CornerstoneRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/BobbyCannon/Cornerstone'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              RequestedThemeVariant='Light'>
    <ThemeVariantScope.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Dark'>
                    <Color x:Key='TestColor'>Black</Color>
                </ResourceDictionary>
                <ResourceDictionary x:Key='Light'>
                    <Color x:Key='TestColor'>White</Color>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </ThemeVariantScope.Resources>

    <Border Name='border' Background='{DynamicResource DemoBackground}'>
        <Border.Resources>
            <ResourceDictionary>
                <SolidColorBrush x:Key='DemoBackground' Color='{DynamicResource TestColor}' />
            </ResourceDictionary>
        </Border.Resources>
    </Border>
</ThemeVariantScope>");
		var border = (Border) themeVariantScope.Child!;

		CornerstoneTest.AreEqual(Colors.White, ((ISolidColorBrush) border.Background!).Color);

		themeVariantScope.RequestedThemeVariant = ThemeVariant.Dark;

		CornerstoneTest.AreEqual(Colors.Black, ((ISolidColorBrush) border.Background!).Color);
	}

	[PresentationTestMethod]
	public void InnerThemeDictionariesWorksProperly()
	{
		var themeVariantScope = (ThemeVariantScope) CornerstoneRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/BobbyCannon/Cornerstone'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              RequestedThemeVariant='Light'>
    <Border Name='border' Background='{DynamicResource DemoBackground}'>
        <Border.Resources>
            <ResourceDictionary>
                <ResourceDictionary.ThemeDictionaries>
                    <ResourceDictionary x:Key='Dark'>
                        <SolidColorBrush x:Key='DemoBackground'>Black</SolidColorBrush>
                    </ResourceDictionary>
                    <ResourceDictionary x:Key='Light'>
                        <SolidColorBrush x:Key='DemoBackground'>White</SolidColorBrush>
                    </ResourceDictionary>
                </ResourceDictionary.ThemeDictionaries>
            </ResourceDictionary>
        </Border.Resources>
    </Border>
</ThemeVariantScope>");
		var border = (Border) themeVariantScope.Child!;

		CornerstoneTest.AreEqual(Colors.White, ((ISolidColorBrush) border.Background!).Color);

		themeVariantScope.RequestedThemeVariant = ThemeVariant.Dark;

		CornerstoneTest.AreEqual(Colors.Black, ((ISolidColorBrush) border.Background!).Color);
	}

	[PresentationTestMethod]
	public void IntermediateDynamicResourceUpdatedWhenControlThemeChanged()
	{
		var themeVariantScope = (ThemeVariantScope) CornerstoneRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/BobbyCannon/Cornerstone'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              RequestedThemeVariant='Light'>
    <ThemeVariantScope.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Dark'>
                    <Color x:Key='TestColor'>Black</Color>
                </ResourceDictionary>
                <ResourceDictionary x:Key='Light'>
                    <Color x:Key='TestColor'>White</Color>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
            <SolidColorBrush x:Key='DemoBackground' Color='{DynamicResource TestColor}' />
        </ResourceDictionary>
    </ThemeVariantScope.Resources>

    <Border Name='border' Background='{DynamicResource DemoBackground}'/>
</ThemeVariantScope>");
		var border = (Border) themeVariantScope.Child!;

		CornerstoneTest.AreEqual(Colors.White, ((ISolidColorBrush) border.Background!).Color);

		themeVariantScope.RequestedThemeVariant = ThemeVariant.Dark;

		CornerstoneTest.AreEqual(Colors.Black, ((ISolidColorBrush) border.Background!).Color);
	}

	[PresentationTestMethod]
	public void IntermediateStaticResourceCanBeReachedFromThemeDictionaries()
	{
		var themeVariantScope = (ThemeVariantScope) CornerstoneRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/BobbyCannon/Cornerstone'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              RequestedThemeVariant='Light'>
    <ThemeVariantScope.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Dark'>
                    <Color x:Key='TestColor'>Black</Color>
                    <StaticResource x:Key='DemoBackground' ResourceKey='TestColor' />
                </ResourceDictionary>
                <ResourceDictionary x:Key='Light'>
                    <Color x:Key='TestColor'>White</Color>
                    <StaticResource x:Key='DemoBackground' ResourceKey='TestColor' />
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </ThemeVariantScope.Resources>

    <Border Name='border' Background='{DynamicResource DemoBackground}'/>
</ThemeVariantScope>");
		var border = (Border) themeVariantScope.Child!;

		CornerstoneTest.AreEqual(Colors.White, ((ISolidColorBrush) border.Background!).Color);

		themeVariantScope.RequestedThemeVariant = ThemeVariant.Dark;

		CornerstoneTest.AreEqual(Colors.Black, ((ISolidColorBrush) border.Background!).Color);
	}

	[PresentationTestMethod]
	public void StaticResourceInsideOfThemeDictionariesShouldUseSameThemeKey()
	{
		var themeVariantScope = (ThemeVariantScope) CornerstoneRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/BobbyCannon/Cornerstone'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              RequestedThemeVariant='Light'>
    <ThemeVariantScope.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Dark'>
                    <Color x:Key='TestColor'>Black</Color>
                </ResourceDictionary>
                <ResourceDictionary x:Key='Light'>
                    <Color x:Key='TestColor'>White</Color>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </ThemeVariantScope.Resources>

    <Border Name='border' Background='{DynamicResource DemoBackground}'>
        <Border.Resources>
            <ResourceDictionary>
                <ResourceDictionary.ThemeDictionaries>
                    <ResourceDictionary x:Key='Dark'>
                        <StaticResource x:Key='DemoBackground' ResourceKey='TestColor' />
                    </ResourceDictionary>
                    <ResourceDictionary x:Key='Light'>
                        <StaticResource x:Key='DemoBackground' ResourceKey='TestColor' />
                    </ResourceDictionary>
                </ResourceDictionary.ThemeDictionaries>
            </ResourceDictionary>
        </Border.Resources>
    </Border>
</ThemeVariantScope>");
		var border = (Border) themeVariantScope.Child!;

		CornerstoneTest.AreEqual(Colors.White, ((ISolidColorBrush) border.Background!).Color);

		themeVariantScope.RequestedThemeVariant = ThemeVariant.Dark;

		CornerstoneTest.AreEqual(Colors.Black, ((ISolidColorBrush) border.Background!).Color);
	}

	[PresentationTestMethod]
	public void StaticResourceInsideOfThemeDictionariesShouldUseSameThemeKeyFromInnerFile()
	{
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Inner.xaml"), @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <StaticResource x:Key='InnerKey' ResourceKey='OuterKey' />
</ResourceDictionary>"),
			new RuntimeXamlLoaderDocument(@"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.ThemeDictionaries>
        <ResourceDictionary x:Key='Default'>
            <Color x:Key='OuterKey'>Green</Color>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source='csres://Tests/Inner.xaml'/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
        <ResourceDictionary x:Key='Dark'>
            <Color x:Key='OuterKey'>White</Color>
            <ResourceDictionary.MergedDictionaries>
                <ResourceInclude Source='csres://Tests/Inner.xaml'/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>")
		};

		var parsed = CornerstoneXamlIlRuntimeLoader.LoadGroup(documents);
		var dictionary = (ResourceDictionary) parsed[1]!;

		dictionary.TryGetResource("InnerKey", ThemeVariant.Dark, out var resource);
		var colorResource = CornerstoneTest.IsType<Color>(resource);
		CornerstoneTest.AreEqual(Colors.White, colorResource);

		dictionary.TryGetResource("InnerKey", ThemeVariant.Light, out resource);
		colorResource = CornerstoneTest.IsType<Color>(resource);
		CornerstoneTest.AreEqual(Colors.Green, colorResource);
	}

	[PresentationTestMethod]
	public void StaticResourceOutsideOfDictionariesShouldUseControlThemeVariant()
	{
		using (PresentationLocator.EnterScope())
		{
			var applicationThemeHost = new StubThemeVariantHost();
			applicationThemeHost.ActualThemeVariant = ThemeVariant.Dark;
			PresentationLocator.CurrentMutable.Bind<IThemeVariantHost>().ToConstant(applicationThemeHost);

			var themeVariantScope = (ThemeVariantScope) CornerstoneRuntimeXamlLoader.Load(@"
<ThemeVariantScope xmlns='https://github.com/BobbyCannon/Cornerstone'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              RequestedThemeVariant='Light'>
    <ThemeVariantScope.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Dark'>
                    <SolidColorBrush x:Key='DemoBackground'>Black</SolidColorBrush>
                </ResourceDictionary>
                <ResourceDictionary x:Key='Light'>
                    <SolidColorBrush x:Key='DemoBackground'>White</SolidColorBrush>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </ThemeVariantScope.Resources>

    <Border Name='border' Background='{StaticResource DemoBackground}'/>
</ThemeVariantScope>");
			var border = (Border) themeVariantScope.Child!;

			themeVariantScope.RequestedThemeVariant = ThemeVariant.Light;
			CornerstoneTest.AreEqual(Colors.White, ((ISolidColorBrush) border.Background!).Color);
		}
	}

	[Ignore("Asserts the upstream Simple theme brush colors (#dedede / Black); CornerstoneTheme palettes differ.")]
	[PresentationTestMethod]
	public void ThemeSwitchWorksInNestedScope()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        RequestedThemeVariant='Dark'>
    <ThemeVariantScope Name='Scope'>
        <TextBlock Name='Text' />
    </ThemeVariantScope>
</Window>");
			window.ApplyTemplate();

			var scope = window.FindControl<ThemeVariantScope>("Scope")!;
			var text = window.FindControl<TextBlock>("Text")!;

			CornerstoneTest.AreEqual(ThemeVariant.Dark, text.ActualThemeVariant);
			CornerstoneTest.AreEqual(Color.Parse("#dedede"), ((ISolidColorBrush) text.Foreground!).Color);

			scope.RequestedThemeVariant = ThemeVariant.Light;
			CornerstoneTest.AreEqual(ThemeVariant.Light, text.ActualThemeVariant);
			CornerstoneTest.AreEqual(Colors.Black, ((ISolidColorBrush) text.Foreground!).Color);
		}
	}

	[Ignore("Asserts the upstream Simple ThemeBackgroundBrush (#282828 / White); CornerstoneTheme palettes differ.")]
	[PresentationTestMethod]
	public void ThemeSwitchWorksInWithPopup()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			using (UnitTestApplication.Start(TestServices.StyledWindow))
			{
				var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ThemeVariantScope Name='Scope' RequestedThemeVariant='Dark'>
        <Popup Name='Popup'>
            <Border Width='100' Height='100'
                    Background='{DynamicResource ThemeBackgroundBrush}'>
            </Border>
        </Popup>
    </ThemeVariantScope>
</Window>");
				window.Show();

				var scope = window.FindControl<ThemeVariantScope>("Scope")!;
				var popup = window.FindControl<Popup>("Popup")!;

				popup.IsOpen = true;

				var border = (Border) popup.Child!;

				CornerstoneTest.AreEqual(ThemeVariant.Dark, popup.ActualThemeVariant);
				CornerstoneTest.AreEqual(ThemeVariant.Dark, border.ActualThemeVariant);
				CornerstoneTest.AreEqual(Color.Parse("#282828"), ((ISolidColorBrush) border.Background!).Color);

				scope.RequestedThemeVariant = ThemeVariant.Light;

				CornerstoneTest.AreEqual(ThemeVariant.Light, popup.ActualThemeVariant);
				CornerstoneTest.AreEqual(ThemeVariant.Light, border.ActualThemeVariant);
				CornerstoneTest.AreEqual(Colors.White, ((ISolidColorBrush) border.Background!).Color);
			}
		}
	}

	#endregion
}