#nullable enable

#region References

using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class ItemsPanelTemplateTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ItemsPanelTemplateInControlAllowsTemplateBinding()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(
				"""
				<Window xmlns="https://github.com/BobbyCannon/Cornerstone"
				        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				    <ListBox Background="DodgerBlue">
				        <ListBox.Template>
				            <ControlTemplate>
				                <ItemsPresenter Name="PART_ItemsPresenter"
				                                ItemsPanel="{TemplateBinding ItemsPanel}" />
				            </ControlTemplate>
				        </ListBox.Template>
				        <ListBox.ItemsPanel>
				            <ItemsPanelTemplate>
				                <Panel Background="{TemplateBinding Background}"
				                       Tag="{TemplateBinding ItemsSource}" />
				            </ItemsPanelTemplate>
				        </ListBox.ItemsPanel>
				    </ListBox>
				</Window>
				""");
			var listBox = CornerstoneTest.IsType<ListBox>(window.Content);
			var items = new[] { "foo", "bar" };
			listBox.ItemsSource = items;

			window.ApplyTemplate();
			listBox.ApplyTemplate();

			var itemsPresenter = listBox.FindDescendantOfType<ItemsPresenter>();
			CornerstoneTest.IsNotNull(itemsPresenter);
			itemsPresenter.ApplyTemplate();

			var panel = itemsPresenter.Panel;
			CornerstoneTest.IsNotNull(panel);
			CornerstoneTest.AreEqual(Brushes.DodgerBlue, panel.Background);
			CornerstoneTest.Same(items, panel.Tag);
		}
	}

	[PresentationTestMethod]
	public void ItemsPanelTemplateInStyleAllowsTemplateBinding()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(
				"""
				<Window xmlns="https://github.com/BobbyCannon/Cornerstone"
				        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
				    <Window.Styles>
				        <Style Selector="ListBox">
				            <Setter Property="Template">
				                <ControlTemplate>
				                    <ItemsPresenter Name="PART_ItemsPresenter"
				                                    ItemsPanel="{TemplateBinding ItemsPanel}" />
				                </ControlTemplate>
				            </Setter>
				            <Setter Property="ItemsPanel">
				                <ItemsPanelTemplate>
				                    <Panel Background="{TemplateBinding Background}"
				                           Tag="{TemplateBinding ItemsSource}" />
				                </ItemsPanelTemplate>
				            </Setter>
				        </Style>
				    </Window.Styles>
				    <ListBox Background="DodgerBlue" />
				</Window>
				""");
			var listBox = CornerstoneTest.IsType<ListBox>(window.Content);
			var items = new[] { "foo", "bar" };
			listBox.ItemsSource = items;

			window.ApplyTemplate();
			listBox.ApplyTemplate();

			var itemsPresenter = listBox.FindDescendantOfType<ItemsPresenter>();
			CornerstoneTest.IsNotNull(itemsPresenter);
			itemsPresenter.ApplyTemplate();

			var panel = itemsPresenter.Panel;
			CornerstoneTest.IsNotNull(panel);
			CornerstoneTest.AreEqual(Brushes.DodgerBlue, panel.Background);
			CornerstoneTest.Same(items, panel.Tag);
		}
	}

	#endregion
}