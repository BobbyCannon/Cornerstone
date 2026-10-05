#nullable enable

#region References

using System;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Markup.Xaml.Templates;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class DataTemplateTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void CanSetDataContextInDataTemplate()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Window.DataTemplates>
        <DataTemplate DataType='{x:Type local:TestViewModel}'>
            <Canvas Name='foo' DataContext='{Binding Child}'/>
        </DataTemplate>
    </Window.DataTemplates>
    <ContentControl Name='target' Content='{Binding Child}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var target = window.GetControl<ContentControl>("target");

			var viewModel = new TestViewModel
			{
				String = "Root",
				Child = new TestViewModel
				{
					String = "Child",
					Child = new TestViewModel
					{
						String = "Grandchild"
					}
				}
			};

			window.DataContext = viewModel;

			window.ApplyTemplate();
			target.ApplyTemplate();
			target.Presenter!.UpdateChild();

			var canvas = (Canvas) target.Presenter.Child!;
			CornerstoneTest.Same(viewModel, target.DataContext);
			CornerstoneTest.Same(viewModel.Child, target.Presenter.DataContext);
			CornerstoneTest.Same(viewModel.Child.Child, canvas.DataContext);
		}
	}

	[PresentationTestMethod]
	public void DataTemplateCanBeEmpty()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:sys='clr-namespace:System;assembly=netstandard'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.DataTemplates>
        <DataTemplate DataType='{x:Type sys:String}' />
    </Window.DataTemplates>
    <ContentControl Name='target' Content='Foo'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var target = window.GetControl<ContentControl>("target");

			window.ApplyTemplate();
			target.ApplyTemplate();
			target.Presenter!.UpdateChild();

			CornerstoneTest.IsNull(target.Presenter.Child);
		}
	}

	[PresentationTestMethod]
	public void DataTemplateCanContainName()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:sys='clr-namespace:System;assembly=netstandard'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.DataTemplates>
        <DataTemplate DataType='{x:Type sys:String}'>
            <Canvas Name='foo'/>
        </DataTemplate>
    </Window.DataTemplates>
    <ContentControl Name='target' Content='Foo'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var target = window.GetControl<ContentControl>("target");

			window.ApplyTemplate();
			target.ApplyTemplate();
			target.Presenter!.UpdateChild();

			CornerstoneTest.IsType<Canvas>(target.Presenter.Child);
		}
	}

	[PresentationTestMethod]
	public void DataTemplateCanContainNamedUserControl()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:sys='clr-namespace:System;assembly=mscorlib'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ItemsControl Name='itemsControl' ItemsSource='{Binding}'>
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <UserControl Name='foo'/>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var itemsControl = window.GetControl<ItemsControl>("itemsControl");

			window.DataContext = new[] { "item1", "item2" };

			window.ApplyTemplate();
			itemsControl.ApplyTemplate();
			itemsControl.Presenter!.ApplyTemplate();

			CornerstoneTest.AreEqual(2, itemsControl.Presenter.Panel!.Children.Count);
		}
	}

	[PresentationTestMethod]
	public void DataTemplatesWithoutTypeShouldThrow()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:sys='clr-namespace:System;assembly=netstandard'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.DataTemplates>
        <DataTemplate>
            <Canvas Name='foo'/>
        </DataTemplate>
    </Window.DataTemplates>
    <ContentControl Name='target' Content='Foo'/>
</Window>";
			Assert.Throws<InvalidOperationException>(() => (Window) CornerstoneRuntimeXamlLoader.Load(xaml));
		}
	}

	[PresentationTestMethod]
	public void XDataTypeShouldBeAssignedToClrProperty()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:sys='clr-namespace:System;assembly=netstandard'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.DataTemplates>
        <DataTemplate x:DataType='sys:String'>
            <Canvas Name='foo'/>
        </DataTemplate>
    </Window.DataTemplates>
    <ContentControl Name='target' Content='Foo'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var target = window.GetControl<ContentControl>("target");
			var template = (DataTemplate) window.DataTemplates.First();

			window.ApplyTemplate();
			target.ApplyTemplate();
			target.Presenter!.UpdateChild();

			CornerstoneTest.AreEqual(typeof(string), template.DataType);
			CornerstoneTest.IsType<Canvas>(target.Presenter.Child);
		}
	}

	[PresentationTestMethod]
	public void XDataTypeShouldBeIgnoredIfDataTypeAlreadySet()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:sys='clr-namespace:System;assembly=netstandard'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.DataTemplates>
        <DataTemplate DataType='sys:String' x:DataType='UserControl'>
            <Canvas Name='foo'/>
        </DataTemplate>
    </Window.DataTemplates>
    <ContentControl Name='target' Content='Foo'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var target = window.GetControl<ContentControl>("target");

			window.ApplyTemplate();
			target.ApplyTemplate();
			target.Presenter!.UpdateChild();

			CornerstoneTest.IsType<Canvas>(target.Presenter.Child);
		}
	}

	[PresentationTestMethod]
	public void XDataTypeShouldBeIgnoredIfDataTypeHasNonStandardName()
	{
		// We don't want DataType to be mapped to FancyDataType, avoid possible confusion.
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:sys='clr-namespace:System;assembly=netstandard'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'>
    <ContentControl Name='target' Content='Foo'>
        <ContentControl.ContentTemplate>
            <local:CustomDataTemplate x:DataType='local:TestDataContext'>
                <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
            </local:CustomDataTemplate>
        </ContentControl.ContentTemplate>
    </ContentControl>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var target = window.GetControl<ContentControl>("target");

			window.ApplyTemplate();
			target.ApplyTemplate();
			target.Presenter!.UpdateChild();

			var dataTemplate = (CustomDataTemplate) target.ContentTemplate!;
			CornerstoneTest.IsNull(dataTemplate.FancyDataType);
		}
	}

	#endregion
}