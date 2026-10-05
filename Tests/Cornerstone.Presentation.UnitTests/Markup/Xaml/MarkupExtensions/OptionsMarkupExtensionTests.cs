#nullable enable

#region References

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Markup.Xaml.MarkupExtensions;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;

[TestClass]
public class OptionsMarkupExtensionTests : XamlTestBase
{
	#region Fields

	public static int? ObjectsCreated;
	public static Func<object, bool>? RaisedOption;

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void AllowNesterMarkupExtensions()
	{
		using var _ = SetupTestGlobals("option 1");

		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
    </UserControl.Resources>
    <Border Background='{local:OptionsMarkupExtension OptionA={StaticResource brush}}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = (Border) userControl.Content!;

		CornerstoneTest.AreEqual(Color.Parse("#ff506070"), ((ISolidColorBrush) border.Background!).Color);
	}

	[PresentationTestMethod]
	public void AllowNesterOnPlatformMarkupExtensions()
	{
		using var _ = SetupTestGlobals("option 1");

		var xaml = @"
<Border xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        Margin='{local:OptionsMarkupExtension OptionA={local:OptionsMarkupExtensionMinimal OptionA=""10,10,10,10""}}' />";

		var border = (Border) CornerstoneRuntimeXamlLoader.Load(xaml);

		CornerstoneTest.AreEqual(new Thickness(10), border.Margin);
	}

	[PresentationTestMethod]
	[DataRow("option 1", "foo")]
	[DataRow("option 2", "bar")]
	public void BindingExtensionWorksInsideOfOptionsMarkupExtension(string option, string expected)
	{
		using var _ = SetupTestGlobals(option);

		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <x:String x:Key='text'>foo</x:String>
    </UserControl.Resources>

    <TextBlock Name='textBlock' Text='{local:OptionsMarkupExtension OptionA={CompiledBinding Source={StaticResource text}}, OptionB=bar}'/>
</UserControl>";

		var window = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var textBlock = window.GetControl<TextBlock>("textBlock");

		CornerstoneTest.AreEqual(expected, textBlock.Text);
	}

	[PresentationTestMethod]
	public void ConvertAvaloniaType()
	{
		using var _ = SetupTestGlobals("option 1");

		var xaml = @"
<Border xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        Padding='{local:OptionsMarkupExtension OptionA=""10, 8, 10, 8""}' />";

		var border = (Border) CornerstoneRuntimeXamlLoader.Load(xaml);

		CornerstoneTest.AreEqual(new Thickness(10, 8, 10, 8), border.Padding);
	}

	[PresentationTestMethod]
	public void ConvertBclType()
	{
		using var _ = SetupTestGlobals("option 1");

		var xaml = @"
<Border xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        Height='{local:OptionsMarkupExtension OptionA=50.1}' />";

		var border = (Border) CornerstoneRuntimeXamlLoader.Load(xaml);

		CornerstoneTest.AreEqual(50.1, border.Height);
	}

	[PresentationTestMethod]
	public void ResolveDefaultValue()
	{
		using var _ = SetupTestGlobals("default");

		var xaml = @"
<TextBlock xmlns='https://github.com/BobbyCannon/Cornerstone'
           xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
           xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
           Text='{local:OptionsMarkupExtension Default=""Hello World""}' />";

		var textBlock = (TextBlock) CornerstoneRuntimeXamlLoader.Load(xaml);

		CornerstoneTest.AreEqual("Hello World", textBlock.Text);
	}

	[PresentationTestMethod]
	public void ResolveDefaultValueFromCtor()
	{
		using var _ = SetupTestGlobals("default");

		var xaml = @"
<TextBlock xmlns='https://github.com/BobbyCannon/Cornerstone'
           xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
           xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
           Text='{local:OptionsMarkupExtension ""Hello World"", OptionB=""Im Android""}' />";

		var textBlock = (TextBlock) CornerstoneRuntimeXamlLoader.Load(xaml);

		CornerstoneTest.AreEqual("Hello World", textBlock.Text);
	}

	[PresentationTestMethod]
	public void ResolveExpectedValueExtensionWithProperty()
	{
		var xaml = @"
<TextBlock xmlns='https://github.com/BobbyCannon/Cornerstone'
           xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
           xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
           IsVisible='{local:OptionsMarkupExtensionWithProperty OptionA=True, Property=5}' />";

		var textBlock = (TextBlock) CornerstoneRuntimeXamlLoader.Load(xaml);

		CornerstoneTest.IsTrue(textBlock.IsSet(Visual.IsVisibleProperty));
		CornerstoneTest.IsTrue(textBlock.IsVisible);
	}

