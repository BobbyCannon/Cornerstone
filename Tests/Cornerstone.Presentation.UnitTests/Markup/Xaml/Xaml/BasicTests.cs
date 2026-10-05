#nullable enable

#region References

using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Documents;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Overlays;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Markup.Xaml.Templates;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Immutable;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class BasicTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AddChildChildIsSet()
	{
		var xaml = @"<ObjectWithAddChild  xmlns='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml'>Foo</ObjectWithAddChild>";

		var target = CornerstoneRuntimeXamlLoader.Parse<ObjectWithAddChild>(xaml);

		CornerstoneTest.IsNotNull(target);
		CornerstoneTest.AreEqual("Foo", target.Child);
	}

	[PresentationTestMethod]
	public void AddChildOfTChildIsSet()
	{
		var xaml = @"<ObjectWithAddChildOfT  xmlns='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml'>Foo</ObjectWithAddChildOfT>";

		var target = CornerstoneRuntimeXamlLoader.Parse<ObjectWithAddChildOfT>(xaml);

		CornerstoneTest.IsNotNull(target);
		CornerstoneTest.IsNull(target.Child);
		CornerstoneTest.AreEqual("Foo", target.Text);
	}

	[Ignore("Asserts the upstream Simple theme applying FontFamily/Foreground on EndInit; CornerstoneTheme does not.")]
	[PresentationTestMethod]
	public void AllPropertiesAreSetBeforeFinalEndInit()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <local:InitializationOrderTracker Width='100' Height='100'
        Tag='{Binding Height, RelativeSource={RelativeSource Self}}' />
</Window>";

			var window = CornerstoneRuntimeXamlLoader.Parse<Window>(xaml);
			var tracker = (InitializationOrderTracker) window.Content!;

			//ensure binding is set and operational first
			CornerstoneTest.AreEqual(100.0, tracker.Tag);

			// EndInit should be second-to-last operation, as last operation will be
			// caused by styling being applied on EndInit.
			CornerstoneTest.AreEqual("EndInit 0", tracker.Order[tracker.Order.Count - 3]);

			// Caused by styling.
			CornerstoneTest.AreEqual("Property FontFamily Changed", tracker.Order[tracker.Order.Count - 2]);
			CornerstoneTest.AreEqual("Property Foreground Changed", tracker.Order[tracker.Order.Count - 1]);
		}
	}

	[PresentationTestMethod]
	public void AttachedPropertyInPanelIsSet()
	{
		var xaml = @"
<Panel xmlns='https://github.com/BobbyCannon/Cornerstone'>
    <ToolTip.Tip>Foo</ToolTip.Tip>
</Panel>";

		var target = CornerstoneRuntimeXamlLoader.Parse<Panel>(xaml);

		CornerstoneTest.Empty(target.Children);

		CornerstoneTest.AreEqual("Foo", ToolTip.GetTip(target));
	}

	[PresentationTestMethod]
	public void AttachedPropertyIsSet()
	{
		var xaml =
			@"<ContentControl xmlns='https://github.com/BobbyCannon/Cornerstone' TextElement.FontSize='21'/>";

		var target = CornerstoneRuntimeXamlLoader.Parse<ContentControl>(xaml);

		CornerstoneTest.IsNotNull(target);
		CornerstoneTest.AreEqual(21.0, TextElement.GetFontSize(target));
	}

	[PresentationTestMethod]
	public void AttachedPropertyIsSetOnControlOutsideAvaloniaNamespace()
	{
		// Test for issue #1548
		var xaml =
			@"<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
    xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
  <local:TestControl Grid.Column='2' />
</UserControl>";

		var target = CornerstoneRuntimeXamlLoader.Parse<UserControl>(xaml);

		CornerstoneTest.AreEqual(2, Grid.GetColumn((TestControl) target.Content!));
	}

	[PresentationTestMethod]
	public void AttachedPropertySupportsBinding()
	{
		using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
		{
			var xaml =
				@"<Window xmlns='https://github.com/BobbyCannon/Cornerstone' TextElement.FontSize='{Binding}'/>";

			var target = CornerstoneRuntimeXamlLoader.Parse<ContentControl>(xaml);

			target.DataContext = 21.0;

			CornerstoneTest.AreEqual(21.0, TextElement.GetFontSize(target));
		}
	}

	[PresentationTestMethod]
	public void AttachedPropertyWithNamespaceIsSet()
	{
		var xaml =
			@"<ContentControl xmlns='https://github.com/BobbyCannon/Cornerstone' 
                    xmlns:test='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'
                    test:BasicTestsAttachedPropertyHolder.Foo='Bar'/>";

		var target = CornerstoneRuntimeXamlLoader.Parse<ContentControl>(xaml);

		CornerstoneTest.IsNotNull(target);
		CornerstoneTest.AreEqual("Bar", BasicTestsAttachedPropertyHolder.GetFoo(target));
	}

	[PresentationTestMethod]
	public void AvaloniaTypeConverterIsUsed()
	{
		var xaml = @"<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone' Background='White' />";

		var control = CornerstoneRuntimeXamlLoader.Parse<UserControl>(xaml);
		var brush = CornerstoneTest.IsType<ImmutableSolidColorBrush>(control.Background);
		CornerstoneTest.AreEqual(Colors.White, brush.Color);
	}

	[PresentationTestMethod]
	public void BeginInitMatchesEndInit()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <local:InitializationOrderTracker />
