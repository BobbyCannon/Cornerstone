#nullable enable

#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml;

[TestClass]
public class XamlIlTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AttachedPropertiesFromStaticTypesShouldWorkInStyleSettersBug2561()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var parsed = (Window) CornerstoneRuntimeXamlLoader.Parse(@"
<Window
  xmlns='https://github.com/BobbyCannon/Cornerstone'
  xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml;assembly=Cornerstone.Presentation.UnitTests'
>
  <Window.Styles>
    <Style Selector='TextBox'>
      <Setter Property='local:XamlIlBugTestsStaticClassWithAttachedProperty.TestInt' Value='100'/>
    </Style>
  </Window.Styles>
  <TextBox/>

</Window>
");
			var tb = (TextBox) parsed.Content!;
			parsed.Show();
			tb.ApplyTemplate();
			CornerstoneTest.AreEqual(100, XamlIlBugTestsStaticClassWithAttachedProperty.GetTestInt(tb));
		}
	}

	[PresentationTestMethod]
	public void CompiledBindingShouldResolveNamedRootDataContextInItemTemplate()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var parsed = (ListBox) CornerstoneRuntimeXamlLoader.Parse(@"
<ListBox Name='ListBoxRoot' ItemsSource='{CompiledBinding Items}'
    xmlns='https://github.com/BobbyCannon/Cornerstone'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml;assembly=Cornerstone.Presentation.UnitTests'
    x:DataType='local:CompiledBindingRootMock'>
    <ListBox.ItemTemplate>
        <DataTemplate x:DataType='local:CompiledBindingItemMock'>
            <TextBlock Text='{CompiledBinding #ListBoxRoot.DataContext.RootProperty}' />
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>");
			CornerstoneTest.IsNotNull(parsed.ItemTemplate);
		}
	}

	[PresentationTestMethod]
	public void CompiledBindingShouldResolveRootCommandFromNestedItemTemplateNamescope()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var parsed = (ListBox) CornerstoneRuntimeXamlLoader.Parse(@"
<ListBox Name='ListBoxRoot' ItemsSource='{CompiledBinding Items}'
    xmlns='https://github.com/BobbyCannon/Cornerstone'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
    xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml;assembly=Cornerstone.Presentation.UnitTests'
    x:DataType='local:CompiledBindingRootMock'>
    <ListBox.ItemTemplate>
        <DataTemplate x:DataType='local:CompiledBindingItemMock'>
            <ListBox ItemsSource='{CompiledBinding InnerItems}'>
                <ListBox.ItemTemplate>
                    <DataTemplate x:DataType='local:CompiledBindingItemMock'>
                        <TextBlock Text='{CompiledBinding #ListBoxRoot.DataContext.RootProperty}' />
                    </DataTemplate>
                </ListBox.ItemTemplate>
            </ListBox>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>");
			CornerstoneTest.IsNotNull(parsed.ItemTemplate);
		}
	}

	[PresentationTestMethod]
	public void ControlThemeParserThrowsForDuplicateSetter()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:u='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml'>
    <Window.Resources>
        <ControlTheme x:Key='MyTheme' TargetType='u:TestTemplatedControl'>
            <Setter Property='Width' Value='100'/>
            <Setter Property='Height' Value='20'/>
            <Setter Property='Height' Value='30'/>
        </ControlTheme>
    </Window.Resources>

    <u:TestTemplatedControl Theme='{StaticResource MyTheme}'/>
