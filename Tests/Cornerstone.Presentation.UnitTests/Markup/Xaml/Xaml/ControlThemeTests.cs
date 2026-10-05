#nullable enable

#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class ControlThemeTests : XamlTestBase
{
	#region Constants

	private const string ControlThemeXaml = @"
<ControlTheme x:Key='MyTheme' TargetType='u:TestTemplatedControl'>
    <Setter Property='Template'>
        <ControlTemplate>
            <Border/>
        </ControlTemplate>
    </Setter>
    <Style Selector='^ /template/ Border'>
        <Setter Property='Background' Value='Red'/>
    </Style>
</ControlTheme>";

	#endregion

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
						        xmlns:vm='using:Cornerstone.Presentation.UnitTests.Markup.Xaml'>
						    <Window.Resources>
						        <ControlTheme x:Key='MyTheme' TargetType='ContentControl' x:DataType='vm:TestViewModel'>
						            <Setter Property='CornerRadius' Value='10, 0, 0, 10' />
						            <Setter Property='(Classes.Banned)' Value='{Binding Boolean}'/>
						            <Setter Property='Content'>
						                <Template>
						                    <Border CornerRadius='{TemplateBinding CornerRadius}'/>
						                </Template>
						            </Setter>
						            <Setter Property='Template'>
						                <ControlTemplate>
						                    <Button Content='{TemplateBinding Content}'
						                            ContentTemplate='{TemplateBinding ContentTemplate}' />
						                </ControlTemplate>
						            </Setter>

						            <Style Selector='^.Banned'>
						                <Setter Property="TextBlock.TextDecorations" Value="Strikethrough"/>
						            </Style>
						        </ControlTheme>
						    </Window.Resources>
						    <Window.DataContext>
						       <vm:TestViewModel/>
						    </Window.DataContext>
						    <ContentControl Theme='{StaticResource MyTheme}' />
						</Window>
						""";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			window.ApplyTemplate();
			var vm = window.DataContext as TestViewModel;
			CornerstoneTest.IsNotNull(vm);
			var control = CornerstoneTest.IsType<ContentControl>(window.Content);
			CornerstoneTest.IsNull(control.GetValue(TextBlock.TextDecorationsProperty));
			vm.Boolean = true;
			CornerstoneTest.Same(TextDecorations.Strikethrough, control.GetValue(TextBlock.TextDecorationsProperty));
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
						    <Window.Resources>
						        <ControlTheme x:Key='MyTheme' TargetType='ContentControl'>
						            <Setter Property='CornerRadius' Value='10, 0, 0, 10' />
						            <Setter Property='(Classes.Banned)' Value='true'/>
						            <Setter Property='Content'>
						                <Template>
						                    <Border CornerRadius='{TemplateBinding CornerRadius}'/>
						                </Template>
						            </Setter>
						            <Setter Property='Template'>
						                <ControlTemplate>
						                    <Button Content='{TemplateBinding Content}'
						                            ContentTemplate='{TemplateBinding ContentTemplate}' />
						                </ControlTemplate>
						            </Setter>

						            <Style Selector='^.Banned'>
						                <Setter Property="TextBlock.TextDecorations" Value="Strikethrough"/>
						            </Style>
						        </ControlTheme>
						    </Window.Resources>
						    <ContentControl Theme='{StaticResource MyTheme}' />
						</Window>
						""";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var control = CornerstoneTest.IsType<ContentControl>(window.Content);
			CornerstoneTest.Same(TextDecorations.Strikethrough, control.GetValue(TextBlock.TextDecorationsProperty));
		}
	}

	[PresentationTestMethod]
	public void ControlThemeCanBeDynamicResource()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = $@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:u='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml'>
    <Window.Resources>
        {ControlThemeXaml}
    </Window.Resources>

    <u:TestTemplatedControl Theme='{{DynamicResource MyTheme}}'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = CornerstoneTest.IsType<TestTemplatedControl>(window.Content);

			window.Show();

			CornerstoneTest.IsNotNull(button.Template);

			var child = CornerstoneTest.Single(button.GetVisualChildren());
			var border = CornerstoneTest.IsType<Border>(child);

			CornerstoneTest.AreEqual(Brushes.Red, border.Background);
		}
	}

	[PresentationTestMethod]
	public void ControlThemeCanBeSetInStyle()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = $@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:u='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml'>
    <Window.Resources>
        {ControlThemeXaml}
    </Window.Resources>

    <Window.Styles>
        <Style Selector='u|TestTemplatedControl'>
            <Setter Property='Theme' Value='{{StaticResource MyTheme}}'/>
        </Style>
    </Window.Styles>

    <u:TestTemplatedControl/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = CornerstoneTest.IsType<TestTemplatedControl>(window.Content);

			window.Show();

			CornerstoneTest.IsNotNull(button.Template);

			var child = CornerstoneTest.Single(button.GetVisualChildren());
			var border = CornerstoneTest.IsType<Border>(child);

			CornerstoneTest.AreEqual(Brushes.Red, border.Background);
		}
	}

	[PresentationTestMethod]
	public void ControlThemeCanBeStaticResource()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = $@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:u='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml'>
    <Window.Resources>
        {ControlThemeXaml}
    </Window.Resources>

    <u:TestTemplatedControl Theme='{{StaticResource MyTheme}}'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = CornerstoneTest.IsType<TestTemplatedControl>(window.Content);

			window.Show();

			CornerstoneTest.IsNotNull(button.Template);

			var child = CornerstoneTest.Single(button.GetVisualChildren());
			var border = CornerstoneTest.IsType<Border>(child);

			CornerstoneTest.AreEqual(Brushes.Red, border.Background);
		}
	}

	[PresentationTestMethod]
	public void CorrectlyResolveTemplateBindingInNestedStyle()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<ControlTheme xmlns='https://github.com/BobbyCannon/Cornerstone'
              xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
              xmlns:u='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml'
              TargetType='u:TestTemplatedControl'>
    <Setter Property='Template'>
        <ControlTemplate>
            <Border/>
        </ControlTemplate>
    </Setter>
    <Style Selector='^ /template/ Border'>
        <Setter Property='Tag' Value='{TemplateBinding TestData}'/>
    </Style>
