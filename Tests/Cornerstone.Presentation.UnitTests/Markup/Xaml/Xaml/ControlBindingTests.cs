#nullable enable

#region References

using System.Collections.Generic;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class ControlBindingTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void BindingProgressBarValueToInvalidValueUsesFallbackValue()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'>
    <ProgressBar Maximum='10' Value='{Binding Value, FallbackValue=3}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var progressBar = (ProgressBar) window.Content!;

			window.DataContext = new { Value = "foo" };
			window.ApplyTemplate();

			CornerstoneTest.AreEqual(3, progressBar.Value);
		}
	}

	[PresentationTestMethod]
	public void CanBindBetweenTabStripAndCarousel()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'>
    <DockPanel>
        <TabStrip Name='strip' DockPanel.Dock='Top' ItemsSource='{Binding Items}' SelectedIndex='0'>
          <TabStrip.ItemTemplate>
            <DataTemplate>
              <TextBlock Text='{Binding Header}'/>
            </DataTemplate>
          </TabStrip.ItemTemplate>
        </TabStrip>
        <Carousel Name='carousel' ItemsSource='{Binding Items}' SelectedIndex='{Binding #strip.SelectedIndex}'>
          <Carousel.ItemTemplate>
            <DataTemplate>
              <TextBlock Text='{Binding Detail}'/>
            </DataTemplate>
          </Carousel.ItemTemplate>
        </Carousel>
    </DockPanel>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var strip = window.GetControl<TabStrip>("strip");
			var carousel = window.GetControl<Carousel>("carousel");

			window.DataContext = new ItemsViewModel
			{
				Items = new[]
				{
					new ItemViewModel { Header = "Item1", Detail = "Detail1" },
					new ItemViewModel { Header = "Item2", Detail = "Detail2" }
				}
			};

			window.Show();

			CornerstoneTest.AreEqual(0, strip.SelectedIndex);
			CornerstoneTest.AreEqual(0, carousel.SelectedIndex);
		}
	}

	#endregion

	#region Classes

	private class ItemViewModel
	{
		#region Properties

		public string? Detail { get; set; }
		public string? Header { get; set; }

		#endregion
	}

	private class ItemsViewModel
	{
		#region Properties

		public IList<ItemViewModel>? Items { get; set; }

		#endregion
	}

	#endregion
}