	[PresentationTestMethod]
	public void ResolveExpectedValueMinimalExtension()
	{
		var xaml = @"
<TextBlock xmlns='https://github.com/BobbyCannon/Cornerstone'
           xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
           xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
           IsVisible='{local:OptionsMarkupExtensionMinimal OptionA=True}' />";

		var textBlock = (TextBlock) CornerstoneRuntimeXamlLoader.Load(xaml);

		CornerstoneTest.IsTrue(textBlock.IsSet(Visual.IsVisibleProperty));
		CornerstoneTest.IsTrue(textBlock.IsVisible);
	}

	[PresentationTestMethod]
	[DataRow("option 1", "Im Option 1")]
	[DataRow("option 2", "Im Option 2")]
	[DataRow("3", "Im Option 3")]
	[DataRow("unknown", "Default value")]
	public void ResolveExpectedValuePerOption(object option, string expectedResult)
	{
		using var _ = SetupTestGlobals(option);

		var xaml = @"
<TextBlock xmlns='https://github.com/BobbyCannon/Cornerstone'
           xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
           xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
           Text='{local:OptionsMarkupExtension ""Default value"",
                OptionA=""Im Option 1"", OptionB=""Im Option 2"",
                OptionNumber=""Im Option 3""}' />";

		var textBlock = (TextBlock) CornerstoneRuntimeXamlLoader.Load(xaml);

