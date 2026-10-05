#nullable enable

#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Naming;
using Cornerstone.Presentation.Controls.StyleClasses;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Markup.Xaml.Templates;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[InvariantCulture]
[TestClass]
public class StyleTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void CanBindingClassesInSetter()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = """
						<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
						             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
						             xmlns:u='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml'
						             xmlns:vm='using:Cornerstone.Presentation.UnitTests.Markup.Xaml'
						             >
						    <Window.Styles>
						        <Style Selector="Border" x:DataType='vm:TestViewModel'>
						            <Setter Property="(Classes.Banned)" Value='{Binding Boolean}'/>

						            <Style Selector="^.Banned">
						               <Setter Property='Background' Value='Red'/>
						            </Style>
						        </Style>
						    </Window.Styles>
						    <Window.DataContext>
						       <vm:TestViewModel/>
						    </Window.DataContext>
						    <Border/>
						</Window>
						""";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			window.ApplyTemplate();
			var vm = window.DataContext as TestViewModel;
			CornerstoneTest.IsNotNull(vm);

			var border = window.Content as Border;
			CornerstoneTest.IsNotNull(border);
			CornerstoneTest.IsNull(border.Background);
			vm.Boolean = true;
			CornerstoneTest.AreEqual(Brushes.Red, border.Background);
		}
	}

	[PresentationTestMethod]
	public void CanUseClassesInSetter()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = """
						<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
						             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
						             xmlns:u='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml'>
						    <Window.Styles>
						        <Style Selector="Border">
						            <Setter Property="(Classes.Banned)" Value='true'/>

						            <Style Selector="^.Banned">
						               <Setter Property='Background' Value='Red'/>
						            </Style>
						        </Style>
						    </Window.Styles>
						    <Border/>
						</Window>
						""";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var border = window.Content as Border;
			CornerstoneTest.IsNotNull(border);
			CornerstoneTest.AreEqual(Brushes.Red, border.Background);
		}
	}

	[PresentationTestMethod]
	public void CanUseNestedStyles()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Border'>
            <Style Selector='^.foo'>
                <Setter Property='Background' Value='Red'/>
            </Style>
        </Style>
    </Window.Styles>
    <StackPanel>
        <Border Name='foo'/>
    </StackPanel>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var foo = window.GetControl<Border>("foo");

			CornerstoneTest.IsNull(foo.Background);

			foo.Classes.Add("foo");

			CornerstoneTest.AreEqual(Colors.Red, ((ISolidColorBrush) foo.Background!).Color);
		}
	}

	[PresentationTestMethod]
	public void ColorCanBeAddedToStyleResources()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
		{
			var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Styles>
        <Style>
            <Style.Resources>
                <Color x:Key='color'>#ff506070</Color>
            </Style.Resources>
        </Style>
    </UserControl.Styles>
</UserControl>";
			var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
			var color = (Color) ((Style) userControl.Styles[0]).Resources["color"]!;

			CornerstoneTest.AreEqual(0xff506070, color.ToUInt32());
		}
	}

	[PresentationTestMethod]
	public void ControlTemplateCanBeAddedToStyleResources()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
		{
			var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Styles>
        <Style>
            <Style.Resources>
                 <ControlTemplate x:Key='controlTemplate' TargetType='{x:Type Button}'>
                    <ContentPresenter Content='{TemplateBinding Content}'/>
                 </ControlTemplate>
            </Style.Resources>
        </Style>
    </UserControl.Styles>
</UserControl>";
			var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
			var controlTemplate = (ControlTemplate?) ((Style) userControl.Styles[0]).Resources["controlTemplate"];

			CornerstoneTest.IsNotNull(controlTemplate);
			CornerstoneTest.AreEqual(typeof(Button), controlTemplate.TargetType);
		}
	}

	[PresentationTestMethod]
	public void CorrectlyResolveTemplateBindingInStyleWithTemplateSelector()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Style xmlns='https://github.com/BobbyCannon/Cornerstone'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
       xmlns:u='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml'
       Selector='u|TestTemplatedControl /template/ Border'>
    <Setter Property='Tag' Value='{TemplateBinding TestData}'/>