</Window>";

			var window = CornerstoneRuntimeXamlLoader.Parse<Window>(xaml);
			var tracker = (InitializationOrderTracker) window.Content!;

			CornerstoneTest.AreEqual(0, tracker.InitState);
		}
	}

	[PresentationTestMethod]
	public void BindingToListPresentationPropertyIsOperational()
	{
		using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'>
    <ListBox ItemsSource='{Binding Items}' SelectedItems='{Binding SelectedItems}'/>
</Window>";

			var window = CornerstoneRuntimeXamlLoader.Parse<Window>(xaml);
			var listBox = (ListBox) window.Content!;

			var vm = new SelectedItemsViewModel
			{
				Items = new[] { "foo", "bar", "baz" }
			};

			window.DataContext = vm;

			CornerstoneTest.AreEqual(vm.Items, listBox.ItemsSource);

			CornerstoneTest.AreEqual(vm.SelectedItems, listBox.SelectedItems);
		}
	}

	[PresentationTestMethod]
	public void CanSpecifyButtonClasses()
	{
		var xaml = "<Button xmlns='https://github.com/BobbyCannon/Cornerstone' Classes='foo bar'/>";
		var target = (Button) CornerstoneRuntimeXamlLoader.Load(xaml);

		CornerstoneTest.Contains(target.Classes, "foo");
		CornerstoneTest.Contains(target.Classes, "bar");
	}

	[PresentationTestMethod]
	public void CanSpecifyButtonClassesLongform()
	{
		var xaml = @"
<Button xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <Button.Classes>
    <x:String>foo</x:String>
    <x:String>bar</x:String>
  </Button.Classes>
</Button>";
		var target = (Button) CornerstoneRuntimeXamlLoader.Load(xaml);

		CornerstoneTest.Contains(target.Classes, "foo");
		CornerstoneTest.Contains(target.Classes, "bar");
	}

	[PresentationTestMethod]
	public void CanSpecifyFlyoutFlyoutPresenterClasses()
	{
		var xaml = "<Flyout xmlns='https://github.com/BobbyCannon/Cornerstone' FlyoutPresenterClasses='foo bar'/>";
		var target = (Flyout) CornerstoneRuntimeXamlLoader.Load(xaml);

		CornerstoneTest.AreEqual(new[] { "foo", "bar" }, target.FlyoutPresenterClasses);
	}

	[PresentationTestMethod]
	public void CollectionXamlBindingIsOperational()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformWrapper
					.With(windowingPlatform: new MockWindowingPlatform())))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'>
    <ItemsControl Name='itemsControl' ItemsSource='{Binding}'>
    </ItemsControl>
