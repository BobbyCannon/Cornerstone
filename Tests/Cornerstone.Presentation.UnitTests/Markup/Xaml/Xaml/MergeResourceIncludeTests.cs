#nullable enable

#region References

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class MergeResourceIncludeTests : XamlTestBase
{
	#region Constructors

	static MergeResourceIncludeTests()
	{
		RuntimeHelpers.RunClassConstructor(typeof(RelativeSource).TypeHandle);
	}

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void MergeResourceIncludeFailsWithThemeDictionariesDuplicateResources()
	{
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Resources1.xaml"), @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.ThemeDictionaries>
        <ResourceDictionary x:Key='Light'>
            <SolidColorBrush x:Key='brush1'>White</SolidColorBrush>
        </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>"),
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Resources2.xaml"), @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.ThemeDictionaries>
        <ResourceDictionary x:Key='Light'>
            <SolidColorBrush x:Key='brush1'>Black</SolidColorBrush>
        </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>"),
			new RuntimeXamlLoaderDocument(@"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.MergedDictionaries>
        <MergeResourceInclude Source='csres://Tests/Resources1.xaml'/>
        <MergeResourceInclude Source='csres://Tests/Resources2.xaml'/>
    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>")
		};

		CornerstoneTest.Throws<ArgumentException>(() => CornerstoneXamlIlRuntimeLoader.LoadGroup(documents));
	}

	[PresentationTestMethod]
	public void MergeResourceIncludeIsAllowedAfterResourceInclude()
	{
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Resources1.xaml"), @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='brush1'>Red</SolidColorBrush>
</ResourceDictionary>"),
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Resources2.xaml"), @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='brush2'>Blue</SolidColorBrush>
</ResourceDictionary>"),
			new RuntimeXamlLoaderDocument(@"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.MergedDictionaries>
        <ResourceInclude Source='csres://Tests/Resources2.xaml'/>
        <MergeResourceInclude Source='csres://Tests/Resources1.xaml'/>
    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>")
		};

		CornerstoneXamlIlRuntimeLoader.LoadGroup(documents);
	}

	[PresentationTestMethod]
	public void MergeResourceIncludeWorksWithMultipleResources()
	{
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Resources1.xaml"), @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='brush1'>Red</SolidColorBrush>
    <SolidColorBrush x:Key='brush2'>Blue</SolidColorBrush>
</ResourceDictionary>"),
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Resources2.xaml"), @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='brush4'>Yellow</SolidColorBrush>
    <ResourceDictionary.MergedDictionaries>
        <MergeResourceInclude Source='csres://Tests/Resources1_2.xaml'/>
    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>"),
			new RuntimeXamlLoaderDocument(@"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.MergedDictionaries>
        <MergeResourceInclude Source='csres://Tests/Resources1.xaml'/>
        <MergeResourceInclude Source='csres://Tests/Resources2.xaml'/>
    </ResourceDictionary.MergedDictionaries>
    <SolidColorBrush x:Key='brush5'>Black</SolidColorBrush>
    <SolidColorBrush x:Key='brush6'>White</SolidColorBrush>
</ResourceDictionary>"),
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Resources1_2.xaml"), @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='brush3'>Green</SolidColorBrush>
</ResourceDictionary>")
		};

		var objects = CornerstoneXamlIlRuntimeLoader.LoadGroup(documents);
		var resources = CornerstoneTest.IsType<ResourceDictionary>(objects[2]);
		CornerstoneTest.Empty(resources.MergedDictionaries);

		CornerstoneTest.AreEqual(Colors.Red, ((ISolidColorBrush) resources["brush1"]!).Color);
		CornerstoneTest.AreEqual(Colors.Blue, ((ISolidColorBrush) resources["brush2"]!).Color);
		CornerstoneTest.AreEqual(Colors.Green, ((ISolidColorBrush) resources["brush3"]!).Color);
		CornerstoneTest.AreEqual(Colors.Yellow, ((ISolidColorBrush) resources["brush4"]!).Color);
		CornerstoneTest.AreEqual(Colors.Black, ((ISolidColorBrush) resources["brush5"]!).Color);
		CornerstoneTest.AreEqual(Colors.White, ((ISolidColorBrush) resources["brush6"]!).Color);
	}

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void MergeResourceIncludeWorksWithSingleResource(bool createSourceInfo)
	{
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Resources.xaml"), @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='brush2'>Red</SolidColorBrush>
</ResourceDictionary>"),
			new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <ResourceDictionary>
            <SolidColorBrush x:Key='brush1'>Blue</SolidColorBrush>
            <ResourceDictionary.MergedDictionaries>
                <MergeResourceInclude Source='csres://Tests/Resources.xaml'/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>
</UserControl>")
		};

		var config = new RuntimeXamlLoaderConfiguration { CreateSourceInfo = createSourceInfo };
		var objects = CornerstoneXamlIlRuntimeLoader.LoadGroup(documents, config);
		var contentControl = CornerstoneTest.IsType<UserControl>(objects[1]);

		var resources = CornerstoneTest.IsType<ResourceDictionary>(contentControl.Resources);
		CornerstoneTest.Empty(resources.MergedDictionaries);

		var initialResource = (ISolidColorBrush) resources["brush1"]!;
		CornerstoneTest.AreEqual(Colors.Blue, initialResource.Color);

		var mergedResource = (ISolidColorBrush) resources["brush2"]!;
		CornerstoneTest.AreEqual(Colors.Red, mergedResource.Color);
	}

	[PresentationTestMethod]
	public void MergeResourceIncludeWorksWithThemeDictionaries()
	{
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Resources1.xaml"), @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.ThemeDictionaries>
        <ResourceDictionary x:Key='Light'>
            <SolidColorBrush x:Key='brush1'>White</SolidColorBrush>
            <SolidColorBrush x:Key='brush2'>Black</SolidColorBrush>
        </ResourceDictionary>
        <ResourceDictionary x:Key='Dark'>
            <SolidColorBrush x:Key='brush1'>Black</SolidColorBrush>
            <SolidColorBrush x:Key='brush2'>White</SolidColorBrush>
        </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>"),
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Resources2.xaml"), @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.ThemeDictionaries>
        <ResourceDictionary x:Key='Light'>
            <SolidColorBrush x:Key='brush3'>Red</SolidColorBrush>
            <SolidColorBrush x:Key='brush4'>Blue</SolidColorBrush>
        </ResourceDictionary>
        <ResourceDictionary x:Key='Dark'>
            <SolidColorBrush x:Key='brush3'>Blue</SolidColorBrush>
            <SolidColorBrush x:Key='brush4'>Red</SolidColorBrush>
        </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>"),
			new RuntimeXamlLoaderDocument(@"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.MergedDictionaries>
        <MergeResourceInclude Source='csres://Tests/Resources1.xaml'/>
        <MergeResourceInclude Source='csres://Tests/Resources2.xaml'/>
    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>")
		};

		var objects = CornerstoneXamlIlRuntimeLoader.LoadGroup(documents);
		var resources = CornerstoneTest.IsType<ResourceDictionary>(objects[2]);
		CornerstoneTest.Empty(resources.MergedDictionaries);

		CornerstoneTest.AreEqual(Colors.White, Get("brush1", ThemeVariant.Light).Color);
		CornerstoneTest.AreEqual(Colors.Black, Get("brush2", ThemeVariant.Light).Color);
		CornerstoneTest.AreEqual(Colors.Black, Get("brush1", ThemeVariant.Dark).Color);
		CornerstoneTest.AreEqual(Colors.White, Get("brush2", ThemeVariant.Dark).Color);

		CornerstoneTest.AreEqual(Colors.Red, Get("brush3", ThemeVariant.Light).Color);
		CornerstoneTest.AreEqual(Colors.Blue, Get("brush4", ThemeVariant.Light).Color);
		CornerstoneTest.AreEqual(Colors.Blue, Get("brush3", ThemeVariant.Dark).Color);
		CornerstoneTest.AreEqual(Colors.Red, Get("brush4", ThemeVariant.Dark).Color);

		ISolidColorBrush Get(string key, ThemeVariant themeVariant)
		{
			return resources.TryGetResource(key, themeVariant, out var res) ? (ISolidColorBrush) res! : throw new KeyNotFoundException();
		}
	}

	[PresentationTestMethod]
	public void MixingMergeResourceIncludeAndResourceIncludeIsNotAllowed()
	{
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Resources1.xaml"), @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='brush1'>Red</SolidColorBrush>
</ResourceDictionary>"),
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Resources2.xaml"), @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <SolidColorBrush x:Key='brush2'>Blue</SolidColorBrush>
</ResourceDictionary>"),
			new RuntimeXamlLoaderDocument(@"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ResourceDictionary.MergedDictionaries>
        <MergeResourceInclude Source='csres://Tests/Resources1.xaml'/>
        <ResourceInclude Source='csres://Tests/Resources2.xaml'/>
    </ResourceDictionary.MergedDictionaries>
</ResourceDictionary>")
		};

		CornerstoneTest.Throws<XmlException>(() => CornerstoneXamlIlRuntimeLoader.LoadGroup(documents));
	}

	#endregion
}