</Style>";

			var style = (Style) CornerstoneRuntimeXamlLoader.Load(xaml);
			var setter = CornerstoneTest.IsType<Setter>(CornerstoneTest.Single(style.Setters));

			CornerstoneTest.AreEqual(TestTemplatedControl.TestDataProperty, (setter.Value as TemplateBinding)?.Property);
		}
	}

	[PresentationTestMethod]
	public void DataTemplateCanBeAddedToStyleResources()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
		{
			var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Styles>
        <Style>
            <Style.Resources>
                <DataTemplate x:Key='dataTemplate'><TextBlock/></DataTemplate>
            </Style.Resources>
        </Style>
    </UserControl.Styles>
</UserControl>";
			var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
			var dataTemplate = (DataTemplate?) ((Style) userControl.Styles[0]).Resources["dataTemplate"];

			CornerstoneTest.IsNotNull(dataTemplate);
		}
	}

	[Ignore("The animation system currently needs to be able to set any property on any object")]
	[PresentationTestMethod]
	public void DisallowsSettingNonRegisteredProperty()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Window.Styles>
        <Style Selector='TextBlock'>
            <Setter Property='Button.IsDefault' Value='True'/>
        </Style>
    </Window.Styles>
    <TextBlock/>
</Window>";
			var ex = Assert.Throws<XmlException>(() => CornerstoneRuntimeXamlLoader.Load(xaml));

			CornerstoneTest.AreEqual("Property 'Button.IsDefault' is not registered on 'Cornerstone.Presentation.Controls.TextBlock'.", ex.InnerException?.Message);
		}
	}

	[PresentationTestMethod]
	public void FailsToResolveTemplateBindingInStyleWithoutTemplateMetadata()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Style xmlns='https://github.com/BobbyCannon/Cornerstone'
       Selector='Border'>
    <Setter Property='Tag' Value='{TemplateBinding TestData}'/>
</Style>";

			var exception = CornerstoneTest.Throws<XmlException>(() => CornerstoneRuntimeXamlLoader.Load(xaml));
			CornerstoneTest.Contains(exception.Message, "ControlTemplate");
		}
	}

	[PresentationTestMethod]
	public void FailsUseClassesInSetterWhenSelectorIsComplex()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = """
						<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
						             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
						             xmlns:u='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml'>
						    <Window.Styles>
						        <Style Selector="Border:pointover">
						            <Setter Property="(Classes.Banned)" Value='true'/>

						            <Style Selector="^.Banned">
						               <Setter Property='Background' Value='Red'/>
						            </Style>
						        </Style>
						    </Window.Styles>
						    <Border/>
						</Window>
						""";

			var exception = CornerstoneTest.Throws<XmlException>(() => CornerstoneRuntimeXamlLoader.Load(xaml));
			CornerstoneTest.AreEqual("Cannot set Classes Binding property '(Classes.Banned)' because the style has an activator. Line 6, position 14.", exception.Message);
		}
	}

	[PresentationTestMethod]
	public void MultipleErrorsAreReported()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='5' />
        <Style Selector='NonExistentType' />
        <Style Selector='Border:normal' />
        <Style Selector='Border+invalid' />
    </Window.Styles>