</Window>
";

			var target = CornerstoneRuntimeXamlLoader.Parse<Window>(xaml);

			CornerstoneTest.IsNotNull(target.Content);

			var itemsControl = target.GetControl<ItemsControl>("itemsControl");

			var items = new[] { "Foo", "Bar" };

			//DelayedBinding.ApplyBindings(itemsControl);

			target.DataContext = items;

			CornerstoneTest.AreEqual(items, itemsControl.ItemsSource);
		}
	}

	[PresentationTestMethod]
	public void ComplexStyleIsParsed()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Styles xmlns='https://github.com/BobbyCannon/Cornerstone'>
  <Style Selector='CheckBox'>
    <Setter Property='BorderBrush' Value='{DynamicResource ThemeBorderMidBrush}'/>
    <Setter Property='BorderThickness' Value='{DynamicResource ThemeBorderThickness}'/>
    <Setter Property='Template'>
      <ControlTemplate>
        <Grid ColumnDefinitions='Auto,*'>
          <Border Name='border'
                  BorderBrush='{TemplateBinding BorderBrush}'
                  BorderThickness='{TemplateBinding BorderThickness}'
                  Width='18'
                  Height='18'
                  VerticalAlignment='Center'>
            <Path Name='checkMark'
                  Fill='{StaticResource HighlightBrush}'
                  Width='11'
                  Height='10'
                  Stretch='Uniform'
                  HorizontalAlignment='Center'
                  VerticalAlignment='Center'
                  Data='M 1145.607177734375,430 C1145.607177734375,430 1141.449951171875,435.0772705078125 1141.449951171875,435.0772705078125 1141.449951171875,435.0772705078125 1139.232177734375,433.0999755859375 1139.232177734375,433.0999755859375 1139.232177734375,433.0999755859375 1138,434.5538330078125 1138,434.5538330078125 1138,434.5538330078125 1141.482177734375,438 1141.482177734375,438 1141.482177734375,438 1141.96875,437.9375 1141.96875,437.9375 1141.96875,437.9375 1147,431.34619140625 1147,431.34619140625 1147,431.34619140625 1145.607177734375,430 1145.607177734375,430 z'/>
          </Border>
          <ContentPresenter Name='PART_ContentPresenter'
                            Content='{TemplateBinding Content}'
                            ContentTemplate='{TemplateBinding ContentTemplate}'
                            Margin='4,0,0,0'
                            VerticalAlignment='Center'
                            Grid.Column='1'/>
        </Grid>
      </ControlTemplate>
    </Setter>
  </Style>
</Styles>
";
			var styles = CornerstoneRuntimeXamlLoader.Parse<Styles>(xaml);

			CornerstoneTest.Single(styles);

			var style = (Style) styles[0];

			var setters = style.Setters.Cast<Setter>().ToArray();

			CornerstoneTest.AreEqual(3, setters.Length);

			CornerstoneTest.AreEqual(CheckBox.BorderBrushProperty, setters[0].Property);
			CornerstoneTest.AreEqual(CheckBox.BorderThicknessProperty, setters[1].Property);
			CornerstoneTest.AreEqual(CheckBox.TemplateProperty, setters[2].Property);

			CornerstoneTest.IsType<ControlTemplate>(setters[2].Value);
		}
	}

	[PresentationTestMethod]
	public void ContentControlContentTemplateIsFunctional()
	{
		var xaml =
			@"<ContentControl xmlns='https://github.com/BobbyCannon/Cornerstone'>
    <ContentControl.ContentTemplate>
        <DataTemplate>
            <TextBlock Text='Foo' />
        </DataTemplate>
    </ContentControl.ContentTemplate>
</ContentControl>";

		var contentControl = CornerstoneRuntimeXamlLoader.Parse<ContentControl>(xaml);
		var target = contentControl.ContentTemplate;

		CornerstoneTest.IsNotNull(target);

		var txt = (TextBlock) target.Build(null)!;

		CornerstoneTest.AreEqual("Foo", txt.Text);
	}

	[PresentationTestMethod]
	public void ContentPresenterDefaultContentPropertyIsSet()
	{
		var xaml = @"<ContentPresenter xmlns='https://github.com/BobbyCannon/Cornerstone'>Foo</ContentPresenter>";

		var target = CornerstoneRuntimeXamlLoader.Parse<ContentPresenter>(xaml);

		CornerstoneTest.IsNotNull(target);
		CornerstoneTest.AreEqual("Foo", target.Content);
	}

	[PresentationTestMethod]
	public void ControlIsAddedToParentBeforeFinalEndInit()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <local:InitializationOrderTracker Width='100'/>
</Window>";

			var window = CornerstoneRuntimeXamlLoader.Parse<Window>(xaml);
			var tracker = (InitializationOrderTracker) window.Content!;

			var attached = tracker.Order.IndexOf("AttachedToLogicalTree");
			var endInit = tracker.Order.IndexOf("EndInit 0");

			CornerstoneTest.AreNotEqual(-1, attached);
			CornerstoneTest.AreNotEqual(-1, endInit);
			CornerstoneTest.IsTrue(attached < endInit);
		}
	}

	[PresentationTestMethod]
	public void ControlIsAddedToParentBeforePropertiesAreSet()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <local:InitializationOrderTracker Width='100'/>
