#nullable enable

#region References

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Metadata;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Diagnostics;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Markup.Xaml.Templates;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class ControlTemplateTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ControlTemplateAttachedValuesAreSetWithStylePriority()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Button>
        <Button.Template>
            <ControlTemplate>
                <ContentPresenter Name='PART_ContentPresenter'
                                  DockPanel.Dock='Top'/>
            </ControlTemplate>
        </Button.Template>
    </Button>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = (Button) window.Content!;

			window.ApplyTemplate();
			button.ApplyTemplate();

			var presenter = button.Presenter;
			CornerstoneTest.IsNotNull(presenter);
			CornerstoneTest.AreEqual(Dock.Top, DockPanel.GetDock(presenter));

			var diagnostic = presenter.GetDiagnostic(DockPanel.DockProperty);
			CornerstoneTest.AreEqual(BindingPriority.Template, diagnostic.Priority);
		}
	}

	[PresentationTestMethod]
	public void ControlTemplateCanBeEmpty()
	{
		var xaml = "<ControlTemplate xmlns='https://github.com/BobbyCannon/Cornerstone' />";
		var template = CornerstoneRuntimeXamlLoader.Parse<ControlTemplate>(xaml);

		var templateResult = template.Build(new TemplatedControl());
		CornerstoneTest.IsNull(templateResult);
	}

	[PresentationTestMethod]
	public void ControlTemplateDynamicResourcesAreSetWithStylePriority()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='red'>Red</SolidColorBrush>
    </Window.Resources>
    <Button Content='Foo'>
        <Button.Template>
            <ControlTemplate>
                <ContentPresenter Name='PART_ContentPresenter'
                                  Background='{DynamicResource red}'/>
            </ControlTemplate>
        </Button.Template>
    </Button>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = (Button) window.Content!;

			window.ApplyTemplate();
			button.ApplyTemplate();

			var presenter = button.Presenter;
			CornerstoneTest.IsNotNull(presenter);
			CornerstoneTest.AreEqual(Brushes.Red, presenter.Background);

			var diagnostic = presenter.GetDiagnostic(Button.BackgroundProperty);
			CornerstoneTest.AreEqual(BindingPriority.Template, diagnostic.Priority);
		}
	}

	[PresentationTestMethod]
	public void ControlTemplateOutputsErrorWhenMissingTemplatePart()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var xaml = @"
<ControlTemplate xmlns='https://github.com/BobbyCannon/Cornerstone'
                 xmlns:controls='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml'
                 TargetType='controls:CustomButtonWithParts'>
    <Border Name='PART_Typo_MainContentBorder'>
        <ContentPresenter Name='PART_ContentPresenter'
                          Content='{TemplateBinding Content}'/>
    </Border>
</ControlTemplate>";
		var diagnostics = new List<RuntimeXamlDiagnostic>();
		CornerstoneRuntimeXamlLoader.Load(new RuntimeXamlLoaderDocument(xaml), new RuntimeXamlLoaderConfiguration
		{
			LocalAssembly = typeof(XamlIlTests).Assembly,
			DiagnosticHandler = diagnostic =>
			{
				diagnostics.Add(diagnostic);
				return diagnostic.Severity;
			}
		});
		var warning = CornerstoneTest.Single(diagnostics);
		CornerstoneTest.AreEqual(RuntimeXamlDiagnosticSeverity.Info, warning.Severity);
		CornerstoneTest.Contains(warning.Title, "'PART_MainContentBorder'");
	}

	[PresentationTestMethod]
	public void ControlTemplateOutputsErrorWhenMissingTemplatePartNestedItemTemplateCase()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var xaml = @"
<ControlTemplate xmlns='https://github.com/BobbyCannon/Cornerstone'
                 xmlns:controls='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml'
                 TargetType='controls:CustomControlWithParts'>
    <Border Name='PART_Typo_MainContentBorder'>
        <StackPanel>
            <ItemsControl>
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <!-- This PART_MainContentBorder shouldn't full parent ControlTemplate, PART_Typo_MainContentBorder still isn't properly named. -->
                        <Border Name='PART_MainContentBorder' />
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
            <ContentPresenter Name='PART_ContentPresenter'
                              Content='{TemplateBinding Content}'/>
        </StackPanel>
    </Border>
