#nullable enable

#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Resources;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Markup;
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
public class DynamicResourceExtensionTests : XamlTestBase
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

    <Border Name='border' Background='{DynamicResource color}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		var brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(border.Background);
		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
	}

	[PresentationTestMethod]
	public void CanDetachControlWithDynamicResourceControlThemeThatContainsDynamicResource()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    RequestedThemeVariant='Light'>
    <Window.Resources>
        <SolidColorBrush x:Key='Blue'>Blue</SolidColorBrush>
        <ControlTheme x:Key='MyTheme' TargetType='Button'>
            <Setter Property='Background' Value='{DynamicResource Blue}'/>
        </ControlTheme>
    </Window.Resources>

    <Button Theme='{DynamicResource MyTheme}'/>
</Window>";

		var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
		var target = CornerstoneTest.IsType<Button>(window.Content);

		window.Show();

		var brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(target.Background);
		CornerstoneTest.AreEqual(Colors.Blue, brush.Color);

		window.Content = null;
	}

	[PresentationTestMethod]
	public void ControlPropertyIsUpdatedWhenParentIsChanged()
	{
		using (StyledWindow())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
    </Window.Resources>

    <Border Name='border' Background='{DynamicResource brush}'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var border = window.GetControl<Border>("border");

			DelayedBinding.ApplyBindings(border);

			var brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(border.Background);
			CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());

			window.Content = null;

			CornerstoneTest.IsNull(border.Background);

			window.Content = border;

			brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(border.Background);
			CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
		}
	}

	[PresentationTestMethod]
	public void DynamicResourceCanBeAssignedToAttachedProperty()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <x:Int32 x:Key='col'>5</x:Int32>
    </UserControl.Resources>

    <Border Name='border' Grid.Column='{DynamicResource col}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		DelayedBinding.ApplyBindings(border);

		CornerstoneTest.AreEqual(5, Grid.GetColumn(border));
	}

	[PresentationTestMethod]
	public void DynamicResourceCanBeAssignedToItemTemplateProperty()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <DataTemplate x:Key='PurpleData'>
          <TextBlock Text='{Binding Name}' Background='Purple'/>
        </DataTemplate>
    </UserControl.Resources>

    <ListBox Name='listBox' ItemTemplate='{DynamicResource PurpleData}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var listBox = userControl.GetControl<ListBox>("listBox");

		DelayedBinding.ApplyBindings(listBox);

		CornerstoneTest.IsNotNull(listBox.ItemTemplate);
	}

	[PresentationTestMethod]
	public void DynamicResourceCanBeAssignedToProperty()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <SolidColorBrush x:Key='brush'>#ff506070</SolidColorBrush>
    </UserControl.Resources>

    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		DelayedBinding.ApplyBindings(border);

		var brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(border.Background);
		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
	}

	[PresentationTestMethod]
	public void DynamicResourceCanBeAssignedToPropertyInControlTemplateInStylesFile()
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
                <Border Name='border' Background='{DynamicResource brush}'/>
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
	public void DynamicResourceCanBeAssignedToResourceProperty()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <Color x:Key='color'>#ff506070</Color>
        <SolidColorBrush x:Key='brush' Color='{DynamicResource color}'/>
    </UserControl.Resources>

    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		DelayedBinding.ApplyBindings(border);

		var brush = CornerstoneTest.IsAssignableFrom<SolidColorBrush>(border.Background);
		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
	}

	[PresentationTestMethod]
	public void DynamicResourceCanBeAssignedToResourcePropertyInApplication()
	{
		var xaml = @"
<Application xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Application.Resources>
        <Color x:Key='color'>#ff506070</Color>
        <SolidColorBrush x:Key='brush' Color='{DynamicResource color}'/>
    </Application.Resources>
</Application>";

		var application = (Application) CornerstoneRuntimeXamlLoader.Load(xaml);
		var brush = CornerstoneTest.IsAssignableFrom<SolidColorBrush>(application.Resources["brush"]);

		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
	}

	[PresentationTestMethod]
	public void DynamicResourceCanBeAssignedToSetter()
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
            <Setter Property='Background' Value='{DynamicResource brush}'/>
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
	public void DynamicResourceCanBeAssignedToSetterInStylesFile()
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
        <Setter Property='Background' Value='{DynamicResource brush}'/>
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
	public void DynamicResourceCanBeFoundAcrossXamlStyleFiles()
	{
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Style1.xaml"), @"
<Style xmlns='https://github.com/BobbyCannon/Cornerstone'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <Style.Resources>
    <Color x:Key='Red'>Red</Color>
  </Style.Resources>
</Style>"),
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Style2.xaml"), @"
<Style xmlns='https://github.com/BobbyCannon/Cornerstone'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <Style.Resources>
    <SolidColorBrush x:Key='RedBrush' Color='{DynamicResource Red}'/>
  </Style.Resources>
</Style>"),
			new RuntimeXamlLoaderDocument(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <StyleInclude Source='csres://Tests/Style1.xaml'/>
        <StyleInclude Source='csres://Tests/Style2.xaml'/>
    </Window.Styles>
    <Border Name='border' Background='{DynamicResource RedBrush}'/>
</Window>")
		};

		using (StyledWindow())
		{
			var compiled = CornerstoneXamlIlRuntimeLoader.LoadGroup(documents);
			var window = CornerstoneTest.IsType<Window>(compiled[2]);
			var border = window.GetControl<Border>("border");
			var borderBrush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(border.Background);

			CornerstoneTest.IsNotNull(borderBrush);
			CornerstoneTest.AreEqual(0xffff0000, borderBrush.Color.ToUInt32());
		}
	}

	[PresentationTestMethod]
	public void DynamicResourceCanBeFoundInNestedStyleFile()
	{
		var documents = new[]
		{
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Style1.xaml"), @"
<Styles xmlns='https://github.com/BobbyCannon/Cornerstone'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <StyleInclude Source='csres://Tests/Style2.xaml'/>
</Styles>"),
			new RuntimeXamlLoaderDocument(new Uri("csres://Tests/Style2.xaml"), @"
<Style xmlns='https://github.com/BobbyCannon/Cornerstone'
       xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <Style.Resources>
    <Color x:Key='Red'>Red</Color>
    <SolidColorBrush x:Key='RedBrush' Color='{DynamicResource Red}'/>
  </Style.Resources>
</Style>"),
			new RuntimeXamlLoaderDocument(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Styles>
        <StyleInclude Source='csres://Tests/Style1.xaml'/>
    </Window.Styles>
    <Border Name='border' Background='{DynamicResource RedBrush}'/>
</Window>")
		};

		using (StyledWindow())
		{
			var compiled = CornerstoneXamlIlRuntimeLoader.LoadGroup(documents);
			var window = CornerstoneTest.IsType<Window>(compiled[2]);
			var border = window.GetControl<Border>("border");
			var borderBrush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(border.Background);

			CornerstoneTest.IsNotNull(borderBrush);
			CornerstoneTest.AreEqual(0xffff0000, borderBrush.Color.ToUInt32());
		}
	}

	[PresentationTestMethod]
	public void DynamicResourceFromApplicationCanBeAssignedToPropertyInUserControl()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			Application.Current!.Resources.Add("brush", new SolidColorBrush(0xff506070));

			var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border Name='border' Background='{DynamicResource brush}'/>
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
	public void DynamicResourceFromApplicationCanBeAssignedToPropertyInWindow()
	{
		using (StyledWindow())
		{
			Application.Current!.Resources.Add("brush", new SolidColorBrush(0xff506070));

			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border Name='border' Background='{DynamicResource brush}'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var border = window.GetControl<Border>("border");

			var brush = CornerstoneTest.IsAssignableFrom<SolidColorBrush>(border.Background);
			CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
		}
	}

	[PresentationTestMethod]
	public void DynamicResourceFromMergedDictionaryCanBeAssignedToProperty()
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

    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		DelayedBinding.ApplyBindings(border);

		var brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(border.Background);
		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
	}

	[PresentationTestMethod]
	public void DynamicResourceFromMergedDictionaryInStyleCanBeAssignedToProperty()
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

    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		DelayedBinding.ApplyBindings(border);

		var brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(border.Background);
		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
	}

	[PresentationTestMethod]
	public void DynamicResourceFromStyleCanBeAssignedToProperty()
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

    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		DelayedBinding.ApplyBindings(border);

		var brush = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(border.Background);
		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
	}

	[PresentationTestMethod]
	public void DynamicResourceFromStyleCanBeAssignedToSetter()
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
            <Setter Property='Background' Value='{DynamicResource brush}'/>
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
	public void DynamicResourceTracksAddedMergedResource()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>
    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		DelayedBinding.ApplyBindings(border);

		CornerstoneTest.IsNull(border.Background);

		((IResourceDictionary) userControl.Resources.MergedDictionaries[0]).Add("brush", new SolidColorBrush(0xff506070));

		var brush = CornerstoneTest.IsAssignableFrom<SolidColorBrush>(border.Background);
		CornerstoneTest.IsNotNull(brush);
		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
	}

	[PresentationTestMethod]
	public void DynamicResourceTracksAddedMergedResourceDictionary()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		DelayedBinding.ApplyBindings(border);

		CornerstoneTest.IsNull(border.Background);

		var dictionary = new ResourceDictionary
		{
			{ "brush", new SolidColorBrush(0xff506070) }
		};

		userControl.Resources.MergedDictionaries.Add(dictionary);

		var brush = CornerstoneTest.IsAssignableFrom<SolidColorBrush>(border.Background);
		CornerstoneTest.IsNotNull(brush);
		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
	}

	[PresentationTestMethod]
	public void DynamicResourceTracksAddedNestedStyleResource()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Styles>
        <Style>
        </Style>
    </UserControl.Styles>
    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		DelayedBinding.ApplyBindings(border);

		CornerstoneTest.IsNull(border.Background);

		((Style) userControl.Styles[0]).Resources.Add("brush", new SolidColorBrush(0xff506070));

		var brush = CornerstoneTest.IsAssignableFrom<SolidColorBrush>(border.Background);
		CornerstoneTest.IsNotNull(brush);
		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
	}

	[PresentationTestMethod]
	public void DynamicResourceTracksAddedResource()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		DelayedBinding.ApplyBindings(border);

		CornerstoneTest.IsNull(border.Background);

		userControl.Resources.Add("brush", new SolidColorBrush(0xff506070));

		var brush = CornerstoneTest.IsAssignableFrom<SolidColorBrush>(border.Background);
		CornerstoneTest.IsNotNull(brush);
		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
	}

	[PresentationTestMethod]
	public void DynamicResourceTracksAddedStyleMergedResourceDictionary()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Styles>
        <Style>
        </Style>
    </UserControl.Styles>
    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		DelayedBinding.ApplyBindings(border);

		CornerstoneTest.IsNull(border.Background);

		var dictionary = new ResourceDictionary
		{
			{ "brush", new SolidColorBrush(0xff506070) }
		};

		((Style) userControl.Styles[0]).Resources.MergedDictionaries.Add(dictionary);

		var brush = CornerstoneTest.IsAssignableFrom<SolidColorBrush>(border.Background);
		CornerstoneTest.IsNotNull(brush);
		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
	}

	[PresentationTestMethod]
	public void DynamicResourceTracksAddedStyleResource()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		DelayedBinding.ApplyBindings(border);

		CornerstoneTest.IsNull(border.Background);

		userControl.Styles.Resources.Add("brush", new SolidColorBrush(0xff506070));

		var brush = CornerstoneTest.IsAssignableFrom<SolidColorBrush>(border.Background);
		CornerstoneTest.IsNotNull(brush);
		CornerstoneTest.AreEqual(0xff506070, brush.Color.ToUInt32());
	}

	[PresentationTestMethod]
	public void HandlesClearingResourcesWithDynamicThemeInDynamicTemplate()
	{
		// Issue #14753
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);
		var xaml = """
					<Window xmlns="https://github.com/BobbyCannon/Cornerstone"
					        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
					    <Window.Resources>
					        <SolidColorBrush x:Key='Blue'>Blue</SolidColorBrush>
					        <ControlTheme x:Key="MyBorder" TargetType="Border">
					            <Setter Property="Background" Value="{DynamicResource Blue}"/>
					        </ControlTheme>
					        <ControlTheme x:Key="MyButton" TargetType="Button">
					            <Setter Property="Template">
					                <ControlTemplate>
					                    <Border Theme="{DynamicResource MyBorder}"/>
					                </ControlTemplate>
					            </Setter>
					        </ControlTheme>
					    </Window.Resources>
					    <Button Theme="{DynamicResource MyButton}" Background="{DynamicResource Blue}"/>
					</Window>
					""";

		var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
		window.Show();

		var button = CornerstoneTest.IsType<Button>(window.Content);
		var border = CornerstoneTest.IsType<Border>(button.GetVisualChildren().Single());
		var background = CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(border.Background);
		CornerstoneTest.AreEqual(Colors.Blue, background.Color);

		window.Resources.Clear();
	}

	[PresentationTestMethod]
	public void MergedDictionaryResourceWithDynamicResourceIsUpdatedWhenAddedToParent()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary>
                    <SolidColorBrush x:Key='brush' Color='{DynamicResource color}'/>
                </ResourceDictionary>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>

    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		DelayedBinding.ApplyBindings(border);

		var brush = CornerstoneTest.IsAssignableFrom<SolidColorBrush>(border.Background);
		CornerstoneTest.AreEqual(0u, brush.Color.ToUInt32());

		brush.GetObservable(SolidColorBrush.ColorProperty).Subscribe(_ => { });

		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window
			{
				Resources =
				{
					{ "color", Colors.Red }
				},
				Content = userControl
			};

			window.Show();

			CornerstoneTest.AreEqual(Colors.Red, brush.Color);
		}
	}

	[PresentationTestMethod]
	public void ResourceInNonActiveStyleIsNotResolved()
	{
		using var app = StyledWindow();

		var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions'>
    <Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <local:TrackingResourceProvider/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Window.Resources>

    <Window.Styles>
        <Style Selector='Border'>
            <Setter Property='Tag' Value='{DynamicResource foo}'/>
        </Style>
        <Style Selector='Border'>
            <Setter Property='Tag' Value='{DynamicResource bar}'/>
        </Style>
    </Window.Styles>

    <Border Name='border'/>
</Window>";

		var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = window.GetControl<Border>("border");

		CornerstoneTest.AreEqual("bar", border.Tag);

		var resourceProvider = (TrackingResourceProvider) window.Resources.MergedDictionaries[0];
		CornerstoneTest.Contains(resourceProvider.RequestedResources, "bar");
		CornerstoneTest.DoesNotContain(resourceProvider.RequestedResources, "foo");
	}

	[PresentationTestMethod]
	public void ResourceInNonMatchingStyleIsNotResolved()
	{
		using var app = StyledWindow();

		var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions'>
    <Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <local:TrackingResourceProvider/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Window.Resources>

    <Window.Styles>
        <Style Selector='Border.nomatch'>
            <Setter Property='Tag' Value='{DynamicResource foo}'/>
        </Style>
        <Style Selector='Border'>
            <Setter Property='Tag' Value='{DynamicResource bar}'/>
        </Style>
    </Window.Styles>

    <Border Name='border'/>
</Window>";

		var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = window.GetControl<Border>("border");

		CornerstoneTest.AreEqual("bar", border.Tag);

		var resourceProvider = (TrackingResourceProvider) window.Resources.MergedDictionaries[0];
		CornerstoneTest.Contains(resourceProvider.RequestedResources, "bar");
		CornerstoneTest.DoesNotContain(resourceProvider.RequestedResources, "foo");
	}

	[PresentationTestMethod]
	public void ResourceWithDynamicResourceIsUpdatedWhenAddedToParent()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Resources>
        <SolidColorBrush x:Key='brush' Color='{DynamicResource color}'/>
    </UserControl.Resources>

    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		DelayedBinding.ApplyBindings(border);

		var brush = CornerstoneTest.IsAssignableFrom<SolidColorBrush>(border.Background);
		CornerstoneTest.AreEqual(0u, brush.Color.ToUInt32());

		brush.GetObservable(SolidColorBrush.ColorProperty).Subscribe(_ => { });

		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window
			{
				Resources =
				{
					{ "color", Colors.Red }
				},
				Content = userControl
			};

			window.Show();

			CornerstoneTest.AreEqual(Colors.Red, brush.Color);
		}
	}

	[PresentationTestMethod]
	public void StyleResourceWithDynamicResourceIsUpdatedWhenAddedToParent()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <UserControl.Styles>
        <Style>
            <Style.Resources>
                <SolidColorBrush x:Key='brush' Color='{DynamicResource color}'/>
            </Style.Resources>
        </Style>
    </UserControl.Styles>

    <Border Name='border' Background='{DynamicResource brush}'/>
</UserControl>";

		var userControl = (UserControl) CornerstoneRuntimeXamlLoader.Load(xaml);
		var border = userControl.GetControl<Border>("border");

		DelayedBinding.ApplyBindings(border);

		var brush = CornerstoneTest.IsAssignableFrom<SolidColorBrush>(border.Background);
		CornerstoneTest.AreEqual(0u, brush.Color.ToUInt32());

		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = new Window
			{
				Resources =
				{
					{ "color", Colors.Red }
				},
				Content = userControl
			};

			window.Show();

			CornerstoneTest.AreEqual(Colors.Red, brush.Color);
		}
	}

	private IDisposable StyledWindow()
	{
		var services = TestServices.StyledWindow.With(
			theme: () => new Styles
			{
				WindowStyle()
			});

		return UnitTestApplication.Start(services);
	}

	private Style WindowStyle()
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

public class TrackingResourceProvider : ResourceProvider
{
	#region Properties

	public override bool HasResources => true;
	public List<object> RequestedResources { get; } = new();

	#endregion

	#region Methods

	public override bool TryGetResource(object key, ThemeVariant? themeVariant, out object value)
	{
		RequestedResources.Add(key);
		value = key;
		return true;
	}

	#endregion
}