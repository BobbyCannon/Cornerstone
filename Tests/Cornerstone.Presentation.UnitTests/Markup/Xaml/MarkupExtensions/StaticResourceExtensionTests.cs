#nullable enable

#region References

using System;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;

[TestClass]
public class StaticResourceExtensionTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AutomaticallyConvertsColorToSolidColorBrush()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <Color x:Key='color'>#ff506070</Color>
    </UserControl.Resources>

    <Border Name='border' Background='{StaticResource color}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		var brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(border.Background);
		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
	}

	[PresentationTestMethod]
	public void AutomaticallyConvertsColorToSolidColorBrushFromSetter()
	{
		using (StyledWindow())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <Color x:Key='color'>#ff506070</Color>
    </Window.Resources>
    <Window.Styles>
        <Style Selector='Button'>
            <Setter Property='Background' Value='{StaticResource color}'/>
        </Style>
    </Window.Styles>
    <Button Name='button'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");
			var brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(button.Background);

			CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
		}
	}

	[PresentationTestMethod]
	public void ControlPropertyIsNotUpdatedWhenParentIsChanged()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
    </UserControl.Resources>

    <Border Name='border' Background='{StaticResource brush}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		var brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(border.Background);
		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());

		userControl.Content = null;

		brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(border.Background);
		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
	}

	[PresentationTestMethod]
	public void StaticResourceCanBeAssignedToAttachedProperty()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <x:Int32 x:Key='col'>5</x:Int32>
    </UserControl.Resources>

    <Border Name='border' Grid.Column='{StaticResource col}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		CornerstoneTest.AreEqual(5, Grid.GetColumn(border));
	}

	[PresentationTestMethod]
	public void StaticResourceCanBeAssignedToBindingConverterInDataTemplate()
	{
		using (StyledWindow())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'>
    <Window.Resources>
        <local:TestValueConverter x:Key='converter' Append='bar'/>
        <DataTemplate x:Key='PurpleData'>
          <TextBlock Name='textBlock' Text='{Binding Converter={StaticResource converter}}'/>
        </DataTemplate>
    </Window.Resources>

    <ContentPresenter Name='presenter' Content='foo' ContentTemplate='{StaticResource PurpleData}'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);

			window.DataContext = "foo";
			var presenter = window.GetControl<ContentPresenter>("presenter");

			window.Show();

			var textBlock = (TextBlock) presenter.GetVisualChildren().Single();

			CornerstoneTest.IsNotNull(textBlock);
			CornerstoneTest.AreEqual("foobar", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void StaticResourceCanBeAssignedToConverter()
	{
		using (StyledWindow())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'>
    <Window.Resources>
        <local:TestValueConverter x:Key='converter' Append='bar'/>
    </Window.Resources>

    <TextBlock Name='textBlock' Text='{Binding Converter={StaticResource converter}}'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			window.DataContext = "foo";
			window.ApplyTemplate();

			CornerstoneTest.AreEqual("foobar", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void StaticResourceCanBeAssignedToItemTemplateProperty()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <DataTemplate x:Key='PurpleData'>
          <TextBlock Text='{Binding Name}' Background='Purple'/>
        </DataTemplate>
    </UserControl.Resources>

    <ListBox Name='listBox' ItemTemplate='{StaticResource PurpleData}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var listBox = userControl.GetControl<ListBox>("listBox");

		CornerstoneTest.IsNotNull(listBox.ItemTemplate);
	}

	[PresentationTestMethod]
	public void StaticResourceCanBeAssignedToProperty()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
    </UserControl.Resources>

    <Border Name='border' Background='{StaticResource brush}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		var brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(border.Background);
		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
	}

	[PresentationTestMethod]
	public void StaticResourceCanBeAssignedToPropertyInControlTemplateInStylesFile()
	{
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Style.xaml"), @"
<Styles xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Styles.Resources>
        <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
    </Styles.Resources>

    <Style Selector='Button'>
        <Setter Property='Template'>
            <ControlTemplate>
                <Border Name='border' Background='{StaticResource brush}'/>
            </ControlTemplate>
        </Setter>
    </Style>
</Styles>"),
			new RuntimeXamlLoaderDocument(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <StyleInclude Source='csres://Tests/Style.xaml'/>
    </Window.Styles>
    <Button Name='button'/>
</Window>")
		};

		using (StyledWindow())
		{
			var compiled = CornerstoneXamlIlRuntimeLoader.LoadGroup(documents);
			var window = CornerstoneTest.IsType<Window>(compiled[1]);
			var button = window.GetControl<Button>("button");

			window.Show();

			var border = (Border) button.GetVisualChildren().Single();
			var brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(border.Background);

			CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
		}
	}

	[PresentationTestMethod]
	public void StaticResourceCanBeAssignedToResourceProperty()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <Color x:Key='color'>#ff506070</Color>
        <SolidColorBrush x:Key='brush' Color='{StaticResource color}'/>
    </UserControl.Resources>

    <Border Name='border' Background='{StaticResource brush}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		var brush = CornerstoneTest.IsAssignableFrom<SolidColorBrush>(border.Background);
		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
	}

	[PresentationTestMethod]
	public void StaticResourceCanBeAssignedToResourcePropertyInStylesFile()
	{
		var xaml = @"
<Styles xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Styles.Resources>
        <Color x:Key='color'>#ff506070</Color>
        <SolidColorBrush x:Key='brush' Color='{StaticResource color}'/>
    </Styles.Resources>
</Styles>";

		var styles = (Styles) CornerstoneRuntimeXamlLoader.Load(xaml);
		var brush = CornerstoneTest.IsAssignableFrom<SolidColorBrush>(styles.Resources["brush"]);

		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
	}

	[PresentationTestMethod]
	public void StaticResourceCanBeAssignedToSetter()
	{
		using (StyledWindow())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
    </Window.Resources>
    <Window.Styles>
        <Style Selector='Button'>
            <Setter Property='Background' Value='{StaticResource brush}'/>
        </Style>
    </Window.Styles>
    <Button Name='button'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");
			var brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(button.Background);

			CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
		}
	}

	[PresentationTestMethod]
	public void StaticResourceCanBeAssignedToSetterInStylesFile()
	{
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Style.xaml"), @"
<Styles xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Styles.Resources>
        <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
    </Styles.Resources>

    <Style Selector='Border'>
        <Setter Property='Background' Value='{StaticResource brush}'/>
    </Style>
</Styles>"),
			new RuntimeXamlLoaderDocument(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <StyleInclude Source='csres://Tests/Style.xaml'/>
    </Window.Styles>
    <Border Name='border'/>
</Window>")
		};

		using (StyledWindow())
		{
			var compiled = CornerstoneXamlIlRuntimeLoader.LoadGroup(documents);
			var window = CornerstoneTest.IsType<Window>(compiled[1]);
			var border = window.GetControl<Border>("border");
			var brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(border.Background);

			CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
		}
	}

	[PresentationTestMethod]
	public void StaticResourceFromApplicationCanBeAssignedToPropertyInUserControl()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			Application.Current!.Resources.Add("brush", new SolidColorBrush(0xff506070));

			var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border Name='border' Background='{StaticResource brush}'/>
