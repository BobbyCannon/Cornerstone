#nullable enable

#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Markup.Xaml.Styling;
using Cornerstone.Presentation.Markup.Xaml.XamlIl.Runtime;
using Cornerstone.Presentation.Platform;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.Theme;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class StyleIncludeTests : XamlTestBase
{
	#region Constructors

	static StyleIncludeTests()
	{
		RuntimeHelpers.RunClassConstructor(typeof(RelativeSource).TypeHandle);
		AssetLoader.RegisterResUriParsers();
	}

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void MissingResourceKeyInStyleIncludeDoesNotCauseStackOverflow()
	{
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Style.xaml"), @"
<Style xmlns='https://github.com/BobbyCannon/Cornerstone'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style.Resources>
        <StaticResource x:Key='brush' ResourceKey='missing' />
    </Style.Resources>
</Style>"),
			new RuntimeXamlLoaderDocument(@"
<ContentControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ContentControl.Styles>
        <StyleInclude Source='csres://Tests/Style.xaml'/>
    </ContentControl.Styles>
</ContentControl>")
		};

		try
		{
			_ = CornerstoneXamlIlRuntimeLoader.LoadGroup(documents);
		}
		catch (KeyNotFoundException)
		{
		}
	}

	[PresentationTestMethod]
	public void NonLatinStyleIncludeIsResolvedWithTwoFiles()
	{
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://アセンブリ/スタイル.xaml"), @"
<Style xmlns='https://github.com/BobbyCannon/Cornerstone'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style.Resources>
        <Color x:Key='Red'>Red</Color>
    </Style.Resources>
</Style>"),
			new RuntimeXamlLoaderDocument(@"
<ContentControl xmlns='https://github.com/BobbyCannon/Cornerstone'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ContentControl.Resources>
        <StyleInclude x:Key='Include' Source='csres://アセンブリ/スタイル.xaml'/>
    </ContentControl.Resources>
</ContentControl>")
		};

		var objects = CornerstoneXamlIlRuntimeLoader.LoadGroup(documents);
		var style = CornerstoneTest.IsType<Style>(objects[0]);
		var contentControl = CornerstoneTest.IsType<ContentControl>(objects[1]);

		CornerstoneTest.IsType<Style>(contentControl.Resources["Include"]);
	}

	[PresentationTestMethod]
	public void RelativeBackStyleIncludeIsResolvedWithTwoFiles()
	{
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Subfolder/Style.xaml"), @"
<Style xmlns='https://github.com/BobbyCannon/Cornerstone'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style.Resources>
        <Color x:Key='Red'>Red</Color>
    </Style.Resources>
</Style>"),
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Subfolder/Folder/Root.xaml"), @"
<ContentControl xmlns='https://github.com/BobbyCannon/Cornerstone'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ContentControl.Resources>
        <StyleInclude x:Key='Include' Source='../Style.xaml'/>
    </ContentControl.Resources>
</ContentControl>")
		};

		var objects = CornerstoneXamlIlRuntimeLoader.LoadGroup(documents);
		var style = CornerstoneTest.IsType<Style>(objects[0]);
		var contentControl = CornerstoneTest.IsType<ContentControl>(objects[1]);

		CornerstoneTest.IsType<Style>(contentControl.Resources["Include"]);
	}

	[PresentationTestMethod]
	public void RelativeDotSyntaxStyleIncludeIsResolvedWithTwoFiles()
	{
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Folder/Style.xaml"), @"
<Style xmlns='https://github.com/BobbyCannon/Cornerstone'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style.Resources>
        <Color x:Key='Red'>Red</Color>
    </Style.Resources>
</Style>"),
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Folder/Root.xaml"), @"
<ContentControl xmlns='https://github.com/BobbyCannon/Cornerstone'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ContentControl.Resources>
        <StyleInclude x:Key='Include' Source='./Style.xaml'/>
    </ContentControl.Resources>
</ContentControl>")
		};

		var objects = CornerstoneXamlIlRuntimeLoader.LoadGroup(documents);
		var style = CornerstoneTest.IsType<Style>(objects[0]);
		var contentControl = CornerstoneTest.IsType<ContentControl>(objects[1]);

		CornerstoneTest.IsType<Style>(contentControl.Resources["Include"]);
	}

	[PresentationTestMethod]
	public void RelativeRootStyleIncludeIsResolvedWithTwoFiles()
	{
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Style.xaml"), @"
<Style xmlns='https://github.com/BobbyCannon/Cornerstone'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style.Resources>
        <Color x:Key='Red'>Red</Color>
    </Style.Resources>
</Style>"),
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Folder/Root.xaml"), @"
<ContentControl xmlns='https://github.com/BobbyCannon/Cornerstone'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ContentControl.Resources>
        <StyleInclude x:Key='Include' Source='/Style.xaml'/>
    </ContentControl.Resources>
</ContentControl>")
		};

		var objects = CornerstoneXamlIlRuntimeLoader.LoadGroup(documents);
		var style = CornerstoneTest.IsType<Style>(objects[0]);
		var contentControl = CornerstoneTest.IsType<ContentControl>(objects[1]);

		CornerstoneTest.IsType<Style>(contentControl.Resources["Include"]);
	}

	[PresentationTestMethod]
	public void RelativeStyleIncludeIsResolvedWithTwoFiles()
	{
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Folder/Style.xaml"), @"
<Style xmlns='https://github.com/BobbyCannon/Cornerstone'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style.Resources>
        <Color x:Key='Red'>Red</Color>
    </Style.Resources>
</Style>"),
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Folder/Root.xaml"), @"
<ContentControl xmlns='https://github.com/BobbyCannon/Cornerstone'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ContentControl.Resources>
        <StyleInclude x:Key='Include' Source='Style.xaml'/>
    </ContentControl.Resources>
</ContentControl>")
		};

		var objects = CornerstoneXamlIlRuntimeLoader.LoadGroup(documents);
		var style = CornerstoneTest.IsType<Style>(objects[0]);
		var contentControl = CornerstoneTest.IsType<ContentControl>(objects[1]);

		CornerstoneTest.IsType<Style>(contentControl.Resources["Include"]);
	}

	[Ignore("Needs compiled StyleWithServiceProvider.xaml; XAML compile is disabled on this test project.")]
	[PresentationTestMethod]
	public void StyleIncludeFromCodeBehindResolvesCompiled()
	{
		using var locatorScope = PresentationLocator.EnterScope();
		PresentationLocator.CurrentMutable.BindToSelf<IAssetLoader>(new StandardAssetLoader(GetType().Assembly));

		var sp = new TestServiceProvider();
		var styleInclude = new StyleInclude(sp)
		{
			Source = new Uri("csres://Cornerstone.Presentation.UnitTests/Xaml/StyleWithServiceProvider.xaml")
		};

		var loaded = CornerstoneTest.IsType<StyleWithServiceProvider>(styleInclude.Loaded);
		CornerstoneTest.IsNotNull(loaded.ServiceProvider);

		CornerstoneTest.AreEqual(sp.GetRequiredService<ICornerstoneXamlIlParentStackProvider>().Parents, loaded.ServiceProvider.GetRequiredService<ICornerstoneXamlIlParentStackProvider>().Parents);
	}

	[Ignore("Needs packed CornerstoneResource Style1.xaml; XAML compile is disabled on this test project.")]
	[PresentationTestMethod]
	public void StyleIncludeIsBuilt()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow
					.With(theme: () => new Styles())))
		{
			var xaml = @"
<ContentControl xmlns='https://github.com/BobbyCannon/Cornerstone'>
    <ContentControl.Styles>
        <StyleInclude Source='csres://Cornerstone.Presentation.UnitTests/Xaml/Style1.xaml'/>
    </ContentControl.Styles>
</ContentControl>";

			var window = CornerstoneRuntimeXamlLoader.Parse<ContentControl>(xaml);

			CornerstoneTest.IsType<Style>(window.Styles[0]);
		}
	}

	[Ignore("Needs packed CornerstoneResource Style1.xaml; XAML compile is disabled on this test project.")]
	[PresentationTestMethod]
	public void StyleIncludeIsBuiltResources()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow
					.With(theme: () => new Styles())))
		{
			var xaml = @"
<ContentControl xmlns='https://github.com/BobbyCannon/Cornerstone'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ContentControl.Resources>
        <StyleInclude x:Key='Include' Source='csres://Cornerstone.Presentation.UnitTests/Xaml/Style1.xaml'/>
    </ContentControl.Resources>
</ContentControl>";

			var contentControl = CornerstoneRuntimeXamlLoader.Parse<ContentControl>(xaml);

			CornerstoneTest.IsType<Style>(contentControl.Resources["Include"]);
		}
	}

	[PresentationTestMethod]
	public void StyleIncludeIsResolvedWithTwoFiles()
	{
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Style.xaml"), @"
<Style xmlns='https://github.com/BobbyCannon/Cornerstone'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style.Resources>
        <Color x:Key='Red'>Red</Color>
    </Style.Resources>
</Style>"),
			new RuntimeXamlLoaderDocument(@"
<ContentControl xmlns='https://github.com/BobbyCannon/Cornerstone'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ContentControl.Resources>
        <StyleInclude x:Key='Include' Source='csres://Tests/Style.xaml'/>
    </ContentControl.Resources>
</ContentControl>")
		};

		var objects = CornerstoneXamlIlRuntimeLoader.LoadGroup(documents);
		var style = CornerstoneTest.IsType<Style>(objects[0]);
		var contentControl = CornerstoneTest.IsType<ContentControl>(objects[1]);

		CornerstoneTest.IsType<Style>(contentControl.Resources["Include"]);
	}

	[PresentationTestMethod]
	public void StyleIncludeShouldBeReplacedWithDirectCall()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var control = (ContentControl) CornerstoneRuntimeXamlLoader.Load(@"
<ContentControl xmlns='https://github.com/BobbyCannon/Cornerstone'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                xmlns:themes='clr-namespace:Cornerstone.Presentation.Theme;assembly=Cornerstone.Presentation'>
    <ContentControl.Styles>
        <themes:CornerstoneTheme />
        <StyleInclude Source='csres://Cornerstone.Presentation/Theme/CornerstoneTheme.cxaml'/>
    </ContentControl.Styles>
</ContentControl>");
		CornerstoneTest.IsType<CornerstoneTheme>(control.Styles[0]);
		CornerstoneTest.IsType<CornerstoneTheme>(control.Styles[1]);
	}

	[PresentationTestMethod]
	public void StyleInsideResourcesShouldProduceWarning()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var diagnostics = new List<RuntimeXamlDiagnostic>();
		var control = (ContentControl) CornerstoneRuntimeXamlLoader.Load(new RuntimeXamlLoaderDocument(@"
<ContentControl xmlns='https://github.com/BobbyCannon/Cornerstone'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                xmlns:themes='clr-namespace:Cornerstone.Presentation.Theme;assembly=Cornerstone.Presentation'>
    <ContentControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <themes:CornerstoneTheme />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </ContentControl.Resources>
</ContentControl>"), new RuntimeXamlLoaderConfiguration
		{
			DiagnosticHandler = diagnostic =>
			{
				diagnostics.Add(diagnostic);
				return diagnostic.Severity;
			}
		});
		CornerstoneTest.IsAssignableFrom<IStyle>(((ResourceDictionary) control.Resources).MergedDictionaries[0]);
		var warning = CornerstoneTest.Single(diagnostics);
		CornerstoneTest.AreEqual(RuntimeXamlDiagnosticSeverity.Warning, warning.Severity);
	}

	#endregion
}

[TestClass]
public class TestServiceProvider :
	IServiceProvider,
	IUriContext,
	ICornerstoneXamlIlEagerParentStackProvider
{
	#region Fields

	private readonly IServiceProvider _root = XamlIlRuntimeHelpers.CreateRootServiceProviderV2();

	#endregion

	#region Properties

	public Uri BaseUri { get; set; } = null!;
	public List<object> ParentsStack { get; set; } = [new ContentControl()];
	IReadOnlyList<object> ICornerstoneXamlIlEagerParentStackProvider.DirectParentsStack => ParentsStack;
	ICornerstoneXamlIlEagerParentStackProvider? ICornerstoneXamlIlEagerParentStackProvider.ParentProvider => null;
	IEnumerable<object> ICornerstoneXamlIlParentStackProvider.Parents => ParentsStack.AsEnumerable().Reverse();

	#endregion

	#region Methods

	public object? GetService(Type serviceType)
	{
		if (serviceType == typeof(IUriContext))
		{
			return this;
		}
		if (serviceType == typeof(ICornerstoneXamlIlParentStackProvider))
		{
			return this;
		}
		return _root.GetService(serviceType);
	}

	#endregion
}