</Window>";
			var ex = Assert.Throws<AggregateException>(() => (Window) CornerstoneRuntimeXamlLoader.Load(xaml));
			CornerstoneTest.Collection(ex.InnerExceptions, inner => CornerstoneTest.IsAssignableFrom<XmlException>(inner), inner => CornerstoneTest.IsAssignableFrom<XmlException>(inner), inner => CornerstoneTest.IsAssignableFrom<XmlException>(inner));
		}
	}

	[PresentationTestMethod]
	[DataRow("<Style>", "</Style>")]
	[DataRow("<Style Selector=''>", "</Style>")]
	public void NoSelectorShouldFailInControlTheme(string styleStart, string styleEnd)
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var exception = CornerstoneTest.Throws<XmlException>(() => (Window) CornerstoneRuntimeXamlLoader.Load(
			$$"""
			<Window xmlns="https://github.com/BobbyCannon/Cornerstone"
			        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
			   <Window.Resources>
			        <ControlTheme x:Key="{x:Type Window}" TargetType="Window">
			            {{styleStart}}
			                <Setter Property="Title" Value="title set via style!" />
			            {{styleEnd}}
			        </ControlTheme>
			    </Window.Resources>
			</Window>
			"""));

		CornerstoneTest.AreEqual("Cannot add a Style without selector to a ControlTheme. Line 5, position 14.", exception.Message);
	}

	[PresentationTestMethod]
	[DataRow("<Style>", "</Style>")]
	[DataRow("<Style Selector=''>", "</Style>")]
	[DataRow("<Styles><Style>", "</Style></Styles>")]
	[DataRow("<Styles><Style Selector=''>", "</Style></Styles>")]
	public void NoSelectorShouldTargetParentType(string styleStart, string styleEnd)
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var window = (Window) CornerstoneRuntimeXamlLoader.Load(
			$"""
			<Window xmlns="https://github.com/BobbyCannon/Cornerstone">
			    <Window.Styles>
			        {styleStart}
			            <Setter Property="Title" Value="title set via style!" />
			        {styleEnd}
			    </Window.Styles>
			</Window>
			""");

		CornerstoneTest.AreEqual("title set via style!", window.Title);
	}

	[PresentationTestMethod]
	public void SelectorShouldNotResolveToMarkupExtensionType()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var style = (Style) CornerstoneRuntimeXamlLoader.Load(
			"""
			<Style xmlns='https://github.com/BobbyCannon/Cornerstone'
			        xmlns:u='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml'
			        Selector='u|TestSelectorControl'>
			</Style>
			""");

		CornerstoneTest.IsNotNull(style.Selector);

		var targetType = style.Selector.TargetType;

		CornerstoneTest.AreNotEqual(typeof(TestSelectorControlExtension), targetType);
	}

	[PresentationTestMethod]
	public void SetterCanContainTemplate()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='ContentControl'>
            <Setter Property='Content'>
                <Template>
                    <TextBlock>Hello World!</TextBlock>
                </Template>
            </Setter>
        </Style>
    </Window.Styles>

    <ContentControl Name='target'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var target = window.Get<ContentControl>("target");

			CornerstoneTest.IsType<TextBlock>(target.Content);
			CornerstoneTest.AreEqual("Hello World!", ((TextBlock) target.Content).Text);
		}
	}

	[PresentationTestMethod]
	public void SetterCanSetAttachedProperty()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Window.Styles>
        <Style Selector='TextBlock'>
            <Setter Property='DockPanel.Dock' Value='Right'/>
        </Style>
    </Window.Styles>
    <TextBlock/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = (TextBlock) window.Content!;

			window.ApplyTemplate();

			CornerstoneTest.AreEqual(Dock.Right, DockPanel.GetDock(textBlock));
		}
	}

	[PresentationTestMethod]
	public void SetterValueIsBoundDirectlyIfTheTargetTypeDerivesFromITemplate()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector=':is(Control)'>
		  <Setter Property='FocusAdorner'>
			<FocusAdornerTemplate>
			  <Rectangle Stroke='Black'
						 StrokeThickness='1'
						 StrokeDashArray='1,2'/>
			</FocusAdornerTemplate>
		  </Setter>
		</Style>
	</Window.Styles>

    <TextBlock Name='target'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var target = window.Get<TextBlock>("target");

			CornerstoneTest.IsNotNull(target.FocusAdorner);
		}
	}

	[PresentationTestMethod]
	public void SolidColorBrushCanBeAddedToStyleResources()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
		{
			var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Styles>
        <Style>
            <Style.Resources>
                <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
            </Style.Resources>
        </Style>
    </UserControl.Styles>
</UserControl>";
			var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
			var brush = (ISolidColorBrush) ((Style) userControl.Styles[0]).Resources["brush"]!;

			CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
		}
	}

	[PresentationTestMethod]
	public void StyleCanUseClassSelectorWithDash()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Border.foo-bar'>
            <Setter Property='Background' Value='Red'/>
        </Style>
    </Window.Styles>
    <StackPanel>
        <Border Name='foo' Classes='foo-bar'/>
    </StackPanel>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var foo = window.GetControl<Border>("foo");

			CornerstoneTest.AreEqual(Colors.Red, ((ISolidColorBrush) foo.Background!).Color);
		}
	}

	[PresentationTestMethod]
	public void StyleCanUseNotSelector()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Border:not(.foo)'>
            <Setter Property='Background' Value='Red'/>
        </Style>
    </Window.Styles>
    <StackPanel>
        <Border Name='foo' Classes='foo bar'/>
        <Border Name='notFoo' Classes='bar'/>
    </StackPanel>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var foo = window.GetControl<Border>("foo");
			var notFoo = window.GetControl<Border>("notFoo");

			CornerstoneTest.IsNull(foo.Background);
			CornerstoneTest.AreEqual(Colors.Red, ((ISolidColorBrush) notFoo.Background!).Color);
		}
	}

	[PresentationTestMethod]
	public void StyleCanUseNthChildSelector()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Border.foo:nth-child(2n+1)'>
            <Setter Property='Background' Value='Red'/>
        </Style>
    </Window.Styles>
    <StackPanel>
        <Border x:Name='b1' Classes='foo'/>
        <Border x:Name='b2' />
    </StackPanel>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var b1 = window.GetControl<Border>("b1");
			var b2 = window.GetControl<Border>("b2");

			CornerstoneTest.AreEqual(Brushes.Red, b1.Background);
			CornerstoneTest.IsNull(b2.Background);
		}
	}

	[PresentationTestMethod]
	public void StyleCanUseNthChildSelectorAfterReorder()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Border:nth-child(2n)'>
            <Setter Property='Background' Value='Red'/>
        </Style>
    </Window.Styles>
    <StackPanel x:Name='parent'>
        <Border x:Name='b1' />
        <Border x:Name='b2' />
    </StackPanel>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);

			var parent = window.GetControl<StackPanel>("parent");
			var b1 = window.GetControl<Border>("b1");
			var b2 = window.GetControl<Border>("b2");

			CornerstoneTest.IsNull(b1.Background);
			CornerstoneTest.AreEqual(Brushes.Red, b2.Background);

			parent.Children.Remove(b1);

			CornerstoneTest.IsNull(b1.Background);
			CornerstoneTest.IsNull(b2.Background);

			parent.Children.Add(b1);

			CornerstoneTest.AreEqual(Brushes.Red, b1.Background);
			CornerstoneTest.IsNull(b2.Background);
		}
	}

	[PresentationTestMethod]
	public void StyleCanUseNthChildSelectorWithListBox()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='ListBoxItem:nth-child(2n)'>
            <Setter Property='Background' Value='{Binding}'/>
        </Style>
    </Window.Styles>
    <ListBox x:Name='list' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var collection = new ObservableCollection<IBrush>
			{
				Brushes.Red, Brushes.Green, Brushes.Blue
			};

			var list = window.GetControl<ListBox>("list");
			list.ItemsSource = collection;

			window.Show();

			IEnumerable<IBrush?> GetColors()
			{
				return list.GetRealizedContainers().Cast<ListBoxItem>().Select(t => t.Background);
			}

			CornerstoneTest.AreEqual(new[] { Brushes.Transparent, Brushes.Green, Brushes.Transparent }, GetColors());

			collection.Remove(Brushes.Green);
			window.UpdateLayout();

			CornerstoneTest.AreEqual(new[] { Brushes.Transparent, Brushes.Blue }, GetColors());

			collection.Add(Brushes.Violet);
			collection.Add(Brushes.Black);
			window.UpdateLayout();

			CornerstoneTest.AreEqual(new[] { Brushes.Transparent, Brushes.Blue, Brushes.Transparent, Brushes.Black }, GetColors());
		}
	}

	[PresentationTestMethod]
	public void StyleCanUseNthLastChildSelectorAfterReorder()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Border:nth-last-child(2n)'>
            <Setter Property='Background' Value='Red'/>
        </Style>
    </Window.Styles>
    <StackPanel x:Name='parent'>
        <Border x:Name='b1' />
        <Border x:Name='b2' />
    </StackPanel>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);

			var parent = window.GetControl<StackPanel>("parent");
			var b1 = window.GetControl<Border>("b1");
			var b2 = window.GetControl<Border>("b2");

			CornerstoneTest.AreEqual(Brushes.Red, b1.Background);
			CornerstoneTest.IsNull(b2.Background);

			parent.Children.Remove(b1);

			CornerstoneTest.IsNull(b1.Background);
			CornerstoneTest.IsNull(b2.Background);

			parent.Children.Add(b1);

			CornerstoneTest.IsNull(b1.Background);
			CornerstoneTest.AreEqual(Brushes.Red, b2.Background);
		}
	}

	[PresentationTestMethod]
	public void StyleCanUseOrSelector1()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Border.foo, Border.bar'>
            <Setter Property='Background' Value='Red'/>
        </Style>
    </Window.Styles>
    <StackPanel>
        <Border Name='foo' Classes='foo'/>
        <Border Name='bar' Classes='bar'/>
        <Border Name='baz' Classes='baz'/>
    </StackPanel>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var foo = window.GetControl<Border>("foo");
			var bar = window.GetControl<Border>("bar");
			var baz = window.GetControl<Border>("baz");

			CornerstoneTest.AreEqual(Brushes.Red, foo.Background);
			CornerstoneTest.AreEqual(Brushes.Red, bar.Background);
			CornerstoneTest.IsNull(baz.Background);
		}
	}

	[Ignore("Comma selector does not apply Background under CornerstoneTheme in this host.")]
	[PresentationTestMethod]
	public void StyleCanUseOrSelector2()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Button,Carousel,ListBox'>
            <Setter Property='Background' Value='Red'/>
        </Style>
    </Window.Styles>
    <StackPanel>
        <Button Name='button'/>
        <Carousel Name='carousel'/>
        <ListBox Name='listBox'/>
    </StackPanel>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");
			var carousel = window.GetControl<Carousel>("carousel");
			var listBox = window.GetControl<ListBox>("listBox");

			CornerstoneTest.AreEqual(Brushes.Red, button.Background);
			CornerstoneTest.AreEqual(Brushes.Red, carousel.Background);
			CornerstoneTest.AreEqual(Brushes.Red, listBox.Background);
		}
	}

	[PresentationTestMethod]
	public void StyleCanUsePseudolassSelectorWithDash()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Border:foo-bar'>
            <Setter Property='Background' Value='Red'/>
        </Style>
    </Window.Styles>
    <StackPanel>
        <Border Name='foo'/>
    </StackPanel>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var foo = window.GetControl<Border>("foo");

			CornerstoneTest.IsNull(foo.Background);

			((IPseudoClasses) foo.Classes).Add(":foo-bar");

			CornerstoneTest.AreEqual(Colors.Red, ((ISolidColorBrush) foo.Background!).Color);
		}
	}

	[PresentationTestMethod]
	public void TransitionsCanBeStyled()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Border'>
            <Setter Property='Transitions'>
                <Transitions>
                    <DoubleTransition Property='Width' Duration='0:0:1'/>
                </Transitions>
            </Setter>
        </Style>
        <Style Selector='Border.foo'>
            <Setter Property='Transitions'>
                <Transitions>
                    <DoubleTransition Property='Height' Duration='0:0:1'/>
                </Transitions>
            </Setter>
        </Style>
    </Window.Styles>
    <Border/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var border = (Border) window.Content!;

			CornerstoneTest.IsNotNull(border.Transitions);
			CornerstoneTest.AreEqual(1, border.Transitions.Count);
			CornerstoneTest.AreEqual(Border.WidthProperty, border.Transitions[0].Property);

			border.Classes.Add("foo");

			CornerstoneTest.IsNotNull(border.Transitions);
			CornerstoneTest.AreEqual(1, border.Transitions.Count);
			CornerstoneTest.AreEqual(Border.HeightProperty, border.Transitions[0].Property);

			border.Classes.Remove("foo");

			CornerstoneTest.IsNotNull(border.Transitions);
			CornerstoneTest.AreEqual(1, border.Transitions.Count);
			CornerstoneTest.AreEqual(Border.WidthProperty, border.Transitions[0].Property);
		}
	}

	#endregion
}