</ControlTheme>";

			var theme = (ControlTheme) CornerstoneRuntimeXamlLoader.Load(xaml);
			var style = CornerstoneTest.IsType<Style>(CornerstoneTest.Single(theme.Children));
			var setter = CornerstoneTest.IsType<Setter>(CornerstoneTest.Single(style.Setters));

			CornerstoneTest.AreEqual(TestTemplatedControl.TestDataProperty, (setter.Value as TemplateBinding)?.Property);
		}
	}

	[PresentationTestMethod]
	public void CorrectlyResolveTemplateBindingInThemeDetachedTemplate()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:u='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml'>
    <Window.Resources>
        <ControlTheme x:Key='MyTheme' TargetType='ContentControl'>
            <Setter Property='CornerRadius' Value='10, 0, 0, 10' />
            <Setter Property='Content'>
                <Template>
                    <Border CornerRadius='{TemplateBinding CornerRadius}'/>
                </Template>
            </Setter>
            <Setter Property='Template'>
                <ControlTemplate>
                    <Button Content='{TemplateBinding Content}'
                            ContentTemplate='{TemplateBinding ContentTemplate}' />
                </ControlTemplate>
            </Setter>
        </ControlTheme>
    </Window.Resources>

    <ContentControl Theme='{StaticResource MyTheme}' />
</Window>");
			var control = CornerstoneTest.IsType<ContentControl>(window.Content);

			window.Show();

			var border = CornerstoneTest.IsType<Border>(control.Content);

			CornerstoneTest.AreEqual(new CornerRadius(10, 0, 0, 10), border.CornerRadius);
		}
	}

	#endregion
}