</UserControl>";

			var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
			var border = userControl.GetControl<Border>("border");

			// We don't actually know where the global styles are until we attach the control
			// to a window, as Window has StylingParent set to Application.
			var window = new Window { Content = userControl };
			window.Show();

			var brush = CornerstoneTest.IsAssignableFrom<SolidColorBrush>(border.Background);
			CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
		}
	}

	[PresentationTestMethod]
	public void StaticResourceFromApplicationCanBeAssignedToPropertyInWindow()
	{
		using (StyledWindow())
		{
			Application.Current!.Resources.Add("brush", new SolidColorBrush(0xff506070));

			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border Name='border' Background='{StaticResource brush}'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var border = window.GetControl<Border>("border");

			var brush = CornerstoneTest.IsAssignableFrom<SolidColorBrush>(border.Background);
			CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
		}
	}

	[PresentationTestMethod]
	public void StaticResourceFromMergedDictionaryCanBeAssignedToProperty()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary>
                    <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
                </ResourceDictionary>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>

    <Border Name='border' Background='{StaticResource brush}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		var brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(border.Background);
		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
	}

	[PresentationTestMethod]
	public void StaticResourceFromMergedDictionaryInStyleCanBeAssignedToProperty()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Styles>
        <Style>
            <Style.Resources>
                <ResourceDictionary>
                    <ResourceDictionary.MergedDictionaries>
                        <ResourceDictionary>
                            <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
                        </ResourceDictionary>
                    </ResourceDictionary.MergedDictionaries>
                </ResourceDictionary>
            </Style.Resources>
        </Style>
    </UserControl.Styles>

    <Border Name='border' Background='{StaticResource brush}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		var brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(border.Background);
		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
	}

	[PresentationTestMethod]
	public void StaticResourceFromStyleCanBeAssignedToProperty()
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

    <Border Name='border' Background='{StaticResource brush}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		var brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(border.Background);
		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
	}

	[PresentationTestMethod]
	public void StaticResourceFromStyleCanBeAssignedToSetter()
	{
		using (StyledWindow())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <Style>
            <Style.Resources>
                <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
            </Style.Resources>
        </Style>
        <Style Selector='Button'>
            <Setter Property='Background' Value='{StaticResource brush}'/>
        </Style>
    </Window.Styles>
    <Button Name='button'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");
			var brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(button.Background);

			CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
		}
	}

	[PresentationTestMethod]
	public void StaticResourceIsCorrectlyChosenForDeferredContent()
	{
		using (StyledWindow())
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'>

  <Window.Resources>
    <Color x:Key='Color'>Purple</Color>
  </Window.Resources>

  <Border>
   <Border.Resources>
      <Color x:Key='Color'>Red</Color>
      <SolidColorBrush x:Key='Brush' Color='{StaticResource Color}' />
    </Border.Resources>
    <TextBlock Foreground='{StaticResource Brush}' />
  </Border>

</Window>");

			window.Show();

			var textBlock = window.GetVisualDescendants().OfType<TextBlock>().Single();

			CornerstoneTest.IsNotNull(textBlock);
			var brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(textBlock.Foreground);
			CornerstoneTest.AreEqual(Colors.Red, brush.Color);
		}
	}

	[PresentationTestMethod]
	public void StaticResourceIsCorrectlyChosenFromWithinDataTemplate()
	{
		// this tests if IAmbientProviders in DataTemplate contexts are in correct order
		// if they wouldn't be, Purple brush would be bound to
		using (StyledWindow())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'>
    <Window.Resources>
        <local:TestValueConverter x:Key='converter' Append='-bar'/>
        <SolidColorBrush x:Key='brush' Color='Purple'/>
        <DataTemplate x:Key='WhiteData'>
          <Border>
            <Border.Resources>
              <SolidColorBrush x:Key='brush' Color='White'/>
            </Border.Resources>
            <TextBlock Name='textBlock' Text='{Binding Color, Source={StaticResource brush}, Converter={StaticResource converter}}' Foreground='{StaticResource brush}' />
          </Border>
        </DataTemplate>
    </Window.Resources>

    <ContentPresenter Content='foo' ContentTemplate='{StaticResource WhiteData}'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);

			window.Show();

			var textBlock = window.GetVisualDescendants().OfType<TextBlock>().Single();

			CornerstoneTest.IsNotNull(textBlock);
			CornerstoneTest.AreEqual("White-bar", textBlock.Text);
		}
	}

	private static IDisposable StyledWindow(params (string, string)[] assets)
	{
		var services = TestServices.StyledWindow.With(
			theme: () => new Styles
			{
				WindowStyle()
			});

		return UnitTestApplication.Start(services);
	}

	private static Style WindowStyle()
	{
		return new Style(x => x.OfType<Window>())
		{
			Setters =
			{
				new Setter(
					Window.TemplateProperty,
					new FuncControlTemplate<Window>((x, scope) =>
						new ContentPresenter
						{
							Name = "PART_ContentPresenter",
							[!ContentPresenter.ContentProperty] = x[!Window.ContentProperty]
						}.RegisterInNameScope(scope)))
			}
		};
	}

	#endregion
}