		CornerstoneTest.AreEqual(expectedResult, textBlock.Text);
	}

	[PresentationTestMethod]
	[DataRow("option 1", "Im Option 1")]
	[DataRow("option 2", "Im Option 2")]
	[DataRow("3", "Im Option 3")]
	[DataRow("unknown", "Default value")]
	public void ResolveExpectedValuePerOptionCreateSingleObject(object option, string expectedResult)
	{
		using var _ = SetupTestGlobals(option);

		var xaml = @"
<ContentControl xmlns='https://github.com/BobbyCannon/Cornerstone'
           xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
           xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ContentControl.Content>
        <local:OptionsMarkupExtension>
            <local:OptionsMarkupExtension.Default>
                <local:ChildObject Name=""Default value"" />
            </local:OptionsMarkupExtension.Default>
            <local:OptionsMarkupExtension.OptionA>
                <local:ChildObject Name=""Im Option 1"" />
            </local:OptionsMarkupExtension.OptionA>
            <local:OptionsMarkupExtension.OptionB>
                <local:ChildObject Name=""Im Option 2"" />
            </local:OptionsMarkupExtension.OptionB>
            <local:OptionsMarkupExtension.OptionNumber>
                <local:ChildObject Name=""Im Option 3"" />
            </local:OptionsMarkupExtension.OptionNumber>
        </local:OptionsMarkupExtension>
    </ContentControl.Content>
</ContentControl>";

		var contentControl = (ContentControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var obj = CornerstoneTest.IsType<ChildObject>(contentControl.Content);

		CornerstoneTest.AreEqual(expectedResult, obj.Name);
		CornerstoneTest.AreEqual(1, ObjectsCreated);
	}

	[PresentationTestMethod]
	public void ResolveExpectedValueWithMethodWithoutServiceProvider()
	{
		using var _ = SetupTestGlobals(2);

		var xaml = @"
<TextBlock xmlns='https://github.com/BobbyCannon/Cornerstone'
           xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
           xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
           Text='{local:OptionsMarkupExtensionNoServiceProvider OptionB=""Im Option 2"", OptionA=""Im Option 1""}' />";

		var textBlock = (TextBlock) CornerstoneRuntimeXamlLoader.Load(xaml);

		CornerstoneTest.AreEqual("Im Option 2", textBlock.Text);
	}

	[PresentationTestMethod]
	public void ResolveImplicitDefaultValueAvaloniaValType()
	{
		using var _ = SetupTestGlobals("default");

		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             Margin='{local:OptionsMarkupExtension OptionA=10}' />";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);

		CornerstoneTest.AreEqual(new Thickness(0), userControl.Margin);
	}

	[PresentationTestMethod]
	public void ResolveImplicitDefaultValueRefType()
	{
		using var _ = SetupTestGlobals("default");

		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             Tag='{local:OptionsMarkupExtension OptionA=""Hello World"", x:DataType=x:String}' />";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);

		CornerstoneTest.AreEqual(null, userControl.Tag);
	}

	[PresentationTestMethod]
	public void ResolveImplicitDefaultValueValType()
	{
		using var _ = SetupTestGlobals("default");

		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             Height='{local:OptionsMarkupExtension OptionA=10}' />";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);

		CornerstoneTest.AreEqual(0d, userControl.Height);
	}

	[PlatformTestMethod(TestPlatforms.Windows | TestPlatforms.Linux, "TypeArguments test is failing on macOS from SRE emit")]
	public void RespectCustomTypeArgument()
	{
		using var _ = SetupTestGlobals("option 1");

		var xaml = @"
<TextBlock xmlns='https://github.com/BobbyCannon/Cornerstone'
           xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
           xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
           Tag='{local:OptionsMarkupExtensionWithGeneric Default=20, OptionA=""10, 10, 10, 10"", x:TypeArguments=Thickness}' />";

		var textBlock = (TextBlock) CornerstoneRuntimeXamlLoader.Load(xaml);

		CornerstoneTest.AreEqual(new Thickness(10, 10, 10, 10), textBlock.Tag);
	}

	[PresentationTestMethod]
	public void SupportComplexPropertySettersDictionary()
	{
		using var _ = SetupTestGlobals("option 1");

		var xaml = @"
<ResourceDictionary xmlns='https://github.com/BobbyCannon/Cornerstone'
                    xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Color x:Key='Color1'>Black</Color>
    <local:OptionsMarkupExtension x:Key='MyKey'>
        <local:OptionsMarkupExtension.OptionA>
            <Button Content='Hello World' />
        </local:OptionsMarkupExtension.OptionA>
    </local:OptionsMarkupExtension>
    <Color x:Key='Color2'>White</Color>
</ResourceDictionary>";

		var resourceDictionary = (ResourceDictionary) CornerstoneRuntimeXamlLoader.Load(xaml);
		var button = CornerstoneTest.IsType<Button>(resourceDictionary["MyKey"]);
		CornerstoneTest.AreEqual("Hello World", button.Content);
		CornerstoneTest.AreEqual(Colors.Black, resourceDictionary["Color1"]);
		CornerstoneTest.AreEqual(Colors.White, resourceDictionary["Color2"]);
	}

	[PresentationTestMethod]
	public void SupportComplexPropertySettersList()
	{
		using var _ = SetupTestGlobals("option 1");

		var xaml = @"
<Panel xmlns='https://github.com/BobbyCannon/Cornerstone'
       xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock />
    <local:OptionsMarkupExtension>
        <local:OptionsMarkupExtension.OptionA>
            <Button Content='Hello World' />
        </local:OptionsMarkupExtension.OptionA>
    </local:OptionsMarkupExtension>
    <TextBox />
</Panel>";

		var panel = (Panel) CornerstoneRuntimeXamlLoader.Load(xaml);
		CornerstoneTest.AreEqual(3, panel.Children.Count);
		CornerstoneTest.IsType<Button>(panel.Children[1]);
	}

	[PresentationTestMethod]
	public void SupportControlInsideXmlSyntax()
	{
		using var _ = SetupTestGlobals("option 1");

		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <local:OptionsMarkupExtension>
        <local:OptionsMarkupExtension.OptionA>
            <Button Content='Hello World' />
        </local:OptionsMarkupExtension.OptionA>
    </local:OptionsMarkupExtension>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var button = (Button) userControl.Content!;

		CornerstoneTest.AreEqual("Hello World", button.Content);
	}

	[PresentationTestMethod]
	public void SupportDefaultControlInsideXmlSyntax()
	{
		using var _ = SetupTestGlobals("unknown");

		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <local:OptionsMarkupExtension>
        <local:OptionsMarkupExtension.Default>
            <Button Content='Hello World' />
        </local:OptionsMarkupExtension.Default>
    </local:OptionsMarkupExtension>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var button = (Button) userControl.Content!;

		CornerstoneTest.AreEqual("Hello World", button.Content);
	}

	[PresentationTestMethod]
	[DataRow("option 1", "#ff506070")]
	[DataRow("3", "#000")]
	public void SupportSpecialOnSyntax(object option, string color)
	{
		using var _ = SetupTestGlobals(option);

		var xaml = @"
<Border xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border.Background>
        <local:OptionsMarkupExtension>
            <On Options='OptionA, OptionB'>
                <SolidColorBrush Color='#ff506070' />
            </On>
            <On Options=' OptionNumber '>
                <SolidColorBrush Color='#000' />
            </On>
        </local:OptionsMarkupExtension>
    </Border.Background>
</Border>";

		var border = (Border) CornerstoneRuntimeXamlLoader.Load(xaml);

		CornerstoneTest.AreEqual(Color.Parse(color), ((ISolidColorBrush) border.Background!).Color);
	}

	[PresentationTestMethod]
	public void SupportXmlSyntax()
	{
		using var _ = SetupTestGlobals("option 1");

		var xaml = @"
<Border xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border.Background>
        <local:OptionsMarkupExtension>
            <local:OptionsMarkupExtension.OptionA>
                <SolidColorBrush Color='#ff506070' />
            </local:OptionsMarkupExtension.OptionA>
        </local:OptionsMarkupExtension>
    </Border.Background>
</Border>";

		var border = (Border) CornerstoneRuntimeXamlLoader.Load(xaml);

		CornerstoneTest.AreEqual(Color.Parse("#ff506070"), ((ISolidColorBrush) border.Background!).Color);
	}

	[PlatformTestMethod(TestPlatforms.Windows | TestPlatforms.Linux, "TypeArguments test is failing on macOS from SRE emit")]
	public void SupportXmlSyntaxWithCustomTypeArguments()
	{
		using var _ = SetupTestGlobals("option 1");

		var xaml = @"
<Border xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border.Tag>
        <local:OptionsMarkupExtensionWithGeneric x:TypeArguments='Thickness' OptionA='10, 10, 10, 10' Default='20' />
    </Border.Tag>
</Border>";

		var border = (Border) CornerstoneRuntimeXamlLoader.Load(xaml);

		CornerstoneTest.AreEqual(new Thickness(10, 10, 10, 10), border.Tag);
	}

	[MemberNotNull(nameof(RaisedOption))]
	private static IDisposable SetupTestGlobals(object acceptedOption)
	{
		RaisedOption = o => o.Equals(acceptedOption);
		ObjectsCreated = 0;
		return Disposable.Create(() =>
		{
			RaisedOption = null;
			ObjectsCreated = null;
		});
	}

	#endregion
}

