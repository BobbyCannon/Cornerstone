#nullable enable

#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;

[TestClass]
public class ResourceIncludeTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void MissingResourceKeyInResourceIncludeDoesNotCauseStackOverflow()
	{
		var app = Application.Current;
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Resource.xaml"), @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
<StaticResource x:Key='brush' ResourceKey='missing' />
</ResourceDictionary>"),
			new RuntimeXamlLoaderDocument(app, @"
<Application xmlns='https://github.com/BobbyCannon/Cornerstone'
         xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceInclude Source='csres://Tests/Resource.xaml'/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
</Application>")
		};

		using (StartWithResources())
		{
			try
			{
				CornerstoneXamlIlRuntimeLoader.LoadGroup(documents);
			}
			catch (KeyNotFoundException)
			{
			}
		}
	}

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void ResourceIncludeLoadsResourceDictionary(bool createSourceInfo)
	{
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Resource.xaml"), @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
<SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
</ResourceDictionary>"),
			new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
         xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
<UserControl.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceInclude Source='csres://Tests/Resource.xaml'/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</UserControl.Resources>

<Border Name='border' Background='{StaticResource brush}'/>
</UserControl>")
		};

		var config = new RuntimeXamlLoaderConfiguration { CreateSourceInfo = createSourceInfo };

		using (StartWithResources())
		{
			var compiled = CornerstoneXamlIlRuntimeLoader.LoadGroup(documents, config);
			var userControl = CornerstoneTest.IsType<UserControl>(compiled[1]);
			var border = userControl.GetControl<Border>("border");

			var brush = (ISolidColorBrush) border.Background!;
			CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
		}
	}

	[PresentationTestMethod]
	public void ResourceIncludeShouldBeAllowedToHaveKeyInCustomContainer()
	{
		var app = Application.Current;
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://Demo/en-us.axaml"), @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <x:String x:Key='OkButton'>OK</x:String>
</ResourceDictionary>"),
			new RuntimeXamlLoaderDocument(app, @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                    xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'>
    <ResourceDictionary.MergedDictionaries>
        <local:LocaleCollection>
            <ResourceInclude Source='csres://Demo/en-us.axaml' x:Key='English' />
        </local:LocaleCollection>
    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>")
		};

		using (StartWithResources())
		{
			var groups = CornerstoneXamlIlRuntimeLoader.LoadGroup(documents);
			var res = CornerstoneTest.IsType<ResourceDictionary>(groups[1]);

			CornerstoneTest.IsTrue(res.TryGetResource("OkButton", null, out var val));
			CornerstoneTest.AreEqual("OK", val);
		}
	}

	private IDisposable StartWithResources(params (string, string)[] assets)
	{
		var assetLoader = new MockAssetLoader(assets);
		var services = new TestServices(assetLoader);
		return UnitTestApplication.Start(services);
	}

	#endregion
}

// See https://github.com/AvaloniaUI/Avalonia/issues/11172
public class LocaleCollection : ResourceProvider
{
	#region Fields

	private readonly Dictionary<object, IResourceProvider> _langs = new();

	#endregion

	#region Properties

	public override bool HasResources => true;

	#endregion

	#region Methods

	// Allow the runtime to use this class as a collection, requires x:Key on the IResourceProvider 
	public void Add(object k, IResourceProvider v)
	{
		_langs.Add(k, v);
	}

	public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
	{
		if (_langs.TryGetValue("English", out var res))
		{
			return res.TryGetResource(key, theme, out value);
		}
		value = null;
		return false;
	}

	#endregion
}