</Window>";

			var window = CornerstoneRuntimeXamlLoader.Parse<Window>(xaml);
			var tracker = (InitializationOrderTracker) window.Content!;

			var attached = tracker.Order.IndexOf("AttachedToLogicalTree");
			var widthChanged = tracker.Order.IndexOf("Property Width Changed");

			CornerstoneTest.AreNotEqual(-1, attached);
			CornerstoneTest.AreNotEqual(-1, widthChanged);
			CornerstoneTest.IsTrue(attached < widthChanged);
		}
	}

	[PresentationTestMethod]
	public void ControlTemplateIsOperational()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformWrapper
					.With(windowingPlatform: new MockWindowingPlatform())))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Template>
        <ControlTemplate TargetType='Window'>
            <ContentPresenter Name='PART_ContentPresenter'
                        Content='{TemplateBinding Content}'/>
        </ControlTemplate>
    </Window.Template>
</Window>";

			var target = CornerstoneRuntimeXamlLoader.Parse<ContentControl>(xaml);

			CornerstoneTest.IsNotNull(target.Template);

			CornerstoneTest.IsNull(target.Presenter);

			target.ApplyTemplate();

			CornerstoneTest.IsNotNull(target.Presenter);

			target.Content = "Foo";

			CornerstoneTest.AreEqual("Foo", target.Presenter.Content);
		}
	}

	[PresentationTestMethod]
	public void DefaultContentPropertyIsSet()
	{
		var xaml = @"<ContentControl xmlns='https://github.com/BobbyCannon/Cornerstone'>Foo</ContentControl>";

		var target = CornerstoneRuntimeXamlLoader.Parse<ContentControl>(xaml);

		CornerstoneTest.IsNotNull(target);
		CornerstoneTest.AreEqual("Foo", target.Content);
	}

	[PresentationTestMethod]
	public void DeferredXamlLoaderShouldPreserveNamespacesContext()
	{
		var xaml =
			@"<ContentControl xmlns='https://github.com/BobbyCannon/Cornerstone'
            xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
            xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <ContentControl.ContentTemplate>
        <DataTemplate>
            <TextBlock  Tag='{x:Static local:NonControl.StringProperty}'/>
        </DataTemplate>
    </ContentControl.ContentTemplate>
</ContentControl>";

		var contentControl = CornerstoneRuntimeXamlLoader.Parse<ContentControl>(xaml);
		var template = contentControl.ContentTemplate;

		CornerstoneTest.IsNotNull(template);

		var txt = (TextBlock) template.Build(null)!;

		CornerstoneTest.AreEqual(NonControl.StringProperty, txt.Tag);
	}

	[PresentationTestMethod]
	public void DirectContentInItemsControlIsOperational()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'>
     <ItemsControl Name='items'>
         <ContentControl>Foo</ContentControl>
         <ContentControl>Bar</ContentControl>
      </ItemsControl>
