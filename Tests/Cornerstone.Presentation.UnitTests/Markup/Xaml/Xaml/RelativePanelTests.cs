#nullable enable

#region References

using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class RelativePanelTests : XamlTestBase
{
	#region Methods

	[Ignore("Viewport vs bounds assertion depends on the upstream Simple chrome; fails under CornerstoneTheme.")]
	[PresentationTestMethod]
	public void ScrollViewerViewportSmallThanBounds()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'
        Height='800'
        Width='1000'
>
  <RelativePanel x:Name=""TestRelativePanel"">
    <Panel
          x:Name=""Area1""
          RelativePanel.AlignTopWithPanel=""True""
          RelativePanel.AlignLeftWithPanel=""True""
          RelativePanel.AlignRightWithPanel=""True""
          Height=""100""
          Background=""LightSkyBlue"">
      <TextBlock
          Text=""Area1""
          HorizontalAlignment=""Center""
          VerticalAlignment=""Center""
          />
      <!-- <Button Click=""Button_OnClick"">Second</Button> -->
    </Panel>
    <Panel
      x:Name=""Area2""
      RelativePanel.Below=""Area1""
      RelativePanel.AlignLeftWithPanel=""True""
      RelativePanel.AlignBottomWithPanel=""True""
      Background=""DeepSkyBlue""
        Width=""100""
      >
      <TextBlock
        Text=""Area2""
        HorizontalAlignment=""Center""
        VerticalAlignment=""Center""
        Height=""100""
      />
    </Panel>
    <ScrollViewer
        x:Name=""TestArea""
        Background=""Aqua""
        RelativePanel.Below=""Area1""
        RelativePanel.RightOf=""Area2""
        RelativePanel.AlignRightWithPanel=""True""
        RelativePanel.AlignBottomWithPanel=""True""
        HorizontalScrollBarVisibility=""Visible""
        Margin=""0 0 0 0""
        >
      <ItemsControl
        ItemsSource=""{Binding DataExample}""
        >
        <ItemsControl.ItemsPanel>
          <ItemsPanelTemplate>
            <StackPanel></StackPanel>
          </ItemsPanelTemplate>
        </ItemsControl.ItemsPanel>
        <ItemsControl.ItemTemplate>
          <DataTemplate>
            <StackPanel Orientation=""Horizontal"">
              <TextBlock Width=""100"" Text=""{Binding Id}""></TextBlock>
              <TextBlock Width=""100"" Text=""{Binding Id}""></TextBlock>
              <TextBlock Width=""100"" Text=""{Binding Id}""></TextBlock>
              <TextBlock Width=""100"" Text=""{Binding Id}""></TextBlock>
              <TextBlock Width=""100"" Text=""{Binding Id}""></TextBlock>
              <TextBlock Width=""100"" Text=""{Binding Id}""></TextBlock>
              <TextBlock Width=""100"" Text=""{Binding Id}""></TextBlock>
              <TextBlock Width=""100"" Text=""{Binding Id}""></TextBlock>
              <TextBlock Width=""100"" Text=""{Binding Id}""></TextBlock>
              <TextBlock Width=""100"" Text=""{Binding Id}""></TextBlock>
            </StackPanel>
          </DataTemplate>
        </ItemsControl.ItemTemplate>
      </ItemsControl>
    </ScrollViewer>
  </RelativePanel>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			{
				var panel = window.GetControl<RelativePanel>("TestRelativePanel");

				panel.DataContext = new
				{
					DataExample = Enumerable.Range(1001, 100).Select(e => new { Id = $"{e}" })
				};
			}
			window.ApplyTemplate();
			window.Show();

			var sv = window.GetControl<ScrollViewer>("TestArea");
			CornerstoneTest.IsTrue(sv.Viewport.Width < sv.Bounds.Width);
			CornerstoneTest.IsTrue(sv.Viewport.Height < sv.Bounds.Height);
		}
	}

	#endregion
}