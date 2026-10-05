#nullable enable

#region References

using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Data;

[TestClass]
public class BindingTestsTemplatedParent : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void BindsToTemplatedParentFromNonControl()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Button Name='button'>
      <Button.Template>
        <ControlTemplate>
          <Grid>
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width='{Binding RelativeSource={RelativeSource TemplatedParent}, Path=Tag}'/>
            </Grid.ColumnDefinitions>
          </Grid>
        </ControlTemplate>
      </Button.Template>
    </Button>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");

			button.Tag = new GridLength(5, GridUnitType.Star);

			window.ApplyTemplate();
			button.ApplyTemplate();

			CornerstoneTest.AreEqual(button.Tag, button.GetTemplateDescendants().OfType<Grid>().First().ColumnDefinitions[0].Width);
		}
	}

	[PresentationTestMethod]
	public void TemplateBindingWithNullPathWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Button Name='button'>
       <Button.Template>
         <ControlTemplate>
           <TextBlock Tag='{TemplateBinding}'/>
         </ControlTemplate>
       </Button.Template>
    </Button>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");

			window.ApplyTemplate();
			button.ApplyTemplate();

			var textBlock = (TextBlock) button.GetVisualChildren().Single();
			CornerstoneTest.Same(button, textBlock.Tag);
		}
	}

	#endregion
}