</Window>";

			var control = CornerstoneRuntimeXamlLoader.Parse<Window>(xaml);

			var itemsControl = control.GetControl<ItemsControl>("items");

			CornerstoneTest.IsNotNull(itemsControl);

			var items = itemsControl.Items.Cast<ContentControl>().ToArray();

			CornerstoneTest.AreEqual("Foo", items[0].Content);
			CornerstoneTest.AreEqual("Bar", items[1].Content);
		}
	}

	[PresentationTestMethod]
	public void DoubleXamlBindingIsOperational()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformWrapper
					.With(windowingPlatform: new MockWindowingPlatform())))
		{
			var xaml =
				@"<Window xmlns='https://github.com/BobbyCannon/Cornerstone' Width='{Binding}'/>";

			var target = CornerstoneRuntimeXamlLoader.Parse<ContentControl>(xaml);

			CornerstoneTest.IsNull(target.Content);

			target.DataContext = 55.0;

			CornerstoneTest.AreEqual(55.0, target.Width);
		}
	}

	[PresentationTestMethod]
	public void ElementWhitespaceShouldBeTrimmed()
	{
		using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'>
    <TextBlock>
        Hello World!
    </TextBlock>
</Window>";

			var window = CornerstoneRuntimeXamlLoader.Parse<Window>(xaml);
			var textBlock = (TextBlock) window.Content!;

			CornerstoneTest.AreEqual("Hello World!", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void GridRowColDefinitionsAreBuilt()
	{
		var xaml = @"
<Grid xmlns='https://github.com/BobbyCannon/Cornerstone'>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width='100' />
        <ColumnDefinition Width='Auto' />
        <ColumnDefinition Width='*' />
        <ColumnDefinition Width='100*' />
    </Grid.ColumnDefinitions>
    <Grid.RowDefinitions>
        <RowDefinition Height='100' />
        <RowDefinition Height='Auto' />
        <RowDefinition Height='*' />
        <RowDefinition Height='100*' />
    </Grid.RowDefinitions>
</Grid>";

		var grid = CornerstoneRuntimeXamlLoader.Parse<Grid>(xaml);

		CornerstoneTest.AreEqual(4, grid.ColumnDefinitions.Count);
		CornerstoneTest.AreEqual(4, grid.RowDefinitions.Count);

		var expected1 = new GridLength(100);
		var expected2 = GridLength.Auto;
		var expected3 = new GridLength(1, GridUnitType.Star);
		var expected4 = new GridLength(100, GridUnitType.Star);

		CornerstoneTest.AreEqual(expected1, grid.ColumnDefinitions[0].Width);
		CornerstoneTest.AreEqual(expected2, grid.ColumnDefinitions[1].Width);
		CornerstoneTest.AreEqual(expected3, grid.ColumnDefinitions[2].Width);
		CornerstoneTest.AreEqual(expected4, grid.ColumnDefinitions[3].Width);

		CornerstoneTest.AreEqual(expected1, grid.RowDefinitions[0].Height);
		CornerstoneTest.AreEqual(expected2, grid.RowDefinitions[1].Height);
		CornerstoneTest.AreEqual(expected3, grid.RowDefinitions[2].Height);
		CornerstoneTest.AreEqual(expected4, grid.RowDefinitions[3].Height);
	}

	[PresentationTestMethod]
	public void GridRowColDefinitionsAreParsed()
	{
		var xaml = @"
<Grid xmlns='https://github.com/BobbyCannon/Cornerstone'
        ColumnDefinitions='100,Auto,*,100*'
        RowDefinitions='100,Auto,*,100*'>
</Grid>";

		var grid = CornerstoneRuntimeXamlLoader.Parse<Grid>(xaml);

		CornerstoneTest.AreEqual(4, grid.ColumnDefinitions.Count);
		CornerstoneTest.AreEqual(4, grid.RowDefinitions.Count);

		var expected1 = new GridLength(100);
		var expected2 = GridLength.Auto;
		var expected3 = new GridLength(1, GridUnitType.Star);
		var expected4 = new GridLength(100, GridUnitType.Star);

		CornerstoneTest.AreEqual(expected1, grid.ColumnDefinitions[0].Width);
		CornerstoneTest.AreEqual(expected2, grid.ColumnDefinitions[1].Width);
		CornerstoneTest.AreEqual(expected3, grid.ColumnDefinitions[2].Width);
		CornerstoneTest.AreEqual(expected4, grid.ColumnDefinitions[3].Width);

		CornerstoneTest.AreEqual(expected1, grid.RowDefinitions[0].Height);
		CornerstoneTest.AreEqual(expected2, grid.RowDefinitions[1].Height);
		CornerstoneTest.AreEqual(expected3, grid.RowDefinitions[2].Height);
		CornerstoneTest.AreEqual(expected4, grid.RowDefinitions[3].Height);
	}

	[PresentationTestMethod]
	public void GridRowColDefinitionsAreParsedSpaceDelimiter()
	{
		var xaml = @"
<Grid xmlns='https://github.com/BobbyCannon/Cornerstone'
        ColumnDefinitions='100 Auto * 100*'
        RowDefinitions='100 Auto * 100*'>
</Grid>";

		var grid = CornerstoneRuntimeXamlLoader.Parse<Grid>(xaml);

		CornerstoneTest.AreEqual(4, grid.ColumnDefinitions.Count);
		CornerstoneTest.AreEqual(4, grid.RowDefinitions.Count);

		var expected1 = new GridLength(100);
		var expected2 = GridLength.Auto;
		var expected3 = new GridLength(1, GridUnitType.Star);
		var expected4 = new GridLength(100, GridUnitType.Star);

		CornerstoneTest.AreEqual(expected1, grid.ColumnDefinitions[0].Width);
		CornerstoneTest.AreEqual(expected2, grid.ColumnDefinitions[1].Width);
		CornerstoneTest.AreEqual(expected3, grid.ColumnDefinitions[2].Width);
		CornerstoneTest.AreEqual(expected4, grid.ColumnDefinitions[3].Width);

		CornerstoneTest.AreEqual(expected1, grid.RowDefinitions[0].Height);
		CornerstoneTest.AreEqual(expected2, grid.RowDefinitions[1].Height);
		CornerstoneTest.AreEqual(expected3, grid.RowDefinitions[2].Height);
		CornerstoneTest.AreEqual(expected4, grid.RowDefinitions[3].Height);
	}

	[PresentationTestMethod]
	public void MultiXamlBindingIsParsed()
	{
		var xaml =
			@"<MultiBinding xmlns='https://github.com/BobbyCannon/Cornerstone' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    Converter ='{x:Static BoolConverters.And}'>
     <Binding Path='Foo' />
     <Binding Path='Bar' />
</MultiBinding>";

		var target = CornerstoneRuntimeXamlLoader.Parse<MultiBinding>(xaml);

		CornerstoneTest.AreEqual(2, target.Bindings.Count);

		CornerstoneTest.AreEqual(BoolConverters.And, target.Converter);

		var bindings = target.Bindings.Cast<ReflectionBinding>().ToArray();

		CornerstoneTest.AreEqual("Foo", bindings[0].Path);
		CornerstoneTest.AreEqual("Bar", bindings[1].Path);
	}

	[PresentationTestMethod]
	public void NamedControlIsAddedToNameScope()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Button Name='button'>Foo</Button>
</Window>";

			var window = CornerstoneRuntimeXamlLoader.Parse<Window>(xaml);
			var button = window.GetControl<Button>("button");

			CornerstoneTest.AreEqual("Foo", button.Content);
		}
	}

	[PresentationTestMethod]
	public void NamedControlIsAddedToNameScopeSimple()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'>
    <Button Name='button'>Foo</Button>