</Window>";
		var diagnostics = new List<RuntimeXamlDiagnostic>();

		// We still have a runtime check in the StyleInstance class, but in this test we only care about compile warnings.
		Assert.Throws<InvalidOperationException>(() => CornerstoneRuntimeXamlLoader.Load(new RuntimeXamlLoaderDocument(xaml), new RuntimeXamlLoaderConfiguration
		{
			LocalAssembly = typeof(XamlIlTests).Assembly,
			DiagnosticHandler = diagnostic =>
			{
				diagnostics.Add(diagnostic);
				return diagnostic.Severity;
			}
		}));
		var warning = CornerstoneTest.Single(diagnostics);
		CornerstoneTest.AreEqual(RuntimeXamlDiagnosticSeverity.Warning, warning.Severity);
		CornerstoneTest.StartsWith(warning.Title, "Duplicate setter encountered for property 'Height'");
	}

	[Ignore("Needs compiled CornerstoneXaml for x:Class pages; test project cannot compile XAML until PresentationObject SetValue matches WellKnownTypes.")]
	[PresentationTestMethod]
	public void CustomPropertiesShouldWorkWithXClass()
	{
		var precompiled = new XamlIlClassWithCustomProperty();
		CornerstoneTest.AreEqual("123", precompiled.Test);
		var loaded = (XamlIlClassWithCustomProperty) CornerstoneRuntimeXamlLoader.Parse(@"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Cornerstone.Presentation.UnitTests.Markup.Xaml.XamlIlClassWithCustomProperty'
             Test='321'>

</UserControl>");
		CornerstoneTest.AreEqual("321", loaded.Test);
	}

	[PresentationTestMethod]
	public void DataContextTypeResolution()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var parsed = CornerstoneRuntimeXamlLoader.Parse<UserControl>(@"
<UserControl 
    xmlns='https://github.com/BobbyCannon/Cornerstone'
    xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml;assembly=Cornerstone.Presentation.UnitTests'
    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:DataType='local:XamlIlBugTestsDataContext' />");
		}
	}

	[PresentationTestMethod]
	public void DataTemplatesShouldResolveNamedControlsFromParentScope()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var parsed = (Window) CornerstoneRuntimeXamlLoader.Parse(@"
<Window
  xmlns='https://github.com/BobbyCannon/Cornerstone'
  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
>
  <StackPanel>
    <StackPanel.DataTemplates>
      <DataTemplate DataType='{x:Type x:String}'>
       <TextBlock Classes='target' Text='{Binding #txt.Text}'/>
      </DataTemplate>
    </StackPanel.DataTemplates>
    <TextBlock Text='Test' Name='txt'/>
    <ContentControl Content='tst'/>
  </StackPanel>
</Window>
");
			parsed.DataContext = new List<string> { "Test" };
			parsed.Show();
			parsed.ApplyTemplate();
			var cc = (ContentControl) ((StackPanel) parsed.Content!).Children.Last();
			cc.ApplyTemplate();
			var templated = cc.GetVisualDescendants().OfType<TextBlock>()
				.First(x => x.Classes.Contains("target"));
			CornerstoneTest.AreEqual("Test", templated.Text);
		}
	}

	[PresentationTestMethod]
	public void EventHandlersShouldWorkForTemplates()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var w = new XamlIlBugTestsEventHandlerCodeBehind();
			w.ApplyTemplate();
			w.Show();

			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			var itemsPresenter = ((ItemsControl) w.Content!).GetVisualChildren().FirstOrDefault();
			CornerstoneTest.IsNotNull(itemsPresenter);

			var item = itemsPresenter
				.GetVisualChildren().First()
				.GetVisualChildren().First()
				.GetVisualChildren().First();

			((Control) item).DataContext = "test";
			CornerstoneTest.AreEqual("test", w.SavedContext);
		}
	}

	[PresentationTestMethod]
	public void ItemContainerInsideOfDataTemplatesShouldBeWarned()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var xaml = new RuntimeXamlLoaderDocument(@"
<TabControl xmlns='https://github.com/BobbyCannon/Cornerstone'
         xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TabControl.DataTemplates>
        <DataTemplate x:DataType='x:Object'>
            <TabItem />
        </DataTemplate>
    </TabControl.DataTemplates>
</TabControl>");
		var diagnostics = new List<RuntimeXamlDiagnostic>();

		// We still have a runtime check in the StyleInstance class, but in this test we only care about compile warnings.
		var tabControl = (TabControl) CornerstoneRuntimeXamlLoader.Load(xaml, new RuntimeXamlLoaderConfiguration
		{
			DiagnosticHandler = diagnostic =>
			{
				diagnostics.Add(diagnostic);
				return diagnostic.Severity;
			}
		});

		// ItemTemplate should still work as before, creating whatever object user put inside
		CornerstoneTest.IsType<TabItem>(tabControl.DataTemplates[0]!.Build(null));

		// But invalid usage should be warned:
		var warning = CornerstoneTest.Single(diagnostics);
		CornerstoneTest.AreEqual(RuntimeXamlDiagnosticSeverity.Warning, warning.Severity);
		CornerstoneTest.AreEqual("CSDC2208", warning.Id);
	}

	[PresentationTestMethod]
	public void ItemContainerInsideOfItemTemplateShouldBeWarned()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var xaml = new RuntimeXamlLoaderDocument(@"
<ListBox xmlns='https://github.com/BobbyCannon/Cornerstone'
         xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <ListBox.ItemTemplate>
        <DataTemplate>
            <ListBoxItem />
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>");
		var diagnostics = new List<RuntimeXamlDiagnostic>();

		// We still have a runtime check in the StyleInstance class, but in this test we only care about compile warnings.
		var listBox = (ListBox) CornerstoneRuntimeXamlLoader.Load(xaml, new RuntimeXamlLoaderConfiguration
		{
			DiagnosticHandler = diagnostic =>
			{
				diagnostics.Add(diagnostic);
				return diagnostic.Severity;
			}
		});

		// ItemTemplate should still work as before, creating whatever object user put inside
		CornerstoneTest.IsType<ListBoxItem>(listBox.ItemTemplate!.Build(null));

		// But invalid usage should be warned:
		var warning = CornerstoneTest.Single(diagnostics);
		CornerstoneTest.AreEqual(RuntimeXamlDiagnosticSeverity.Warning, warning.Severity);
		CornerstoneTest.AreEqual("CSDC2208", warning.Id);
	}

	[Ignore("Needs compiled CornerstoneXaml for x:Class pages; test project cannot compile XAML until PresentationObject SetValue matches WellKnownTypes.")]
	[PresentationTestMethod]
	public void ParserShouldOverridePrecompiledXaml()
	{
		var precompiled = new XamlIlClassWithPrecompiledXaml();
		CornerstoneTest.AreEqual(Brushes.Red, precompiled.Background);
		CornerstoneTest.AreEqual(1, precompiled.Opacity);
		var loaded = (XamlIlClassWithPrecompiledXaml) CornerstoneRuntimeXamlLoader.Parse(@"
<UserControl xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             x:Class='Cornerstone.Presentation.UnitTests.Markup.Xaml.XamlIlClassWithPrecompiledXaml'
             Opacity='0'>
    
</UserControl>");
		CornerstoneTest.AreEqual(loaded.Opacity, 0);
		CornerstoneTest.IsNull(loaded.Background);
	}

	[PresentationTestMethod]
	public void ProvideValueTargetShouldProvideClrPropertyInfo()
	{
		var parsed = CornerstoneRuntimeXamlLoader.Parse<XamlIlClassWithClrPropertyWithValue>(@"
<XamlIlClassWithClrPropertyWithValue 
    xmlns='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml'
    Count='{XamlIlCheckClrPropertyInfo ExpectedPropertyName=Count}'
/>", typeof(XamlIlClassWithClrPropertyWithValue).Assembly);
		CornerstoneTest.AreEqual(6, parsed.Count);
	}

	[PresentationTestMethod]
	public void RelativeSourceTemplatedParentWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			CornerstoneRuntimeXamlLoader.Load(@"
<Application
  xmlns='https://github.com/BobbyCannon/Cornerstone'
  xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml;assembly=Cornerstone.Presentation.UnitTests'
>
<Application.Styles>
    <Style Selector='Button'>
      <Setter Property='Template'>
        <ControlTemplate>
          <Grid><Grid><Grid>
            <Canvas>
              <Canvas.Background>
                <SolidColorBrush>
                  <SolidColorBrush.Color>
                    <MultiBinding>
                      <MultiBinding.Converter>
                          <local:XamlIlBugTestsBrushToColorConverter/>
                      </MultiBinding.Converter>
                      <Binding Path='Background' RelativeSource='{RelativeSource TemplatedParent}'/>
                      <Binding Path='Background' RelativeSource='{RelativeSource TemplatedParent}'/>
                      <Binding Path='Background' RelativeSource='{RelativeSource TemplatedParent}'/>
                    </MultiBinding>
                  </SolidColorBrush.Color>
                </SolidColorBrush>
              </Canvas.Background>
            </Canvas>
          </Grid></Grid></Grid>
        </ControlTemplate>
      </Setter>
    </Style>
  </Application.Styles>
</Application>",
				null, Application.Current);
			var parsed = (Window) CornerstoneRuntimeXamlLoader.Parse(@"
<Window
  xmlns='https://github.com/BobbyCannon/Cornerstone'
  xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml;assembly=Cornerstone.Presentation.UnitTests'
>
  
  <Button Background='Red' />

</Window>
");
			var btn = (Button) parsed.Content!;
			btn.ApplyTemplate();
			var canvas = (Canvas) btn.GetVisualChildren().First()
				.VisualChildren.First()
				.VisualChildren.First()
				.VisualChildren.First();
			CornerstoneTest.AreEqual(Brushes.Red.Color, ((ISolidColorBrush) canvas.Background!).Color);
		}
	}

	[PresentationTestMethod]
	public void RuntimeLoaderShouldPassParentsFromServiceProvider()
	{
		var sp = new TestServiceProvider
		{
			ParentsStack = new List<object>
			{
				new UserControl { Resources = { ["Resource1"] = new SolidColorBrush(Colors.Blue) } }
			}
		};
		var document = new RuntimeXamlLoaderDocument(@"
<Button xmlns='https://github.com/BobbyCannon/Cornerstone' Background='{StaticResource Resource1}' />")
		{
			ServiceProvider = sp
		};

		var parsed = (Button) CornerstoneRuntimeXamlLoader.Load(document);
		CornerstoneTest.AreEqual(Colors.Blue, ((ISolidColorBrush) parsed.Background!).Color);
	}

	[PresentationTestMethod]
	public void ShouldWorkWithBaseProperty()
	{
		var parsed = (ListBox) CornerstoneRuntimeXamlLoader.Load(@"
<ListBox
  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
  xmlns='https://github.com/BobbyCannon/Cornerstone'
  xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml;assembly=Cornerstone.Presentation.UnitTests'
>
    <ItemsControl.ItemTemplate>
      <DataTemplate>
        <ContentControl Content='{Binding}' />
      </DataTemplate>
    </ItemsControl.ItemTemplate>
</ListBox>");

		CornerstoneTest.IsNotNull(parsed.ItemTemplate);
	}

	[PresentationTestMethod]
	public void StyleParserThrowsForDuplicateSetter()
	{
		using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

		var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Window.Styles>
        <Style Selector='TextBlock'>
            <Setter Property='Width' Value='100'/>
            <Setter Property='Height' Value='20'/>
            <Setter Property='Height' Value='30'/>
        </Style>
    </Window.Styles>
    <TextBlock/>
</Window>";
		var diagnostics = new List<RuntimeXamlDiagnostic>();

		// We still have a runtime check in the StyleInstance class, but in this test we only care about compile warnings.
		Assert.Throws<InvalidOperationException>(() => CornerstoneRuntimeXamlLoader.Load(new RuntimeXamlLoaderDocument(xaml), new RuntimeXamlLoaderConfiguration
		{
			LocalAssembly = typeof(XamlIlTests).Assembly,
			DiagnosticHandler = diagnostic =>
			{
				diagnostics.Add(diagnostic);
				return diagnostic.Severity;
			}
		}));
		var warning = CornerstoneTest.Single(diagnostics);
		CornerstoneTest.AreEqual(RuntimeXamlDiagnosticSeverity.Warning, warning.Severity);
		CornerstoneTest.StartsWith(warning.Title, "Duplicate setter encountered for property 'Height'");
	}

	[PresentationTestMethod]
	public void TransitionsShouldBeProperlyParsed()
	{
		var parsed = (Grid) CornerstoneRuntimeXamlLoader.Parse(@"
<Grid xmlns='https://github.com/BobbyCannon/Cornerstone' >
  <Grid.Transitions>
    <Transitions>
      <DoubleTransition Property='Opacity'
        Easing='CircularEaseIn'
        Duration='0:0:0.5' />
    </Transitions>
  </Grid.Transitions>
</Grid>");
		CornerstoneTest.IsNotNull(parsed.Transitions);
		CornerstoneTest.AreEqual(1, parsed.Transitions.Count);
		CornerstoneTest.AreEqual(Visual.OpacityProperty, parsed.Transitions[0].Property);
	}

	[PresentationTestMethod]
	public void TypeConvertersShouldWorkWhenSpecifiedWithAttributesOnAvaloniaProperties()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var parsed = (XamlIlClassWithTypeConverterOnPresentationProperty)
				CornerstoneRuntimeXamlLoader.Parse(@"
<XamlIlClassWithTypeConverterOnPresentationProperty
    xmlns='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml;assembly=Cornerstone.Presentation.UnitTests' 
    MyProp='a,b,c'/>",
					typeof(XamlIlBugTestsEventHandlerCodeBehind).Assembly);

			CornerstoneTest.AreEqual((IEnumerable<string>) ["a", "b", "c"], parsed.MyProp.Select(x => x.Value));
		}
	}

	#endregion
}

public class XamlIlBugTestsEventHandlerCodeBehind : Window
{
	#region Fields

	public object? SavedContext;

	#endregion

	#region Constructors

	public XamlIlBugTestsEventHandlerCodeBehind()
	{
		CornerstoneRuntimeXamlLoader.Load(@"
<Window x:Class='Cornerstone.Presentation.UnitTests.Markup.Xaml.XamlIlBugTestsEventHandlerCodeBehind'
  xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
  xmlns='https://github.com/BobbyCannon/Cornerstone'
  xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml;assembly=Cornerstone.Presentation.UnitTests'
>
  <ItemsControl>
    <ItemsControl.ItemTemplate>
      <DataTemplate>
        <Button DataContextChanged='HandleDataContextChanged' Content='{Binding .}' />
      </DataTemplate>
    </ItemsControl.ItemTemplate>
  </ItemsControl>
</Window>
", typeof(XamlIlBugTestsEventHandlerCodeBehind).Assembly, this);
		((ItemsControl) Content!).ItemsSource = new[] { "123" };
	}

	#endregion

	#region Methods

	public void HandleDataContextChanged(object? sender, EventArgs args)
	{
		SavedContext = ((Control) sender!).DataContext;
	}

	#endregion
}

public class XamlIlClassWithCustomProperty : UserControl
{
	#region Constructors

	public XamlIlClassWithCustomProperty()
	{
		CornerstoneXamlLoader.Load(this);
	}

	#endregion

	#region Properties

	public string? Test { get; set; }

	#endregion
}

public class XamlIlBugTestsBrushToColorConverter : IMultiValueConverter
{
	#region Methods

	public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
	{
		return (values[0] as ISolidColorBrush)?.Color;
	}

	#endregion
}

public class XamlIlBugTestsDataContext : INotifyPropertyChanged
{
	#region Methods

	protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	#endregion

	#region Events

	public event PropertyChangedEventHandler? PropertyChanged;

	#endregion
}

public class XamlIlClassWithPrecompiledXaml : UserControl
{
}

public static class XamlIlBugTestsStaticClassWithAttachedProperty
{
	#region Fields

	public static readonly PresentationProperty<int> TestIntProperty = PresentationProperty
		.RegisterAttached<Control, int>("TestInt", typeof(XamlIlBugTestsStaticClassWithAttachedProperty));

	#endregion

	#region Methods

	public static int GetTestInt(Control control)
	{
		return (int) control.GetValue(TestIntProperty)!;
	}

	public static void SetTestInt(Control control, int value)
	{
		control.SetValue(TestIntProperty, value);
	}

	#endregion
}

public class XamlIlCheckClrPropertyInfoExtension
{
	#region Properties

	public string? ExpectedPropertyName { get; set; }

	#endregion

	#region Methods

	public object ProvideValue(IServiceProvider prov)
	{
		var pvt = prov.GetRequiredService<IProvideValueTarget>();
		var info = (ClrPropertyInfo) pvt.TargetProperty;
		var v = (int) info.Get(pvt.TargetObject)!;
		return v + 1;
	}

	#endregion
}

public class XamlIlClassWithClrPropertyWithValue
{
	#region Properties

	public int Count { get; set; } = 5;

	#endregion
}

public class XamlIlClassWithTypeConverterOnPresentationProperty : PresentationObject
{
	#region Fields

	public static readonly StyledProperty<IEnumerable<MyType>> MyPropProperty = PresentationProperty.Register<XamlIlClassWithTypeConverterOnPresentationProperty, IEnumerable<MyType>>(
		"MyProp");

	#endregion

	#region Properties

	[TypeConverter(typeof(MyTypeConverter))]
	public IEnumerable<MyType> MyProp
	{
		get => GetValue(MyPropProperty);
		set => SetValue(MyPropProperty, value);
	}

	#endregion

	#region Classes

	public class MyType(string value)
	{
		#region Properties

		public string Value => value;

		#endregion
	}

	public class MyTypeConverter : TypeConverter
	{
		#region Methods

		public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
		{
			return sourceType == typeof(string);
		}

		public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
		{
			if (value is string s)
			{
				return s.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(x => new MyType(x.Trim()));
			}
			return base.ConvertFrom(context, culture, value);
		}

		#endregion
	}

	#endregion
}

public class CompiledBindingRootMock : UserControl
{
	#region Properties

	public string Greeting => "Hello";
	public IReadOnlyList<CompiledBindingItemMock> Items { get; } = [new() { Name = "Outer", InnerItems = [new() { Name = "Inner" }] }];
	public string RootProperty => "RootValue";

	#endregion
}

public class CompiledBindingItemMock
{
	#region Properties

	public IReadOnlyList<CompiledBindingItemMock> InnerItems { get; set; } = Array.Empty<CompiledBindingItemMock>();
	public string Name { get; set; } = string.Empty;

	#endregion
}