[TestClass]
public class OptionsMarkupExtension : OptionsMarkupExtensionBase<object, On>
{
	#region Constructors

	public OptionsMarkupExtension()
	{
	}

	public OptionsMarkupExtension(object defaultValue)
	{
		Default = defaultValue;
	}

	#endregion
}

public class OptionsMarkupExtension<TReturn> : OptionsMarkupExtensionBase<TReturn, On<TReturn>>
{
	#region Constructors

	public OptionsMarkupExtension()
	{
	}

	public OptionsMarkupExtension(TReturn defaultValue)
	{
		Default = defaultValue;
	}

	#endregion
}

public class OptionsMarkupExtensionBase<TReturn, TOn> : IAddChild<TOn>
	where TOn : On<TReturn>
{
	#region Properties

	[Content]
	[MarkupExtensionDefaultOption]
	public TReturn? Default { get; set; }

	[MarkupExtensionOption("option 1")]
	public TReturn? OptionA { get; set; }

	[MarkupExtensionOption("option 2")]
	public TReturn? OptionB { get; set; }

	[MarkupExtensionOption(3)]
	public TReturn? OptionNumber { get; set; }

	#endregion

	#region Methods

	public void AddChild(TOn child)
	{
	}

	public TReturn ProvideValue(IServiceProvider serviceProvider)
	{
		throw null!;
	}

	public bool ShouldProvideOption(IServiceProvider serviceProvider, string option)
	{
		return OptionsMarkupExtensionTests.RaisedOption!(option);
	}

	#endregion
}

[TestClass]
public class OptionsMarkupExtensionNoServiceProvider
{
	#region Properties

	[Content]
	[MarkupExtensionDefaultOption]
	public object? Default { get; set; }

	[MarkupExtensionOption(1)]
	public object? OptionA { get; set; }

	[MarkupExtensionOption(2)]
	public object? OptionB { get; set; }

	#endregion

	#region Methods

	public object ProvideValue(IServiceProvider serviceProvider)
	{
		throw null!;
	}

	public static bool ShouldProvideOption(int option)
	{
		return OptionsMarkupExtensionTests.RaisedOption!(option);
	}

	#endregion
}

[TestClass]
public class OptionsMarkupExtensionMinimal
{
	#region Properties

	[MarkupExtensionOption(11.0)]
	public bool OptionA { get; set; }

	#endregion

	#region Methods

	public object ProvideValue()
	{
		throw null!;
	}

	public static bool ShouldProvideOption(double option)
	{
		return option > 0;
	}

	#endregion
}

[TestClass]
public class OptionsMarkupExtensionWithProperty
{
	#region Properties

	[MarkupExtensionOption(5)]
	public bool OptionA { get; set; }

	public int Property { get; set; }

	#endregion

	#region Methods

	public object ProvideValue()
	{
		throw null!;
	}

	public bool ShouldProvideOption(int option)
	{
		return option == Property;
	}

	#endregion
}

public class OptionsMarkupExtensionWithGeneric<TResult>
{
	#region Properties

	[Content]
	[MarkupExtensionDefaultOption]
	public TResult? Default { get; set; }

	[MarkupExtensionOption("option 1")]
	public TResult? OptionA { get; set; }

	#endregion

	#region Methods

	public TResult ProvideValue()
	{
		throw null!;
	}

	public bool ShouldProvideOption(string option)
	{
		return OptionsMarkupExtensionTests.RaisedOption!(option);
	}

	#endregion
}

[TestClass]
public class ChildObject
{
	#region Constructors

	public ChildObject()
	{
		OptionsMarkupExtensionTests.ObjectsCreated++;
	}

	#endregion

	#region Properties

	public string? Name { get; set; }

	#endregion
}