</UserControl>";

		var control = CornerstoneRuntimeXamlLoader.Parse<UserControl>(xaml);
		var button = control.GetControl<Button>("button");

		CornerstoneTest.AreEqual("Foo", button.Content);
	}

	[PresentationTestMethod]
	public void NamedxControlIsAddedToNameScopeSimple()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Button x:Name='button'>Foo</Button>
</UserControl>";

		var control = CornerstoneRuntimeXamlLoader.Parse<UserControl>(xaml);
		var button = control.GetControl<Button>("button");

		CornerstoneTest.AreEqual("Foo", button.Content);
	}

	[PresentationTestMethod]
	public void NonExistentPropertyThrows()
	{
		var xaml =
			@"<ContentControl xmlns='https://github.com/BobbyCannon/Cornerstone' DoesntExist='foo'/>";

		XamlTestHelpers.AssertThrowsXamlException(() => CornerstoneRuntimeXamlLoader.Parse<ContentControl>(xaml));
	}

	[PresentationTestMethod]
	public void PanelChildrenAreAdded()
	{
		var xaml = @"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'>
    <Panel Name='panel'>
        <ContentControl Name='Foo' />
        <ContentControl Name='Bar' />
    </Panel>
</UserControl>";

		var control = CornerstoneRuntimeXamlLoader.Parse<UserControl>(xaml);

		var panel = control.GetControl<Panel>("panel");

		CornerstoneTest.AreEqual(2, panel.Children.Count);

		var foo = control.GetControl<ContentControl>("Foo");
		var bar = control.GetControl<ContentControl>("Bar");

		CornerstoneTest.Contains(panel.Children, foo);
		CornerstoneTest.Contains(panel.Children, bar);
	}

	[PresentationTestMethod]
	public void ShouldParseAndPopulateTypeWithoutPublicCtor()
	{
		var xaml = @"<ObjectWithoutPublicCtor xmlns='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml' Test2='World' />";
		var target = (ObjectWithoutPublicCtor) CornerstoneRuntimeXamlLoader.Load(xaml, rootInstance: new ObjectWithoutPublicCtor("Hello"));

		CornerstoneTest.IsNotNull(target);
		CornerstoneTest.AreEqual("World", target.Test2);
		CornerstoneTest.AreEqual("Hello", target.Test1);
	}

	[PresentationTestMethod]
	public void ShouldParseTipWithComment()
	{
		var xaml = @"
                <TextBlock xmlns='https://github.com/BobbyCannon/Cornerstone' Text='TextBlock with tooltip'>
                    <ToolTip.Tip>
                        <!--Comment-->
                        <ToolTip>
                            Foo
                        </ToolTip>
                    </ToolTip.Tip>
                </TextBlock>";

		var textBlock = CornerstoneRuntimeXamlLoader.Parse<TextBlock>(xaml);

		var toolTip = ToolTip.GetTip(textBlock) as ToolTip;

		CornerstoneTest.IsNotNull(toolTip);

		CornerstoneTest.AreEqual("Foo", toolTip.Content);
	}

	[PresentationTestMethod]
	public void SimplePropertyIsSet()
	{
		var xaml = @"<ContentControl xmlns='https://github.com/BobbyCannon/Cornerstone' Content='Foo'/>";

		var target = CornerstoneRuntimeXamlLoader.Parse<ContentControl>(xaml);

		CornerstoneTest.IsNotNull(target);
		CornerstoneTest.AreEqual("Foo", target.Content);
	}

	[PresentationTestMethod]
	public void SimpleStyleIsParsed()
	{
		var xaml = @"
<Styles xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style Selector='TextBlock'>
        <Setter Property='Background' Value='White'/>
        <Setter Property='Width' Value='100'/>
    </Style>
</Styles>";

		var styles = CornerstoneRuntimeXamlLoader.Parse<Styles>(xaml);

		CornerstoneTest.Single(styles);

		var style = (Style) styles[0];

		var setters = style.Setters.Cast<Setter>().ToArray();

		CornerstoneTest.AreEqual(2, setters.Length);

		CornerstoneTest.AreEqual(TextBlock.BackgroundProperty, setters[0].Property);
		CornerstoneTest.AreEqual(Brushes.White.Color, ((ISolidColorBrush) setters[0].Value!).Color);

		CornerstoneTest.AreEqual(TextBlock.WidthProperty, setters[1].Property);
		CornerstoneTest.AreEqual(100.0, setters[1].Value);
	}

	[PresentationTestMethod]
	public void SimpleXamlBindingIsOperational()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformWrapper
					.With(windowingPlatform: new MockWindowingPlatform())))
		{
			var xaml =
				@"<Window xmlns='https://github.com/BobbyCannon/Cornerstone' Content='{Binding}'/>";

			var target = CornerstoneRuntimeXamlLoader.Parse<ContentControl>(xaml);

			CornerstoneTest.IsNull(target.Content);

			target.DataContext = "Foo";

			CornerstoneTest.AreEqual("Foo", target.Content);
		}
	}

	[PresentationTestMethod]
	public void SliderPropertiesCanBeSetInAnyOrder()
	{
		using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'>
    <Slider Width='400' Value='500' Minimum='0' Maximum='1000'/>
</Window>";

			var window = CornerstoneRuntimeXamlLoader.Parse<Window>(xaml);
			var slider = (Slider) window.Content!;

			CornerstoneTest.AreEqual(0, slider.Minimum);
			CornerstoneTest.AreEqual(1000, slider.Maximum);
			CornerstoneTest.AreEqual(500, slider.Value);
		}
	}

	[PresentationTestMethod]
	public void StandardTypeConverterIsUsed()
	{
		var xaml = @"<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone' Width='200.5' />";

		var control = CornerstoneRuntimeXamlLoader.Parse<UserControl>(xaml);
		CornerstoneTest.AreEqual(200.5, control.Width);
	}

	[PresentationTestMethod]
	public void StyleControlTemplateIsBuilt()
	{
		var xaml = @"
<Style xmlns='https://github.com/BobbyCannon/Cornerstone' Selector='ContentControl'>
  <Setter Property='Template'>
     <ControlTemplate>
        <ContentPresenter Name='PART_ContentPresenter'
                       Content='{TemplateBinding Content}'
                       ContentTemplate='{TemplateBinding ContentTemplate}' />
      </ControlTemplate>
  </Setter>
</Style> ";

		var style = CornerstoneRuntimeXamlLoader.Parse<Style>(xaml);

		CornerstoneTest.Single(style.Setters);

		var setter = (Setter) style.Setters.First();

		CornerstoneTest.AreEqual(ContentControl.TemplateProperty, setter.Property);

		CornerstoneTest.IsType<ControlTemplate>(setter.Value);

		var template = (ControlTemplate) setter.Value;

		var control = new ContentControl();

		var result = (ContentPresenter) template.Build(control)!.Result;

		CornerstoneTest.IsNotNull(result);
	}

	[PresentationTestMethod]
	public void StyleResourcesAreBuilt()
	{
		var xaml = @"
<Style xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:sys='clr-namespace:System;assembly=netstandard'>
    <Style.Resources>
        <SolidColorBrush x:Key='Brush'>White</SolidColorBrush>
        <sys:Double x:Key='Double'>10</sys:Double>
    </Style.Resources>
</Style>";

		var style = CornerstoneRuntimeXamlLoader.Parse<Style>(xaml);

		CornerstoneTest.IsTrue(style.Resources.Count > 0);

		style.TryGetResource("Brush", null, out var brush);

		CornerstoneTest.IsNotNull(brush);
		CornerstoneTest.IsAssignableFrom<ISolidColorBrush>(brush);
		CornerstoneTest.AreEqual(Colors.White, ((ISolidColorBrush) brush).Color);

		style.TryGetResource("Double", null, out var d);

		CornerstoneTest.AreEqual(10.0, d);
	}

	[PresentationTestMethod]
	public void StyleSetterWithAttachedPropertyIsParsed()
	{
		var xaml = @"
<Styles xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Style Selector='ContentControl'>
        <Setter Property='TextBlock.FontSize' Value='21'/>
    </Style>
</Styles>";

		var styles = CornerstoneRuntimeXamlLoader.Parse<Styles>(xaml);

		CornerstoneTest.Single(styles);

		var style = (Style) styles[0];

		var setters = style.Setters.Cast<Setter>().ToArray();

		CornerstoneTest.Single(setters);

		CornerstoneTest.AreEqual(TextBlock.FontSizeProperty, setters[0].Property);
		CornerstoneTest.AreEqual(21.0, setters[0].Value);
	}

	[PresentationTestMethod]
	public void TryingToBindItemsControlItemsThrows()
	{
		var xaml = "<ItemsControl xmlns='https://github.com/BobbyCannon/Cornerstone' Items='{Binding}'/>";

		CornerstoneTest.Throws<XmlException>(() => CornerstoneRuntimeXamlLoader.Load(xaml));
	}

	#endregion

	#region Classes

	private class SelectedItemsViewModel : INotifyPropertyChanged
	{
		#region Fields

		private IList _selectedItems = new PresentationList<string>();

		#endregion

		#region Properties

		public string[]? Items { get; set; }

		public IList SelectedItems
		{
			get => _selectedItems;
			set
			{
				_selectedItems = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItems)));
			}
		}

		#endregion

		#region Events

		public event PropertyChangedEventHandler? PropertyChanged;

		#endregion
	}

	#endregion
}

