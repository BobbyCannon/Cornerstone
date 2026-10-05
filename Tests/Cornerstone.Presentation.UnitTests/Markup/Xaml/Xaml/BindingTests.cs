#nullable enable

#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive.Subjects;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;

[TestClass]
public class BindingTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void BindingClassesWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			// Note, this test also checks `Classes` reordering, so it should be kept AFTER the last single class
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Button Name='button' Classes.MyClass='{Binding Foo}' Classes.MySecondClass='True' Classes='foo bar'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");

			button.DataContext = new { Foo = true };
			window.ApplyTemplate();

			CornerstoneTest.IsTrue(button.Classes.Contains("MyClass"));
			CornerstoneTest.IsTrue(button.Classes.Contains("MySecondClass"));
			CornerstoneTest.IsTrue(button.Classes.Contains("foo"));
			CornerstoneTest.IsTrue(button.Classes.Contains("bar"));

			button.DataContext = new { Foo = false };

			CornerstoneTest.IsFalse(button.Classes.Contains("MyClass"));
			CornerstoneTest.IsTrue(button.Classes.Contains("MySecondClass"));
			CornerstoneTest.IsTrue(button.Classes.Contains("foo"));
			CornerstoneTest.IsTrue(button.Classes.Contains("bar"));
		}
	}

	[PresentationTestMethod]
	public void BindingDataContextToInheritedDataContextWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border DataContext='{Binding Foo}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var border = (Border) window.Content!;

			window.DataContext = new { Foo = "foo" };
			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();

			CornerstoneTest.AreEqual("foo", border.DataContext);
		}
	}

	[PresentationTestMethod]
	public void BindingOneWayToSourceWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        ShowInTaskbar='{Binding ShowInTaskbar, Mode=OneWayToSource}'>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var viewModel = new WindowViewModel();

			window.DataContext = viewModel;
			window.ApplyTemplate();

			CornerstoneTest.IsTrue(window.ShowInTaskbar);
			CornerstoneTest.IsTrue(viewModel.ShowInTaskbar);
		}
	}

	[PresentationTestMethod]
	public void BindingToAddOwneredAttachedPropertyWorks()
	{
		using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <local:TestControl Double='{Binding}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var testControl = (TestControl) window.Content!;

			window.DataContext = 5.6;
			window.ApplyTemplate();

			CornerstoneTest.AreEqual(5.6, testControl.Double);
		}
	}

	[PresentationTestMethod]
	public void BindingToAttachedPropertyInStyleWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Window.Styles>
        <Style Selector='TextBlock'>
            <Setter Property='local:TestControl.Double' Value='{Binding}'/>
        </Style>
    </Window.Styles>
    <TextBlock/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = (TextBlock) window.Content!;

			window.DataContext = 5.6;
			window.ApplyTemplate();

			CornerstoneTest.AreEqual(5.6, AttachedPropertyOwner.GetDouble(textBlock));
		}
	}

	[PresentationTestMethod]
	public void BindingToAttachedPropertyUsingAddOwneredTypeWorks()
	{
		using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <TextBlock local:TestControl.Double='{Binding}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = (TextBlock) window.Content!;

			window.DataContext = 5.6;
			window.ApplyTemplate();

			CornerstoneTest.AreEqual(5.6, AttachedPropertyOwner.GetDouble(textBlock));
		}
	}

	[PresentationTestMethod]
	public void BindingToDataContextWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Button Name='button' Content='{Binding Foo}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");

			button.DataContext = new { Foo = "foo" };
			window.ApplyTemplate();

			CornerstoneTest.AreEqual("foo", button.Content);
		}
	}

	[PresentationTestMethod]
	public void BindingToNamespacedAttachedPropertyWorks()
	{
		using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <TextBlock local:AttachedPropertyOwner.Double='{Binding}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = (TextBlock) window.Content!;

			window.DataContext = 5.6;
			window.ApplyTemplate();

			CornerstoneTest.AreEqual(5.6, AttachedPropertyOwner.GetDouble(textBlock));
		}
	}

	[PresentationTestMethod]
	public void BindingToSelfInStyleWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
       
    <Window.Styles>
        <Style Selector='Button'>
            <Setter Property='IsVisible' Value='{Binding $self.IsEnabled}' />
        </Style>
    </Window.Styles>

    <Button Name='button' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");

			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();

			CornerstoneTest.IsTrue(button.IsVisible);

			button.IsEnabled = false;

			CornerstoneTest.IsFalse(button.IsVisible);
		}
	}

	[PresentationTestMethod]
	public void BindingToSelfWorks()
	{
		using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock Name='textblock' Text='{Binding Tag, RelativeSource={RelativeSource Self}}'/>
</Window>";

			var window = CornerstoneRuntimeXamlLoader.Parse<ContentControl>(xaml);
			var textBlock = (TextBlock) window.Content!;

			textBlock.Tag = "foo";

			CornerstoneTest.AreEqual("foo", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	[DataRow(@"Hello \{0\}")]
	[DataRow(@"'Hello {0}'")]
	[DataRow(@"Hello {0}")]
	public void BindingToTextBlockTextWithStringConverterWorks(string fmt)
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <TextBlock Name='textBlock' Text=""{Binding Foo, StringFormat=" + fmt + @"}""/> 
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			textBlock.DataContext = new { Foo = "world" };
			window.ApplyTemplate();

			CornerstoneTest.AreEqual("Hello world", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void BindingToWindowWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        Title='{Binding Foo}'>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);

			window.DataContext = new { Foo = "foo" };
			window.ApplyTemplate();

			CornerstoneTest.AreEqual("foo", window.Title);
		}
	}

	[PresentationTestMethod]
	public void CanBindControlToNonControl()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Button Name='button' Content='Foo'>
        <Button.Tag>
            <local:NonControl Control='{Binding #button}'/>
        </Button.Tag>
    </Button>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");

			CornerstoneTest.Same(button, ((NonControl) button.Tag!).Control);
		}
	}

	[PresentationTestMethod]
	public void CanBindToDataContextOfAnchorOnNonControl()
	{
		using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Button Name='button'>
        <Button.Tag>
            <local:NonControl String='{Binding Foo}'/>
        </Button.Tag>
    </Button>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");

			button.DataContext = new { Foo = "foo" };

			CornerstoneTest.AreEqual("foo", ((NonControl) button.Tag!).String);
		}
	}

	[PresentationTestMethod]
	public void ConverterCultureCanBeSpecifiedByIetfLanguageTag()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
  <TextBlock Name='textBlock' Text='{Binding Greeting1, Converter={x:Static local:BindingTests+CultureAppender.Instance}, ConverterCulture=ar-SA}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = CornerstoneTest.IsType<TextBlock>(window.Content);

			window.DataContext = new WindowViewModel();
			window.ApplyTemplate();

			CornerstoneTest.AreEqual("Hello+ar-SA", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	[TestData(nameof(NegationData))]
	public void DoubleNegatingObjectReturnsCorrectValue(object value, bool? negated)
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        Tag='{Binding !!Object}'>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var viewModel = new WindowViewModel { Object = value };

			window.DataContext = viewModel;
			window.ApplyTemplate();

			var expected = negated.HasValue ? !negated : null;
			CornerstoneTest.AreEqual(expected, window.Tag);
		}
	}

	[PresentationTestMethod]
	[TestData(nameof(NegationData))]
	public void DoubleNegatingObjectReturnsCorrectValueWhenBoundToBool(object value, bool? negated)
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        IsVisible='{Binding !!Object}'>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var viewModel = new WindowViewModel { Object = value };

			window.DataContext = viewModel;
			window.ApplyTemplate();

			var expected = negated.HasValue ? !negated : false;
			CornerstoneTest.AreEqual(expected, window.IsVisible);
		}
	}

	[PresentationTestMethod]
	public void LongformBindingToSelfWorks()
	{
		using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock Name='textblock' Tag='foo'>
        <TextBlock.Text>
            <Binding RelativeSource='{RelativeSource Self}' Path='Tag'/>
        </TextBlock.Text>
    </TextBlock>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = (TextBlock) window.Content!;

			window.ApplyTemplate();

			CornerstoneTest.AreEqual("foo", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void LonghandBindingToDataContextWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Button Name='button'>
        <Button.Content>
            <Binding Path='Foo'/>
        </Button.Content>
    </Button>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");

			button.DataContext = new { Foo = "foo" };
			window.ApplyTemplate();

			CornerstoneTest.AreEqual("foo", button.Content);
		}
	}

	[PresentationTestMethod]
	[DataRow("{}{0} {1}!")]
	[DataRow(@"\{0\} \{1\}!")]
	public void MultiBindingToTextBlockTextWithStringConverterWorks(string fmt)
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <TextBlock Name='textBlock'>
        <TextBlock.Text>
            <MultiBinding StringFormat='" + fmt + @"'>
                <Binding Path='Greeting1'/>
                <Binding Path='Greeting2'/>
            </MultiBinding>
        </TextBlock.Text>
    </TextBlock> 
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			textBlock.DataContext = new WindowViewModel();
			window.ApplyTemplate();

			CornerstoneTest.AreEqual("Hello World!", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	[TestData(nameof(NegationData))]
	public void NegatingObjectReturnsCorrectValue(object value, bool? expected)
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        Tag='{Binding !Object}'>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var viewModel = new WindowViewModel { Object = value };

			window.DataContext = viewModel;
			window.ApplyTemplate();

			CornerstoneTest.AreEqual(expected, window.Tag);
		}
	}

	[PresentationTestMethod]
	[TestData(nameof(NegationData))]
	public void NegatingObjectReturnsCorrectValueWhenBoundToBool(object value, bool? expected)
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        IsVisible='{Binding !Object}'>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var viewModel = new WindowViewModel { Object = value };

			window.DataContext = viewModel;
			window.ApplyTemplate();

			CornerstoneTest.AreEqual(expected ?? false, window.IsVisible);
		}
	}

	public static IEnumerable<object?[]> NegationData()
	{
		yield return [true, false];
		yield return [false, true];
		yield return [null, true];
		yield return [new object(), null];
		yield return ["foo", null];
		yield return ["true", false];
		yield return ["false", true];
		yield return [0, true];
		yield return [1, false];
		yield return [2, false];
		yield return [-1, false];
		yield return [0.0, true];
		yield return [1.0, false];
		yield return [2.0, false];
		yield return [-1.0, false];
		yield return [double.NaN, false];
		yield return [double.PositiveInfinity, false];
		yield return [double.NegativeInfinity, false];
	}

	[PresentationTestMethod]
	public void StreamBindingToObservableWorks()
	{
		using (UnitTestApplication.Start(TestServices.MockWindowingPlatform))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock Name='textblock' Text='{Binding Observable^}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = (TextBlock) window.Content!;
			var observable = new BehaviorSubject<string>("foo");

			window.DataContext = new { Observable = observable };
			window.ApplyTemplate();

			CornerstoneTest.AreEqual("foo", textBlock.Text);
			observable.OnNext("bar");
			CornerstoneTest.AreEqual("bar", textBlock.Text);
		}
	}

	#endregion

	#region Classes

	public class CultureAppender : IValueConverter
	{
		#region Properties

		public static CultureAppender Instance { get; } = new();

		#endregion

		#region Methods

		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return $"{value}+{culture}";
		}

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}

	private class WindowViewModel
	{
		#region Properties

		public string? Greeting1 { get; set; } = "Hello";
		public string? Greeting2 { get; set; } = "World";
		public object? Object { get; set; }
		public bool ShowInTaskbar { get; set; }

		#endregion
	}

	#endregion
}