</ControlTemplate>";
		var diagnostics = new List<RuntimeXamlDiagnostic>();
		CornerstoneRuntimeXamlLoader.Load(new RuntimeXamlLoaderDocument(xaml), new RuntimeXamlLoaderConfiguration
		{
			LocalAssembly = typeof(XamlIlTests).Assembly,
			DiagnosticHandler = diagnostic =>
			{
				diagnostics.Add(diagnostic);
				return diagnostic.Severity;
			}
		});
		var warning = CornerstoneTest.Single(diagnostics);
		CornerstoneTest.AreEqual(RuntimeXamlDiagnosticSeverity.Info, warning.Severity);
		CornerstoneTest.Contains(warning.Title, "'PART_MainContentBorder'");
	}

	[PresentationTestMethod]
	public void ControlTemplateOutputsErrorWhenUsingWrongTypeWithTemplatePart()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var xaml = @"
<ControlTemplate xmlns='https://github.com/BobbyCannon/Cornerstone'
                 xmlns:controls='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml'
                 TargetType='controls:CustomControlWithParts'>
    <Border Name='PART_MainContentBorder'>
        <ContentControl Name='PART_ContentPresenter'
                        Content='{TemplateBinding Content}'/>
    </Border>
</ControlTemplate>";
		var diagnostics = new List<RuntimeXamlDiagnostic>();
		CornerstoneTest.Throws<XmlException>(() => CornerstoneRuntimeXamlLoader.Load(new RuntimeXamlLoaderDocument(xaml), new RuntimeXamlLoaderConfiguration
		{
			LocalAssembly = typeof(XamlIlTests).Assembly,
			DiagnosticHandler = diagnostic =>
			{
				diagnostics.Add(diagnostic);
				return diagnostic.Severity;
			}
		}));
		var warning = CornerstoneTest.Single(diagnostics);
		CornerstoneTest.AreEqual(RuntimeXamlDiagnosticSeverity.Error, warning.Severity);
		CornerstoneTest.Contains(warning.Title, "'ContentPresenter'");
	}

	[PresentationTestMethod]
	public void ControlTemplateStaticResourcesAreSetWithStylePriority()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='red'>Red</SolidColorBrush>
    </Window.Resources>
    <Button Content='Foo'>
        <Button.Template>
            <ControlTemplate>
                <ContentPresenter Name='PART_ContentPresenter'
                                  Background='{StaticResource red}'/>
            </ControlTemplate>
        </Button.Template>
    </Button>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = (Button) window.Content!;

			window.ApplyTemplate();
			button.ApplyTemplate();

			var presenter = button.Presenter;
			CornerstoneTest.IsNotNull(presenter);
			CornerstoneTest.AreEqual(Brushes.Red, presenter.Background);

			var diagnostic = presenter.GetDiagnostic(Button.BackgroundProperty);
			CornerstoneTest.AreEqual(BindingPriority.Template, diagnostic.Priority);
		}
	}

	[PresentationTestMethod]
	public void ControlTemplateTemplateBindingsAreSetWithTemplatedParentPriority()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Button Content='Foo'>
        <Button.Template>
            <ControlTemplate>
                <ContentPresenter Name='PART_ContentPresenter'
                                  Content='{TemplateBinding Content}'/>
            </ControlTemplate>
        </Button.Template>
    </Button>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = (Button) window.Content!;

			window.ApplyTemplate();
			button.ApplyTemplate();

			var presenter = button.Presenter;
			CornerstoneTest.IsNotNull(presenter);
			CornerstoneTest.AreEqual("Foo", presenter.Content);

			var diagnostic = presenter.GetDiagnostic(ContentPresenter.ContentProperty);
			CornerstoneTest.AreEqual(BindingPriority.Template, diagnostic.Priority);
		}
	}

	[PresentationTestMethod]
	public void ControlTemplateWithNestedChildIsOperational()
	{
		var xaml = @"
<ControlTemplate xmlns='https://github.com/BobbyCannon/Cornerstone'>
    <ContentControl Name='parent'>
        <ContentControl Name='child' />
    </ContentControl>
</ControlTemplate>
";
		var template = CornerstoneRuntimeXamlLoader.Parse<ControlTemplate>(xaml);

		var parent = (ContentControl) template.Build(new ContentControl())!.Result;

		CornerstoneTest.AreEqual("parent", parent.Name);

		var child = parent.Content as ContentControl;

		CornerstoneTest.IsNotNull(child);

		CornerstoneTest.AreEqual("child", child.Name);
	}

	[PresentationTestMethod]
	public void ControlTemplateWithPanelChildrenAreAdded()
	{
		var xaml = @"
<ControlTemplate xmlns='https://github.com/BobbyCannon/Cornerstone'>
    <Panel Name='panel'>
        <ContentControl Name='Foo' />
        <ContentControl Name='Bar' />
    </Panel>
</ControlTemplate>
";
		var template = CornerstoneRuntimeXamlLoader.Parse<ControlTemplate>(xaml);

		var panel = (Panel) template.Build(new ContentControl())!.Result;

		CornerstoneTest.AreEqual(2, panel.Children.Count);

		var foo = panel.Children[0];
		var bar = panel.Children[1];

		CornerstoneTest.AreEqual("Foo", foo.Name);
		CornerstoneTest.AreEqual("Bar", bar.Name);
	}

	[PresentationTestMethod]
	public void ControlTemplateWithStringTargetType()
	{
		var xaml = @"
<ControlTemplate xmlns='https://github.com/BobbyCannon/Cornerstone' 
                 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                 TargetType='ContentControl'>
    <ContentPresenter x:Name='PART_ContentPresenter' Content='{TemplateBinding Content}' />
</ControlTemplate>
";
		var template = CornerstoneRuntimeXamlLoader.Parse<ControlTemplate>(xaml);

		CornerstoneTest.AreEqual(typeof(ContentControl), template.TargetType);

		CornerstoneTest.IsType(typeof(ContentPresenter), template.Build(new ContentControl())!.Result);
	}

	[PresentationTestMethod]
	public void ControlTemplateWithTargetTypeIsOperational()
	{
		var xaml = @"
<ControlTemplate xmlns='https://github.com/BobbyCannon/Cornerstone' 
                 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                 TargetType='{x:Type ContentControl}'>
    <ContentPresenter x:Name='PART_ContentPresenter' Content='{TemplateBinding Content}' />
</ControlTemplate>
";
		var template = CornerstoneRuntimeXamlLoader.Parse<ControlTemplate>(xaml);

		CornerstoneTest.AreEqual(typeof(ContentControl), template.TargetType);

		CornerstoneTest.IsType(typeof(ContentPresenter), template.Build(new ContentControl())!.Result);
	}

	[PresentationTestMethod]
	public void CustomControlTemplateAllowsTemplateBindings()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(
				"""
				<Window xmlns="https://github.com/BobbyCannon/Cornerstone"
				        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				        xmlns:controls="using:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml">
				    <Button Content="Foo">
				        <Button.Template>
				            <controls:CustomControlTemplate>
				                <ContentPresenter Name="PART_ContentPresenter"
				                                  Content="{TemplateBinding Content}"/>
				            </controls:CustomControlTemplate>
				        </Button.Template>
				    </Button>
				</Window>
				""");
			var button = CornerstoneTest.IsType<Button>(window.Content);

			window.ApplyTemplate();
			button.ApplyTemplate();

			var presenter = button.Presenter;
			CornerstoneTest.IsNotNull(presenter);
			CornerstoneTest.AreEqual("Foo", presenter.Content);
		}
	}

	[PresentationTestMethod]
	public void InlineControlTemplateStyledValuesAreSetWithStylePriority()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Button>
        <Button.Template>
            <ControlTemplate>
                <ContentPresenter Name='PART_ContentPresenter'
                                  Background='Red'/>
            </ControlTemplate>
        </Button.Template>
    </Button>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = (Button) window.Content!;

			window.ApplyTemplate();
			button.ApplyTemplate();

			var presenter = button.Presenter!;
			CornerstoneTest.AreEqual(Brushes.Red, presenter.Background);

			var diagnostic = presenter.GetDiagnostic(Button.BackgroundProperty);
			CornerstoneTest.AreEqual(BindingPriority.Template, diagnostic.Priority);
		}
	}

	[PresentationTestMethod]
	public void StyleControlTemplateStyledValuesAreSetWithStylePriority()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style Selector='Button'>
            <Setter Property='Template'>
                <ControlTemplate>
                    <ContentPresenter Name='PART_ContentPresenter'
                                      Background='Red'/>
                </ControlTemplate>
            </Setter>
        </Style>
    </Window.Styles>
    <Button/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = (Button) window.Content!;

			window.ApplyTemplate();
			button.ApplyTemplate();

			var presenter = button.Presenter;
			CornerstoneTest.IsNotNull(presenter);
			CornerstoneTest.AreEqual(Brushes.Red, presenter.Background);

			var diagnostic = presenter.GetDiagnostic(Button.BackgroundProperty);
			CornerstoneTest.AreEqual(BindingPriority.Template, diagnostic.Priority);
		}
	}

	[PresentationTestMethod]
	public void StyledPropertiesShouldBeSetInTheControlTemplate()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:controls=""using:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml"">
    <Button>
        <Button.Template>
            <ControlTemplate>
                <controls:ListBoxHierarchyLine>
                    <controls:ListBoxHierarchyLine.LineDashStyle>
                        <DashStyle Dashes=""2,2"" Offset=""1"" />
                    </controls:ListBoxHierarchyLine.LineDashStyle>
                </controls:ListBoxHierarchyLine>
            </ControlTemplate>
        </Button.Template>
    </Button>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = (Button) window.Content!;

			window.ApplyTemplate();
			button.ApplyTemplate();
			var listBoxHierarchyLine = button.GetVisualChildren().ElementAt(0) as ListBoxHierarchyLine;
			CornerstoneTest.IsNotNull(listBoxHierarchyLine);
			CornerstoneTest.IsNotNull(listBoxHierarchyLine.LineDashStyle);
			CornerstoneTest.IsNotNull(listBoxHierarchyLine.LineDashStyle.Dashes);
			CornerstoneTest.AreEqual(1, listBoxHierarchyLine.LineDashStyle.Offset);
			CornerstoneTest.AreEqual(2, listBoxHierarchyLine.LineDashStyle.Dashes.Count);
			CornerstoneTest.AreEqual(2, listBoxHierarchyLine.LineDashStyle.Dashes[0]);
			CornerstoneTest.AreEqual(2, listBoxHierarchyLine.LineDashStyle.Dashes[1]);
		}
	}

	#endregion
}

public class ListBoxHierarchyLine : Panel
{
	#region Fields

	public static readonly StyledProperty<DashStyle?> LineDashStyleProperty =
		PresentationProperty.Register<ListBoxHierarchyLine, DashStyle?>(nameof(LineDashStyle));

	#endregion

	#region Properties

	public DashStyle? LineDashStyle
	{
		get => GetValue(LineDashStyleProperty);
		set => SetValue(LineDashStyleProperty, value);
	}

	#endregion
}

[TemplatePart("PART_MainContentBorder", typeof(Border))]
[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
public class CustomControlWithParts : ContentControl
{
}

public class CustomButtonWithParts : CustomControlWithParts
{
}

public class CustomControlTemplate : IControlTemplate
{
	#region Properties

	[Content]
	[TemplateContent]
	public object? Content { get; set; }

	public Type? TargetType { get; set; }

	#endregion

	#region Methods

	public TemplateResult<Control>? Build(TemplatedControl control)
	{
		return TemplateContent.Load(Content);
	}

	#endregion
}