public class ObjectWithAddChild : IAddChild
{
	#region Properties

	public object? Child { get; set; }

	#endregion

	#region Methods

	void IAddChild.AddChild(object child)
	{
		Child = child;
	}

	#endregion
}

public class ObjectWithoutPublicCtor
{
	#region Constructors

	public ObjectWithoutPublicCtor(string param)
	{
		Test1 = param;
	}

	#endregion

	#region Properties

	public string? Test1 { get; set; }

	public string? Test2 { get; set; }

	#endregion
}

public class ObjectWithAddChildOfT : IAddChild, IAddChild<string>
{
	#region Properties

	public object? Child { get; set; }
	public string? Text { get; set; }

	#endregion

	#region Methods

	void IAddChild.AddChild(object child)
	{
		Child = child;
	}

	void IAddChild<string>.AddChild(string child)
	{
		Text = child;
	}

	#endregion
}

public class BasicTestsAttachedPropertyHolder
{
	#region Fields

	public static PresentationProperty<string?> FooProperty =
		PresentationProperty.RegisterAttached<BasicTestsAttachedPropertyHolder, PresentationObject, string?>("Foo");

	#endregion

	#region Methods

	public static string? GetFoo(PresentationObject target)
	{
		return (string?) target.GetValue(FooProperty);
	}

	public static void SetFoo(PresentationObject target, string? value)
	{
		target.SetValue(FooProperty, value);
	}

	#endregion
}