#nullable enable

#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Layout;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Markup;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Markup.Xaml.Templates;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Immutable;
using Cornerstone.Presentation.Metadata;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;

[TestClass]
public class CompiledBindingExtensionTests : XamlTestBase
{
	#region Constructors

	static CompiledBindingExtensionTests()
	{
		RuntimeHelpers.RunClassConstructor(typeof(RelativeSource).TypeHandle);
	}

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void BindingMethodToCommandInStyleWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:MethodAsCommandDataContext'>
    <Window.Styles>
        <Style Selector='Button'>
            <Setter Property='Command' Value='{CompiledBinding Method}'/>
        </Style>
    </Window.Styles>
    <Button Name='button'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");
			var vm = new MethodAsCommandDataContext();

			button.DataContext = vm;
			window.ApplyTemplate();

			CornerstoneTest.IsNotNull(button.Command);
			PerformClick(button);
			CornerstoneTest.AreEqual("Called", vm.Value);
		}
	}

	[PresentationTestMethod]
	public void BindingMethodToCommandWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:MethodAsCommandDataContext'>
    <Button Name='button' Command='{CompiledBinding Method}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");
			var vm = new MethodAsCommandDataContext();

			button.DataContext = vm;
			window.ApplyTemplate();

			CornerstoneTest.IsNotNull(button.Command);
			PerformClick(button);
			CornerstoneTest.AreEqual("Called", vm.Value);
		}
	}

	[PresentationTestMethod]
	public void BindingMethodToTextBlockTextWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:MethodAsCommandDataContext'>
    <TextBlock Name='textBlock' Text='{CompiledBinding Method}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");
			var vm = new MethodAsCommandDataContext();

			textBlock.DataContext = vm;
			window.ApplyTemplate();

			CornerstoneTest.IsNotNull(textBlock.Text);
		}
	}

	[PresentationTestMethod]
	[DataRow(null, "Not called")]
	[DataRow("A", "Do A")]
	public void BindingMethodWithParameterToCommandCanExecute(object? commandParameter, string result)
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:MethodAsCommandDataContext'>
    <Button Name='button' Command='{CompiledBinding Do}' CommandParameter='{CompiledBinding Parameter, Mode=OneTime}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");
			var vm = new MethodAsCommandDataContext
			{
				Parameter = commandParameter
			};

			button.DataContext = vm;
			window.ApplyTemplate();

			CornerstoneTest.IsNotNull(button.Command);
			PerformClick(button);
			CornerstoneTest.AreEqual(vm.Value, result);
		}
	}

	[PresentationTestMethod]
	public void BindingMethodWithParameterToCommandCanExecuteDependsOn()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:MethodAsCommandDataContext'>
    <Button Name='button' Command='{CompiledBinding Do}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");
			var vm = new MethodAsCommandDataContext
			{
				Parameter = null
			};

			button.DataContext = vm;
			window.ApplyTemplate();

			CornerstoneTest.IsNotNull(button.Command);

			CornerstoneTest.AreEqual(button.IsEffectivelyEnabled, false);

			vm.Parameter = true;
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.AreEqual(button.IsEffectivelyEnabled, true);
		}
	}

	[PresentationTestMethod]
	public void BindingMethodWithParameterToCommandFailsWithMultipleSingleParameterOverloadsWithoutObject()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var exception = CornerstoneTest.Throws<XmlException>(() => (Window) CornerstoneRuntimeXamlLoader.Load(
			"""
			<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
			        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
			        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
			        x:DataType='local:MethodAsCommandDataContext'>
			    <Button Name='button' Command='{CompiledBinding MethodWithOverloads2}' CommandParameter="foo" />
			</Window>
			"""));

		CornerstoneTest.StartsWith(exception.Message, "Unable to resolve method of name 'MethodWithOverloads2' on type 'Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions.MethodAsCommandDataContext'. " +
			"Found 2 overloads accepting one parameter: 'System.Int32', 'System.String'. " +
			"Expected either a single overload with one parameter, or an overload accepting System.Object.");
	}

	[PresentationTestMethod]
	public void BindingMethodWithParameterToCommandFailsWithoutValidOverloads()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var exception = CornerstoneTest.Throws<XmlException>(() => (Window) CornerstoneRuntimeXamlLoader.Load(
			"""
			<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
			        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
			        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
			        x:DataType='local:MethodAsCommandDataContext'>
			    <Button Name='button' Command='{CompiledBinding MethodWithOverloads4}' CommandParameter="foo" />
			</Window>
			"""));

		CornerstoneTest.StartsWith(exception.Message, "Unable to resolve method of name 'MethodWithOverloads4' on type 'Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions.MethodAsCommandDataContext'. " +
			"Found 2 overloads accepting more than one parameter. " +
			"Expected a method with zero or one parameter. ");
	}

	[PresentationTestMethod]
	public void BindingMethodWithParameterToCommandPrefersObjectOverload()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var window = (Window) CornerstoneRuntimeXamlLoader.Load(
			"""
			<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
			        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
			        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
			        x:DataType='local:MethodAsCommandDataContext'>
			    <Button Name='button' Command='{CompiledBinding MethodWithOverloads}' CommandParameter="foo" />
			</Window>
			""");
		var button = window.GetControl<Button>("button");
		var vm = new MethodAsCommandDataContext();

		button.DataContext = vm;
		window.ApplyTemplate();

		CornerstoneTest.IsNotNull(button.Command);
		PerformClick(button);
		CornerstoneTest.AreEqual("Called MethodWithOverloads with Object foo", vm.Value);
	}

	[PresentationTestMethod]
	public void BindingMethodWithParameterToCommandUsesParameterlessOverloadWhenNoOverloadsWithParameterExist()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var window = (Window) CornerstoneRuntimeXamlLoader.Load(
			"""
			<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
			        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
			        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
			        x:DataType='local:MethodAsCommandDataContext'>
			    <Button Name='button' Command='{CompiledBinding MethodWithOverloads3}' CommandParameter="foo" />
			</Window>
			""");
		var button = window.GetControl<Button>("button");
		var vm = new MethodAsCommandDataContext();

		button.DataContext = vm;
		window.ApplyTemplate();

		CornerstoneTest.IsNotNull(button.Command);
		PerformClick(button);
		CornerstoneTest.AreEqual("Called MethodWithOverloads3 without parameter", vm.Value);
	}

	[PresentationTestMethod]
	[DataRow("ObjectMethod", "<x:String>hello</x:String>", "Called ObjectMethod with hello")]
	[DataRow("StringMethod", "<x:String>hello</x:String>", "Called StringMethod with hello")]
	[DataRow("StringMethod", "<x:Null />", "Called StringMethod with ")]
	[DataRow("Int32Method", "<x:Int32>42</x:Int32>", "Called Int32Method with 42")]
	[DataRow("VirtualObjectMethod", "<x:String>hello</x:String>", "Called VirtualObjectMethod with hello")]
	[DataRow("VirtualStringMethod", "<x:String>hello</x:String>", "Called VirtualStringMethod with hello")]
	[DataRow("VirtualStringMethod", "<x:Null />", "Called VirtualStringMethod with ")]
	[DataRow("VirtualInt32Method", "<x:Int32>42</x:Int32>", "Called VirtualInt32Method with 42")]
	[DataRow("MethodWithNewSlot", "<x:Int32>42</x:Int32>", "Called MethodWithNewSlot with 42")]
	public void BindingMethodWithParameterToCommandUsesSingleParameterOverload(
		string methodName,
		string xamlParameter,
		string expected)
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var window = (Window) CornerstoneRuntimeXamlLoader.Load(
			$$"""
			<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
			        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
			        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
			        x:DataType='local:MethodAsCommandDataContext'>
			    <Button Name='button' Command='{CompiledBinding {{methodName}}}'>
			      <Button.CommandParameter>
			        {{xamlParameter}}
			      </Button.CommandParameter>
			    </Button>
			</Window>
			""");
		var button = window.GetControl<Button>("button");
		var vm = new MethodAsCommandDataContext();

		button.DataContext = vm;
		window.ApplyTemplate();

		CornerstoneTest.IsNotNull(button.Command);
		PerformClick(button);
		CornerstoneTest.AreEqual(expected, vm.Value);
	}

	[PresentationTestMethod]
	[DataRow("Int32Method", "<x:String>hello</x:String>", typeof(InvalidCastException))]
	[DataRow("Int32Method", "<x:Null />", typeof(NullReferenceException))]
	[DataRow("StringMethod", "<x:Int32>42</x:Int32>", typeof(InvalidCastException))]
	public void BindingMethodWithParameterToCommandWithSingleParameterOverloadThrowsAtRuntimeIfMismatchedTypes(
		string methodName,
		string xamlParameter,
		Type exceptionType)
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var window = (Window) CornerstoneRuntimeXamlLoader.Load(
			$$"""
			<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
			        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
			        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
			        x:DataType='local:MethodAsCommandDataContext'>
			    <Button Name='button' Command='{CompiledBinding {{methodName}}}'>
			      <Button.CommandParameter>
			        {{xamlParameter}}
			      </Button.CommandParameter>
			    </Button>
			</Window>
			""");
		var button = window.GetControl<Button>("button");
		var vm = new MethodAsCommandDataContext();

		button.DataContext = vm;
		window.ApplyTemplate();

		CornerstoneTest.IsNotNull(button.Command);
		CornerstoneTest.Throws(exceptionType, () => PerformClick(button));
	}

	[PresentationTestMethod]
	public void BindsToRelativeSourceSelf()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Name='textBlock' Text='{CompiledBinding RelativeSource={RelativeSource Self}}' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();

			CornerstoneTest.AreEqual("Cornerstone.Presentation.Controls.TextBlock", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void BindsToRelativeSourceSelfInMultiBinding(bool compileBindings)
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = $@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        x:CompileBindings='{compileBindings}'>
  <StackPanel>
    <TextBlock Name='textBlock'>
      <TextBlock.Text>
        <MultiBinding StringFormat=""{{}} $self = {{0}}, $parent = {{1}}"">
          <Binding Path=""$self.FontStyle""/>
          <Binding Path=""$parent.Orientation""/>
        </MultiBinding>
      </TextBlock.Text>
    </TextBlock>
  </StackPanel>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext();
			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(" $self = Normal, $parent = Vertical", textBlock.GetValue(TextBlock.TextProperty));
		}
	}

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void BindsToRelativeSourceSelfInMultiBindingInStyle(bool compileBindings)
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = $@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        x:CompileBindings='{compileBindings}'>
  <Window.Styles>
    <Style Selector='TextBlock'>
        <Setter Property='Text'>
          <MultiBinding StringFormat=""{{}} $self = {{0}}"">
            <Binding Path=""$self.FontStyle""/>
          </MultiBinding>
        </Setter>
    </Style>
  </Window.Styles>
  <StackPanel>
    <TextBlock Name='textBlock'/>
  </StackPanel>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext();
			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(" $self = Normal", textBlock.GetValue(TextBlock.TextProperty));
		}
	}

	[PresentationTestMethod]
	public void BindsToSelf()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Name='textBlock' Text='{CompiledBinding $self}' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();

			CornerstoneTest.AreEqual("Cornerstone.Presentation.Controls.TextBlock", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void BindsToSelfInStyle()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
       
    <Window.Styles>
        <Style Selector='Button'>
            <Setter Property='IsVisible' Value='{CompiledBinding $self.IsEnabled}' />
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
	public void BindsToSelfWithoutDataType()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'>
    <TextBlock Name='textBlock' Text='{CompiledBinding $self.Name}' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();

			CornerstoneTest.AreEqual(textBlock.Name, textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void BindsToSource()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'
        Title='test'>
    <TextBlock Text='{CompiledBinding Length, Source=Test}' x:Name='text'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var target = window.GetControl<TextBlock>("text");

			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();
			target.ApplyTemplate();

			CornerstoneTest.AreEqual("Test".Length.ToString(), target.Text);
		}
	}

	[PresentationTestMethod]
	public void BindsToSourceStaticResource()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions'
             x:CompileBindings='True'>
    <Window.Resources>
        <local:TestDataContext x:Key='dataKey' StringProperty='foobar'/>
    </Window.Resources>
    <TextBlock Name='textBlock' Text='{Binding StringProperty, Source={StaticResource dataKey}}'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			CornerstoneTest.AreEqual("foobar", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void BindsToSourceStaticResource1()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions'
             x:CompileBindings='True'>
    <Window.Resources>
        <local:TestDataContext x:Key='dataKey' StringProperty='foobar'/>
        <x:String x:Key='otherObjectKey'>test</x:String>
    </Window.Resources>
    <TextBlock Name='textBlock' Text='{Binding StringProperty, Source={StaticResource dataKey}}'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			CornerstoneTest.AreEqual("foobar", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void BindsToSourceStaticResourceInResourceDictionary()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions'
             x:DataType='local:TestDataContext' x:CompileBindings='True'>
    <Window.Resources>
        <ResourceDictionary>
            <local:TestDataContext x:Key='dataKey' StringProperty='foobar'/>
        </ResourceDictionary>
    </Window.Resources>
    <TextBlock Name='textBlock' Text='{Binding StringProperty, Source={StaticResource dataKey}}'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			CornerstoneTest.AreEqual("foobar", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void BindsToSourceStaticResourceInResourceDictionary1()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions'
             x:DataType='local:TestDataContext' x:CompileBindings='True'>
    <Window.Resources>
        <ResourceDictionary>
            <local:TestDataContext x:Key='dataKey' StringProperty='foobar'/>
            <x:String x:Key='otherObjectKey'>test</x:String>
        </ResourceDictionary>
    </Window.Resources>
    <TextBlock Name='textBlock' Text='{Binding StringProperty, Source={StaticResource dataKey}}'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			CornerstoneTest.AreEqual("foobar", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void BindsToSourcexStatic()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
             xmlns:local='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions'
             x:CompileBindings='True'>
    <ContentControl Name='contentControl' Content='{Binding Color, Source={x:Static Brushes.Red}}'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var contentControl = window.GetControl<ContentControl>("contentControl");

			CornerstoneTest.AreEqual(Brushes.Red.Color, contentControl.Content);
		}
	}

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
              <ColumnDefinition Width='{CompiledBinding RelativeSource={RelativeSource TemplatedParent}, Path=Tag}'/>
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
	public void BoolPropertyGetterUsesCachedBoxes()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Tag='{CompiledBinding BoolProperty}' Name='textBlock' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			window.DataContext = new TestDataContext { BoolProperty = true };
			var boxedTrue = textBlock.Tag;
			window.DataContext = new TestDataContext { BoolProperty = false };
			var boxedFalse = textBlock.Tag;

			CornerstoneTest.AreEqual(true, boxedTrue);
			CornerstoneTest.AreEqual(false, boxedFalse);

			// The getter must return the cached boxes instead of allocating a new box per read.
			window.DataContext = new TestDataContext { BoolProperty = true };
			CornerstoneTest.Same(boxedTrue, textBlock.Tag);
			window.DataContext = new TestDataContext { BoolProperty = false };
			CornerstoneTest.Same(boxedFalse, textBlock.Tag);
		}
	}

	[PresentationTestMethod]
	public void CanBindBrushToHexString()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestData'
        x:CompileBindings='True'>
    <TextBlock Name='textBlock' Background='{Binding StringProperty}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.FindControl<TextBlock>("textBlock");

			var dataContext = new TestData { StringProperty = "#ff0000" };
			window.DataContext = dataContext;

			var brush = CornerstoneTest.IsType<ImmutableSolidColorBrush>(textBlock!.Background);
			CornerstoneTest.AreEqual(Colors.Red, brush.Color);
		}
	}

	[PresentationTestMethod]
	public void CanUseImplicitConversions()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:ImplicitConvertible'
        x:CompileBindings='True'>
    <TextBlock Name='textBlock'>
        <TextBlock.Background>
            <SolidColorBrush Color='{Binding}'/>
        </TextBlock.Background>
    </TextBlock>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new ImplicitConvertible("Green");
			window.DataContext = dataContext;

			var brush = CornerstoneTest.IsType<SolidColorBrush>(textBlock.Background);
			CornerstoneTest.AreEqual(Colors.Green, brush.Color);
		}
	}

	[PresentationTestMethod]
	public void CompilesBindingWhenRequested()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'
        x:CompileBindings='true'>
    <TextBlock Text='{Binding StringProperty}' Name='textBlock' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext
			{
				StringProperty = "foobar"
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.StringProperty, textBlock.Text);
		}
	}

	[PresentationTestMethod]
	[DataRow("OneWay")]
	[DataRow("TwoWay")]
	[DataRow("OneWayToSource")]
	[DataRow("OneTime")]
	public void EmitsTypedBindingExpressionForAllStandardModes(string mode)
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load($@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{{CompiledBinding StringProperty, Mode={mode}}}' Name='textBlock' />
</Window>");
			var textBlock = window.GetControl<TextBlock>("textBlock");
			window.DataContext = new TestDataContext { StringProperty = "x" };

			var expression = BindingOperations.GetBindingExpressionBase(textBlock, TextBlock.TextProperty);
			CornerstoneTest.IsType<TypedBindingExpression<TestDataContext, string>>(expression);
		}
	}

	[PresentationTestMethod]
	public void EmitsTypedBindingExpressionForBindingWithCompileBindingsTrue()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'
        x:CompileBindings='True'>
    <TextBlock Text='{Binding StringProperty}' Name='textBlock' />
</Window>");
			var textBlock = window.GetControl<TextBlock>("textBlock");
			window.DataContext = new TestDataContext { StringProperty = "hi" };

			var expression = BindingOperations.GetBindingExpressionBase(textBlock, TextBlock.TextProperty);
			CornerstoneTest.IsType<TypedBindingExpression<TestDataContext, string>>(expression);
		}
	}

	[PresentationTestMethod]
	public void EmitsTypedBindingExpressionForSimpleDataContextBinding()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
</Window>");
			var textBlock = window.GetControl<TextBlock>("textBlock");
			window.DataContext = new TestDataContext { StringProperty = "hello" };

			var expression = BindingOperations.GetBindingExpressionBase(textBlock, TextBlock.TextProperty);
			CornerstoneTest.IsType<TypedBindingExpression<TestDataContext, string>>(expression);
			CornerstoneTest.AreEqual("hello", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ExplicitDataTypeStillWorksOnDataGridLikeControls()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <local:DataGridLikeControl Name='target'>
        <local:DataGridLikeControl.Columns>
            <local:DataGridLikeColumn Binding='{CompiledBinding Length}' x:DataType='x:String'>
                <local:DataGridLikeColumn.Template>
                    <DataTemplate x:DataType='x:String'>
                        <TextBlock Text='{CompiledBinding Length}' />
                    </DataTemplate>
                </local:DataGridLikeColumn.Template>
            </local:DataGridLikeColumn>
        </local:DataGridLikeControl.Columns>
    </local:DataGridLikeControl>
</Window>");
			var target = window.GetControl<DataGridLikeControl>("target");
			var column = target.Columns.Single();

			var dataContext = new TestDataContext();
			dataContext.ListProperty.Add("Test");
			target.Items = dataContext.ListProperty;

			window.ApplyTemplate();
			target.ApplyTemplate();

			// Assert DataGridLikeColumn.Binding data type.
			var compiledPath = ((CompiledBinding) column.Binding!).Path;
			var node = CornerstoneTest.IsAssignableFrom<PropertyElement>(CornerstoneTest.Single(compiledPath!.Elements));
			CornerstoneTest.AreEqual(typeof(int), node.Property.PropertyType);

			// Assert DataGridLikeColumn.Template data type by evaluating the template.
			var firstItem = dataContext.ListProperty[0];
			var textBlockFromTemplate = (TextBlock) column.Template!.Build(firstItem)!;
			textBlockFromTemplate.DataContext = firstItem;
			CornerstoneTest.AreEqual(firstItem.Length.ToString(), textBlockFromTemplate.Text);
		}
	}

	[PresentationTestMethod]
	public void FallsBackToBindingExpressionForDataContextTarget()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock DataContext='{CompiledBinding StringProperty}' Name='textBlock' />
</Window>");
			var textBlock = window.GetControl<TextBlock>("textBlock");
			window.DataContext = new TestDataContext { StringProperty = "x" };

			var expression = BindingOperations.GetBindingExpressionBase(textBlock, StyledElement.DataContextProperty);
			CornerstoneTest.IsType<BindingExpression>(expression);
		}
	}

	[PresentationTestMethod]
	public void FallsBackToBindingExpressionForDataValidationEnabledProperty()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			// TextBox.Text enables data validation, which TypedBindingExpression does not
			// support, so the binding must fall back to the untyped BindingExpression even
			// though it is otherwise eligible for the typed path.
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBox Text='{CompiledBinding StringProperty}' Name='textBox' />
</Window>");
			var textBox = window.GetControl<TextBox>("textBox");
			window.DataContext = new TestDataContext { StringProperty = "hello" };

			var expression = BindingOperations.GetBindingExpressionBase(textBox, TextBox.TextProperty);
			CornerstoneTest.IsType<BindingExpression>(expression);
			CornerstoneTest.AreEqual("hello", textBox.Text);
		}
	}

	[PresentationTestMethod]
	public void FallsBackToBindingExpressionForNegatedBinding()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Tag='{CompiledBinding !BoolProperty}' Name='textBlock' />
</Window>");
			var textBlock = window.GetControl<TextBlock>("textBlock");
			window.DataContext = new TestDataContext { BoolProperty = true };

			var expression = BindingOperations.GetBindingExpressionBase(textBlock, TextBlock.TagProperty);
			CornerstoneTest.IsType<BindingExpression>(expression);
		}
	}

	[PresentationTestMethod]
	public void FallsBackToBindingExpressionForNestedPath()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding NestedGenericString.Value}' Name='textBlock' />
</Window>");
			var textBlock = window.GetControl<TextBlock>("textBlock");
			window.DataContext = new TestDataContext { NestedGenericString = new TestDataContext.NestedGeneric<string> { Value = "v" } };

			var expression = BindingOperations.GetBindingExpressionBase(textBlock, TextBlock.TextProperty);
			CornerstoneTest.IsType<BindingExpression>(expression);
		}
	}

	[PresentationTestMethod]
	public void FallsBackToBindingExpressionWhenConverterSet()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding StringProperty, Converter={x:Static local:AppendConverter.Instance}, ConverterParameter=suffix}' Name='textBlock' />
</Window>");
			var textBlock = window.GetControl<TextBlock>("textBlock");
			window.DataContext = new TestDataContext { StringProperty = "x" };

			var expression = BindingOperations.GetBindingExpressionBase(textBlock, TextBlock.TextProperty);
			CornerstoneTest.IsType<BindingExpression>(expression);
		}
	}

	[PresentationTestMethod]
	public void FallsBackToBindingExpressionWhenStringFormatSet()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding DecimalValue, StringFormat=c2}' Name='textBlock' />
</Window>");
			var textBlock = window.GetControl<TextBlock>("textBlock");
			window.DataContext = new TestDataContext();

			var expression = BindingOperations.GetBindingExpressionBase(textBlock, TextBlock.TextProperty);
			CornerstoneTest.IsType<BindingExpression>(expression);
		}
	}

	[PresentationTestMethod]
	public void IgnoresDataTemplateTypeFromDataTypePropertyIfXDataTypeDefined()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'>
    <Window.DataTemplates>
        <DataTemplate DataType='local:TestDataContextBaseClass' x:DataType='local:TestDataContext'>
            <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
        </DataTemplate>
    </Window.DataTemplates>
    <ContentControl x:DataType='local:TestDataContext' Name='target' Content='{CompiledBinding}' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var target = window.GetControl<ContentControl>("target");

			var dataContext = new TestDataContext();

			dataContext.StringProperty = "Initial Value";

			window.DataContext = dataContext;

			window.ApplyTemplate();
			target.ApplyTemplate();
			target.Presenter!.UpdateChild();

			CornerstoneTest.AreEqual(dataContext.StringProperty, ((TextBlock) target.Presenter.Child!).Text);
		}
	}

	[PresentationTestMethod]
	public void IndexerSetterBindsCorrectly()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBox Text='{CompiledBinding ListProperty[3], Mode=TwoWay}' Name='textBox' />
</Window>");
			var textBox = window.GetControl<TextBox>("textBox");

			var dataContext = new TestDataContext
			{
				ListProperty = { "A", "B", "C", "D", "E" }
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.ListProperty[3], textBox.Text);

			textBox.Text = "Z";

			CornerstoneTest.AreEqual("Z", dataContext.ListProperty[3]);
			CornerstoneTest.AreEqual(dataContext.ListProperty[3], textBox.Text);
		}
	}

	[PresentationTestMethod]
	public void InfersCompiledBindingDataContextFromDataContextBinding()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock DataContext='{CompiledBinding StringProperty}' Text='{CompiledBinding}' Name='textBlock' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();

			var dataContext = new TestDataContext
			{
				StringProperty = "A"
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.StringProperty, textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void InfersCustomDataTemplateBasedOnAttribute()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'>
    <Window.DataTemplates>
        <local:CustomDataTemplate FancyDataType='local:TestDataContext'>
            <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
        </local:CustomDataTemplate>
    </Window.DataTemplates>
    <ContentControl x:DataType='local:TestDataContext' Name='target' Content='{CompiledBinding}' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var target = window.GetControl<ContentControl>("target");

			var dataContext = new TestDataContext();

			dataContext.StringProperty = "Initial Value";

			window.DataContext = dataContext;

			window.ApplyTemplate();
			target.ApplyTemplate();
			target.Presenter!.UpdateChild();

			CornerstoneTest.AreEqual(dataContext.StringProperty, ((TextBlock) target.Presenter.Child!).Text);
		}
	}

	[PresentationTestMethod]
	public void InfersCustomDataTemplateBasedOnAttributeFromBaseClass()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'>
    <Window.DataTemplates>
        <local:CustomDataTemplateInherit FancyDataType='local:TestDataContext'>
            <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
        </local:CustomDataTemplateInherit>
    </Window.DataTemplates>
    <ContentControl x:DataType='local:TestDataContext' Name='target' Content='{CompiledBinding}' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var target = window.GetControl<ContentControl>("target");

			var dataContext = new TestDataContext();

			dataContext.StringProperty = "Initial Value";

			window.DataContext = dataContext;

			window.ApplyTemplate();
			target.ApplyTemplate();
			target.Presenter!.UpdateChild();

			CornerstoneTest.AreEqual(dataContext.StringProperty, ((TextBlock) target.Presenter.Child!).Text);
		}
	}

	[PresentationTestMethod]
	public void InfersDataTemplateTypeFromDataTypeProperty()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <Window.DataTemplates>
        <DataTemplate DataType='{x:Type x:String}'>
            <TextBlock Text='{CompiledBinding}' Name='textBlock' />
        </DataTemplate>
    </Window.DataTemplates>
    <ContentControl Name='target' Content='{CompiledBinding StringProperty}' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var target = window.GetControl<ContentControl>("target");

			var dataContext = new TestDataContext();

			dataContext.StringProperty = "Initial Value";

			window.DataContext = dataContext;

			window.ApplyTemplate();
			target.ApplyTemplate();
			target.Presenter!.UpdateChild();

			CornerstoneTest.AreEqual(dataContext.StringProperty, ((TextBlock) target.Presenter.Child!).Text);
		}
	}

	[PresentationTestMethod]
	public void InfersDataTemplateTypeFromParentCollectionItemsType()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <ItemsControl ItemsSource='{CompiledBinding ListProperty}' Name='target'>
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <TextBlock Text='{CompiledBinding}' Name='textBlock' />
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var target = window.GetControl<ItemsControl>("target");

			var dataContext = new TestDataContext();

			dataContext.ListProperty.Add("Test");

			window.DataContext = dataContext;

			window.ApplyTemplate();
			target.ApplyTemplate();
			target.Presenter!.ApplyTemplate();

			CornerstoneTest.AreEqual(dataContext.ListProperty[0], (string?) ((ContentPresenter) target.Presenter.Panel!.Children[0]).Content);
		}
	}

	[PresentationTestMethod]
	public void InfersDataTemplateTypeFromParentDataGridItemsType()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <local:DataGridLikeControl Items='{CompiledBinding ListProperty}' Name='target'>
        <local:DataGridLikeControl.Columns>
            <local:DataGridLikeColumn Binding='{CompiledBinding Length}'>
                <local:DataGridLikeColumn.Template>
                    <DataTemplate>
                        <TextBlock Text='{CompiledBinding Length}' />
                    </DataTemplate>
                </local:DataGridLikeColumn.Template>
            </local:DataGridLikeColumn>
        </local:DataGridLikeControl.Columns>
    </local:DataGridLikeControl>
</Window>");
			var target = window.GetControl<DataGridLikeControl>("target");
			var column = target.Columns.Single();

			var dataContext = new TestDataContext();

			dataContext.ListProperty.Add("Test");

			window.DataContext = dataContext;

			window.ApplyTemplate();
			target.ApplyTemplate();

			// Assert DataGridLikeColumn.Binding data type.
			var compiledPath = ((CompiledBinding) column.Binding!).Path;
			var node = CornerstoneTest.IsAssignableFrom<PropertyElement>(CornerstoneTest.Single(compiledPath!.Elements));
			CornerstoneTest.AreEqual(typeof(int), node.Property.PropertyType);

			// Assert DataGridLikeColumn.Template data type by evaluating the template.
			var firstItem = dataContext.ListProperty[0];
			var textBlockFromTemplate = (TextBlock) column.Template!.Build(firstItem)!;
			textBlockFromTemplate.DataContext = firstItem;
			CornerstoneTest.AreEqual(firstItem.Length.ToString(), textBlockFromTemplate.Text);
		}
	}

	[PresentationTestMethod]
	public void InfersDataTypeFromParentDataGridItemsTypeInCaseOfControlInheritance()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestItemsCollectionDataContext'>
    <local:DataGridLikeControlInheritor Items='{CompiledBinding Items}' Name='target'>
        <local:DataGridLikeControlInheritor.Columns>
            <local:DataGridLikeColumn Binding='{CompiledBinding StringProperty}'>
            </local:DataGridLikeColumn>
        </local:DataGridLikeControlInheritor.Columns>
    </local:DataGridLikeControlInheritor>
</Window>");
			var target = window.GetControl<DataGridLikeControl>("target");
			var column = target.Columns.Single();

			var dataContext = new TestItemsCollectionDataContext();

			dataContext.Items.Add(new TestData { StringProperty = "Test" });

			window.DataContext = dataContext;

			window.ApplyTemplate();
			target.ApplyTemplate();

			// Assert DataGridLikeColumn.Binding data type.
			var compiledPath = ((CompiledBinding) column.Binding!).Path;
			var node = CornerstoneTest.IsAssignableFrom<PropertyElement>(CornerstoneTest.Single(compiledPath!.Elements));

			CornerstoneTest.AreEqual(typeof(string), node.Property.PropertyType);
			CornerstoneTest.AreEqual(nameof(TestData.StringProperty), node.Property.Name);
		}
	}

	[PresentationTestMethod]
	public void ReportsMultipleErrorsOnDataContextAndBindingPathErrors()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'>
    <ContentControl Content='{CompiledBinding NoDataContext}'
                    Tag='{CompiledBinding NonExistentProp, DataType=local:TestDataContext}'
                    Height='{CompiledBinding invalid.}' />
</Window>";
			var ex = Assert.Throws<AggregateException>(() => CornerstoneRuntimeXamlLoader.Load(xaml));
			CornerstoneTest.Collection(ex.InnerExceptions, inner => CornerstoneTest.IsAssignableFrom<XmlException>(inner), inner => CornerstoneTest.IsAssignableFrom<XmlException>(inner), inner => CornerstoneTest.IsAssignableFrom<XmlException>(inner));
		}
	}

	[PresentationTestMethod]
	public void ResolvesArrayIndexerBindingCorrectly()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding ArrayProperty[3]}' Name='textBlock' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext
			{
				ArrayProperty = new[] { "A", "B", "C", "D", "E" }
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.ArrayProperty[3], textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesClrPropertyBasedOnDataContextType()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext
			{
				StringProperty = "foobar"
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.StringProperty, textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesClrPropertyBasedOnDataContextTypeInterfaceInheritance()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:IHasPropertyDerived'>
    <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext
			{
				StringProperty = "foobar"
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.StringProperty, textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesDataTypeForAssignBinding()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<local:AssignBindingControl xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'
        X='{CompiledBinding StringProperty}' />";
			var control = (AssignBindingControl) CornerstoneRuntimeXamlLoader.Load(xaml);
			var compiledPath = ((CompiledBinding) control.X!).Path;

			var node = CornerstoneTest.IsAssignableFrom<PropertyElement>(CornerstoneTest.Single(compiledPath!.Elements));
			CornerstoneTest.AreEqual(typeof(string), node.Property.PropertyType);
		}
	}

	[PresentationTestMethod]
	public void ResolvesDataTypeForAssignBindingFromBindingProperty()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<local:AssignBindingControl xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        X='{CompiledBinding StringProperty, DataType=local:TestDataContext}' />";
			var control = (AssignBindingControl) CornerstoneRuntimeXamlLoader.Load(xaml);
			var compiledPath = ((CompiledBinding) control.X!).Path;

			var node = CornerstoneTest.IsAssignableFrom<PropertyElement>(CornerstoneTest.Single(compiledPath!.Elements));
			CornerstoneTest.AreEqual(typeof(string), node.Property.PropertyType);
		}
	}

	[PresentationTestMethod]
	public void ResolvesDataTypeFromBindingProperty()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'>
    <TextBlock Text='{CompiledBinding StringProperty, DataType=local:TestDataContext}' Name='textBlock' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext
			{
				StringProperty = "foobar"
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.StringProperty, textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesDataTypeFromBindingPropertyTypeExtension()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'>
    <TextBlock Text='{CompiledBinding StringProperty, DataType={x:Type local:TestDataContext}}' Name='textBlock' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext
			{
				StringProperty = "foobar"
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.StringProperty, textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesElementNameBinding()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <StackPanel>
        <TextBlock Text='{CompiledBinding StringProperty}' x:Name='text' />
        <TextBlock Text='{CompiledBinding #text.Text}' x:Name='text2' />
    </StackPanel>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("text2");

			var dataContext = new TestDataContext
			{
				StringProperty = "foobar"
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.StringProperty, textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesElementNameBindingFromLongForm()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <StackPanel>
        <TextBlock Text='{CompiledBinding StringProperty}' x:Name='text' />
        <TextBlock Text='{CompiledBinding Text, ElementName=text}' x:Name='text2' />
    </StackPanel>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("text2");

			var dataContext = new TestDataContext
			{
				StringProperty = "foobar"
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.StringProperty, textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesElementNameBindingFromLongFormWithoutPath()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <StackPanel>
        <TextBlock Text='{CompiledBinding StringProperty}' x:Name='text' />
        <TextBlock Text='{CompiledBinding ElementName=text}' x:Name='text2' />
    </StackPanel>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("text2");

			CornerstoneTest.AreEqual("Cornerstone.Presentation.Controls.TextBlock", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesElementNameDataContextTypeBasedOnContext()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'
        x:Name='MyWindow'>
    <TextBlock Text='{CompiledBinding ElementName=MyWindow, Path=DataContext.StringProperty}' Name='textBlock' />
</Window>");
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext
			{
				StringProperty = "foobar"
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.StringProperty, textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesElementNameDataContextTypeBasedOnContextShortSyntax()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'
        x:Name='MyWindow'>
    <TextBlock Text='{CompiledBinding #MyWindow.DataContext.StringProperty}' Name='textBlock' />
</Window>");
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext
			{
				StringProperty = "foobar"
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.StringProperty, textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesElementNameInTemplate()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<ContentControl xmlns='https://github.com/BobbyCannon/Cornerstone'
                Content='Hello'>
    <ContentControl.Styles>
        <Style Selector='ContentControl'>
            <Setter Property='Template'>
                <ControlTemplate>
                    <Panel>
                        <TextBox Name='InnerTextBox' Text='Hello' />
                        <ContentPresenter Content='{CompiledBinding Text, ElementName=InnerTextBox}' />
                    </Panel>
                </ControlTemplate>
            </Setter>
        </Style>
    </ContentControl.Styles>
</ContentControl>";

			var contentControl = CornerstoneRuntimeXamlLoader.Parse<ContentControl>(xaml);
			contentControl.Measure(new Size(10, 10));

			var result = contentControl.GetTemplateDescendants().OfType<ContentPresenter>().First();

			CornerstoneTest.AreEqual("Hello", result.Content);
		}
	}

	[PresentationTestMethod]
	public void ResolvesIndexerBindingCorrectly()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding ListProperty[3]}' Name='textBlock' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext
			{
				ListProperty = { "A", "B", "C", "D", "E" }
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.ListProperty[3], textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesNestedGenericDataTypes()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='{x:Type local:TestDataContext+NestedGeneric, x:TypeArguments=x:String}'
        x:Name='MyWindow'>
    <Panel>
        <TextBlock Text='{CompiledBinding Value}' Name='textBlock' />
    </Panel>
</Window>");
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext
			{
				NestedGenericString = new TestDataContext.NestedGeneric<string>
				{
					Value = "10"
				}
			};

			window.DataContext = dataContext.NestedGenericString;

			CornerstoneTest.AreEqual(dataContext.NestedGenericString.Value, textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesNonIntegerIndexerBindingCorrectly()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding NonIntegerIndexerProperty[Test]}' Name='textBlock' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext();

			dataContext.NonIntegerIndexerProperty["Test"] = "Initial Value";

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.NonIntegerIndexerProperty["Test"], textBlock.Text);

			dataContext.NonIntegerIndexerProperty["Test"] = "New Value";

			CornerstoneTest.AreEqual(dataContext.NonIntegerIndexerProperty["Test"], textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesNonIntegerIndexerBindingFromParentInterfaceCorrectly()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding NonIntegerIndexerInterfaceProperty[Test]}' Name='textBlock' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext();

			dataContext.NonIntegerIndexerInterfaceProperty["Test"] = "Initial Value";

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.NonIntegerIndexerInterfaceProperty["Test"], textBlock.Text);

			dataContext.NonIntegerIndexerInterfaceProperty["Test"] = "New Value";

			CornerstoneTest.AreEqual(dataContext.NonIntegerIndexerInterfaceProperty["Test"], textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesObservableIndexerBindingCorrectly()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding ObservableCollectionProperty[3]}' Name='textBlock' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext
			{
				ObservableCollectionProperty = { "A", "B", "C", "D", "E" }
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.ObservableCollectionProperty[3], textBlock.Text);

			dataContext.ObservableCollectionProperty[3] = "New Value";

			CornerstoneTest.AreEqual(dataContext.ObservableCollectionProperty[3], textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesParentDataContextTypeBasedOnContext()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'
        x:Name='MyWindow'>
    <Panel>
        <TextBlock Text='{CompiledBinding $parent[Panel].DataContext.StringProperty}' Name='textBlock' />
    </Panel>
</Window>");
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext
			{
				StringProperty = "foobar"
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.StringProperty, textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesParentDataContextTypeBasedOnContextShortSyntax()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'
        x:Name='MyWindow'>
    <Panel>
        <TextBlock Text='{CompiledBinding $parent.DataContext.StringProperty}' Name='textBlock' />
    </Panel>
</Window>");
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext
			{
				StringProperty = "foobar"
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.StringProperty, textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesPathPassedByProperty()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding Path=StringProperty}' Name='textBlock' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext
			{
				StringProperty = "foobar"
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.StringProperty, textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesPathPassedByPropertyWithInnerItemTemplate()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <ItemsControl Name='itemsControl' ItemsSource='{CompiledBinding Path=ListProperty}'>
	    <ItemsControl.ItemTemplate>
		    <DataTemplate>
			    <TextBlock />
		    </DataTemplate>
	    </ItemsControl.ItemTemplate>
    </ItemsControl>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<ItemsControl>("itemsControl");

			var dataContext = new TestDataContext
			{
				ListProperty =
				{
					"Hello"
				}
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.ListProperty, textBlock.ItemsSource);
		}
	}

	[PresentationTestMethod]
	public void ResolvesRelativeSourceBindingEvenLongerForm()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'
        Title='test'>
    <TextBlock Text='{CompiledBinding Title, RelativeSource={RelativeSource AncestorType={x:Type Window}}}' x:Name='text'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var target = window.GetControl<TextBlock>("text");

			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();
			target.ApplyTemplate();

			//CornerstoneTest.AreEqual("test", target.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesRelativeSourceBindingFromStyleSelector()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<TextBox xmlns='https://github.com/BobbyCannon/Cornerstone'
         xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
         InnerLeftContent='Hello'>
    <TextBox.Styles>
        <Style Selector='TextBox'>
            <Setter Property='Template'>
                <ControlTemplate>
                    <StackPanel>
                        <ContentPresenter x:Name='Content' />
                        <TextPresenter x:Name='PART_TextPresenter' />
                    </StackPanel>
                </ControlTemplate>
            </Setter>
            <Style Selector='^ /template/ ContentPresenter#Content'>
                <Setter Property='Content' Value='{CompiledBinding InnerLeftContent, RelativeSource={RelativeSource TemplatedParent}}' />
            </Style>
        </Style>
    </TextBox.Styles>
</TextBox>";

			var textBox = CornerstoneRuntimeXamlLoader.Parse<TextBox>(xaml);
			textBox.DataContext = new TestDataContext(); // should be ignored
			textBox.Measure(new Size(10, 10));

			var result = textBox.GetTemplateDescendants().OfType<ContentPresenter>().First();
			CornerstoneTest.AreEqual(textBox.InnerLeftContent, result.Content);
		}
	}

	[PresentationTestMethod]
	public void ResolvesRelativeSourceBindingFromTemplate()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<ContentControl xmlns='https://github.com/BobbyCannon/Cornerstone'
                Focusable='True'>
    <ContentControl.Styles>
        <Style Selector='ContentControl'>
            <Setter Property='Template'>
                <ControlTemplate>
                    <ContentPresenter Focusable='{CompiledBinding !Focusable, RelativeSource={RelativeSource TemplatedParent}}' />
                </ControlTemplate>
            </Setter>
        </Style>
    </ContentControl.Styles>
</ContentControl>";

			var contentControl = CornerstoneRuntimeXamlLoader.Parse<ContentControl>(xaml);
			contentControl.DataContext = new TestDataContext(); // should be ignored
			contentControl.Measure(new Size(10, 10));

			var result = contentControl.GetTemplateDescendants().OfType<ContentPresenter>().First();
			CornerstoneTest.AreEqual(false, result.Focusable);
		}
	}

	[PresentationTestMethod]
	public void ResolvesRelativeSourceBindingLongForm()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'
        Title='test'>
    <TextBlock Text='{CompiledBinding Title, RelativeSource={RelativeSource AncestorType=Window}}' x:Name='text'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var target = window.GetControl<TextBlock>("text");

			window.ApplyTemplate();
			window.Presenter!.ApplyTemplate();
			target.ApplyTemplate();

			CornerstoneTest.AreEqual("test", target.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesStaticClrPropertyBased()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding StaticProperty}' Name='textBlock' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");
			textBlock.DataContext = new TestDataContext();

			CornerstoneTest.AreEqual(TestDataContext.StaticProperty, textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesStreamObservableBindingCorrectly()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding ObservableProperty^}' Name='textBlock' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			DelayedBinding.ApplyBindings(textBlock);

			var subject = new Subject<string>();
			var dataContext = new TestDataContext
			{
				ObservableProperty = subject
			};

			window.DataContext = dataContext;

			subject.OnNext("foobar");

			CornerstoneTest.AreEqual("foobar", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ResolvesStreamTaskBindingCorrectly()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding TaskProperty^}' Name='textBlock' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext
			{
				TaskProperty = Task.FromResult("foobar")
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual("foobar", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ShouldBindToNestedGenericProperty()
	{
		// See https://github.com/AvaloniaUI/Avalonia/issues/10485
		// This code works fine with SRE, and test is passing, but it fails on Cecil.
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'
        x:CompileBindings='True'>
    <ComboBox x:Name='comboBox' ItemsSource='{Binding GenericProperty}' SelectedItem='{Binding GenericProperty.CurrentItem}' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var comboBox = window.GetControl<ComboBox>("comboBox");

			var dataContext = new TestDataContext();
			dataContext.GenericProperty.Add(123);
			dataContext.GenericProperty.CurrentItem = 123;
			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(123, comboBox.SelectedItem);
		}
	}

	[PresentationTestMethod]
	[DataRow(true)]
	[DataRow(false)]
	public void ShouldNegateBooleanValue(bool value)
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'
        x:CompileBindings='True'>
    <TextBlock Name='textBlock' Tag='{Binding !BoolProperty}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext { BoolProperty = value };
			window.DataContext = dataContext;

			var result = CornerstoneTest.IsType<bool>(textBlock.Tag);
			CornerstoneTest.AreEqual(!value, result);
		}
	}

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void ShouldUseStringFormatWithoutBraces(bool compileBindings)
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = $@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'
        x:CompileBindings='{compileBindings}'>
    <TextBlock Name='textBlock' Text='{{Binding DecimalValue, StringFormat=c2}}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext();
			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(string.Format("{0:c2}", TestDataContext.ExpectedDecimal), textBlock.GetValue(TextBlock.TextProperty));
		}
	}

	[PresentationTestMethod]
	public void SupportCastToTypeInExpression()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions'
        x:DataType='local:TestDataContext'>
    <ContentControl Content='{CompiledBinding $parent.((local:TestDataContext)DataContext)}' Name='contentControl' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var contentControl = window.GetControl<ContentControl>("contentControl");

			var dataContext = new TestDataContext();

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext, contentControl.Content);
		}
	}

	[PresentationTestMethod]
	public void SupportCastToTypeInExpressionDifferentTypeEvaluatesToNull()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions'
        x:DataType='local:TestDataContext'>
    <ContentControl Content='{CompiledBinding $parent.((local:TestDataContext)DataContext)}' Name='contentControl' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var contentControl = window.GetControl<ContentControl>("contentControl");

			var dataContext = "foo";

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(null, contentControl.Content);
		}
	}

	[PresentationTestMethod]
	public void SupportCastToTypeInExpressionWithProperty()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions'
        x:DataType='local:TestDataContext'>
    <ContentControl Content='{CompiledBinding $parent.((local:TestDataContext)DataContext).StringProperty}' Name='contentControl' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var contentControl = window.GetControl<ContentControl>("contentControl");

			var dataContext = new TestDataContext
			{
				StringProperty = "foobar"
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.StringProperty, contentControl.Content);
		}
	}

	[PresentationTestMethod]
	public void SupportCastToTypeInExpressionWithProperty1()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions'
        x:DataType='local:TestDataContext'>
    <ContentControl Content='{CompiledBinding $parent.DataContext(local:TestDataContext).StringProperty}' Name='contentControl' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var contentControl = window.GetControl<ContentControl>("contentControl");

			var dataContext = new TestDataContext
			{
				StringProperty = "foobar"
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.StringProperty, contentControl.Content);
		}
	}

	[PresentationTestMethod]
	public void SupportCastToTypeInExpressionWithPropertyDifferentTypeEvaluatesToNull()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions'
        x:DataType='local:TestDataContext'>
    <ContentControl Content='{CompiledBinding $parent.((local:TestDataContext)DataContext).StringProperty}' Name='contentControl' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var contentControl = window.GetControl<ContentControl>("contentControl");

			var dataContext = new TestDataContext
			{
				StringProperty = "foobar"
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(dataContext.StringProperty, contentControl.Content);

			window.DataContext = "foo";

			CornerstoneTest.AreEqual(null, contentControl.Content);
		}
	}

	[PresentationTestMethod]
	public void SupportCastToTypeInExpressionWithPropertyExplicitPropertyCast()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions'>
    <ContentControl Content='{CompiledBinding $parent.((local:IHasExplicitProperty)DataContext).ExplicitProperty}' Name='contentControl' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var contentControl = window.GetControl<ContentControl>("contentControl");

			var dataContext = new TestDataContext();

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(((IHasExplicitProperty) dataContext).ExplicitProperty, contentControl.Content);
		}
	}

	[PresentationTestMethod]
	public void SupportCastToTypeInExpressionWithPropertyIndexer()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='using:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions'
        x:DataType='local:TestDataContext'>
    <ContentControl Content='{CompiledBinding ((local:TestData)ObjectsArrayProperty[0]).StringProperty}' Name='contentControl' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var contentControl = window.GetControl<ContentControl>("contentControl");

			var data = new TestData
			{
				StringProperty = "Foo"
			};
			var dataContext = new TestDataContext
			{
				ObjectsArrayProperty = new object[] { data }
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(data.StringProperty, contentControl.Content);
		}
	}

	[PresentationTestMethod]
	public void SupportConverterWithCulture()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext' x:CompileBindings='True'>
    <TextBlock Name='textBlock' Text='{Binding StringProperty, Converter={x:Static local:AppendConverter.Instance}, ConverterCulture=ar-SA}'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			window.DataContext = new TestDataContext { StringProperty = "Foo" };

			CornerstoneTest.AreEqual("Foo++ar-SA", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void SupportConverterWithParameter()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext' x:CompileBindings='True'>
    <TextBlock Name='textBlock' Text='{Binding StringProperty, Converter={x:Static local:AppendConverter.Instance}, ConverterParameter=Bar}'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			window.DataContext = new TestDataContext { StringProperty = "Foo" };

			CornerstoneTest.AreEqual($"Foo+Bar+{CultureInfo.CurrentCulture}", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void SupportParentInPath()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        Title='foo'>
    <ContentControl Content='{CompiledBinding $parent.Title}' Name='contentControl' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var contentControl = window.GetControl<ContentControl>("contentControl");

			CornerstoneTest.AreEqual("foo", contentControl.Content);
		}
	}

	[PresentationTestMethod]
	public void SupportsDotPath()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding .}' Name='textBlock' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext
			{
				StringProperty = "foobar"
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(typeof(TestDataContext).FullName, textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void SupportsEmptyPath()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding}' Name='textBlock' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext
			{
				StringProperty = "foobar"
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(typeof(TestDataContext).FullName, textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void SupportsEmptyPathWithStringFormat()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding StringFormat=bar-\{0\}}' Name='textBlock' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext
			{
				StringProperty = "foobar"
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual("bar-" + typeof(TestDataContext).FullName, textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void SupportsExplicitDotPathWithStringFormat()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <TextBlock Text='{CompiledBinding Path=., StringFormat=bar-\{0\}}' Name='textBlock' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var dataContext = new TestDataContext
			{
				StringProperty = "foobar"
			};

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual("bar-" + typeof(TestDataContext).FullName, textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void SupportsMethodBindingAsDelegate()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:MethodDataContext'>
    <StackPanel>
        <ContentControl Content='{CompiledBinding Action}' Name='action' />
        <ContentControl Content='{CompiledBinding Func}' Name='func' />
        <ContentControl Content='{CompiledBinding Func2}' Name='func2' />
        <ContentControl Content='{CompiledBinding CustomDelegateTypeVoid}' Name='customvoid' />
        <ContentControl Content='{CompiledBinding CustomDelegateTypeInt}' Name='customint' />
    </StackPanel>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			window.DataContext = new MethodDataContext();

			CornerstoneTest.IsType(typeof(Action), window.GetControl<ContentControl>("action").Content);
			CornerstoneTest.IsType(typeof(Func<object>), window.GetControl<ContentControl>("func").Content);
			CornerstoneTest.IsType(typeof(Func<object, object>), window.GetControl<ContentControl>("func2").Content);
			CornerstoneTest.IsTrue(typeof(Delegate).IsAssignableFrom(window.GetControl<ContentControl>("customvoid").Content!.GetType()));
			CornerstoneTest.IsTrue(typeof(Delegate).IsAssignableFrom(window.GetControl<ContentControl>("customint").Content!.GetType()));
		}
	}

	[PresentationTestMethod]
	public void SupportsParentInPathWithTypeAndLevelFilter()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Border x:Name='p2'>
        <Border x:Name='p1'>
            <Button x:Name='p0'>
                <TextBlock x:Name='textBlock' Text='{CompiledBinding $parent[Control;1].Name}' />
            </Button>
        </Border>
    </Border>
</Window>");
			var textBlock = window.GetControl<TextBlock>("textBlock");

			CornerstoneTest.AreEqual("p1", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void ThrowsOnInvalidBindingPathOnCompiledBindingEnabledViaDirective()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'
        x:CompileBindings='true'>
    <TextBlock Text='{Binding InvalidPath}' Name='textBlock' />
</Window>";
			CornerstoneTest.Throws<XmlException>(() => CornerstoneRuntimeXamlLoader.Load(xaml));
		}
	}

	[PresentationTestMethod]
	public void ThrowsOnInvalidCompileBindingsDirective()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'
        x:CompileBindings='notabool'>
</Window>";
			CornerstoneTest.Throws<XmlException>(() => CornerstoneRuntimeXamlLoader.Load(xaml));
		}
	}

	[PresentationTestMethod]
	public void ThrowsOnUninferrableDataTemplateInItemsControlWithoutItemsBinding()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <ItemsControl Name='target'>
        <ItemsControl.DataTemplates>
            <DataTemplate>
                <TextBlock Text='{CompiledBinding Property}' Name='textBlock' />
            </DataTemplate>
        </ItemsControl.DataTemplates>
    </ItemsControl>
</Window>";
			CornerstoneTest.Throws<XmlException>(() => CornerstoneRuntimeXamlLoader.Load(xaml));
		}
	}

	[PresentationTestMethod]
	public void ThrowsOnUninferrableDataTypeFromNonCompiledDataContextBindingWithCompiledBindingPath()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <ContentControl Name='target' DataContext='{Binding}'>
        <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
    </ContentControl>
</Window>";
			CornerstoneTest.Throws<XmlException>(() => CornerstoneRuntimeXamlLoader.Load(xaml));
		}
	}

	[PresentationTestMethod]
	public void ThrowsOnUninferrableLooseDataTemplateNoDataTypeWithCompiledBindingPath()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'>
    <Window.DataTemplates>
        <DataTemplate>
            <TextBlock Text='{CompiledBinding StringProperty}' Name='textBlock' />
        </DataTemplate>
    </Window.DataTemplates>
    <ContentControl Name='target' Content='{CompiledBinding}' />
</Window>";
			CornerstoneTest.Throws<XmlException>(() => CornerstoneRuntimeXamlLoader.Load(xaml));
		}
	}

	[PresentationTestMethod]
	public void TypeCastWorksWithElementNameDataContext()
	{
		// By default, DataContext will infer DataType from the XAML context, which will be local:TestDataContext here.
		// But developer should be able to re-define this type via type casing, if they know better.
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(@"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        x:DataType='local:TestDataContext'
        x:Name='MyWindow'>
    <Panel>
        <TextBlock Text='{CompiledBinding $parent.((Button)DataContext).Tag}' Name='textBlock' />
    </Panel>
</Window>");
			var textBlock = window.GetControl<TextBlock>("textBlock");

			var panelDataContext = new Button { Tag = "foo" };
			((Panel) window.Content!).DataContext = panelDataContext;

			CornerstoneTest.AreEqual(panelDataContext.Tag, textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void UsesRuntimeLoaderConfigurationToEnabledCompiled()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<local:AssignBindingControl xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        X='{CompiledBinding StringProperty, DataType=local:TestDataContext}' />";
			var control = (AssignBindingControl) CornerstoneRuntimeXamlLoader.Load(new RuntimeXamlLoaderDocument(xaml),
				new RuntimeXamlLoaderConfiguration { UseCompiledBindingsByDefault = true });
			var compiledPath = ((CompiledBinding) control.X!).Path;

			var node = CornerstoneTest.IsAssignableFrom<PropertyElement>(CornerstoneTest.Single(compiledPath!.Elements));
			CornerstoneTest.AreEqual(typeof(string), node.Property.PropertyType);
		}
	}

	private static void PerformClick(Button button)
	{
		button.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = Key.Enter
		});
	}

	private static void Throws(string type, Action cb)
	{
		try
		{
			cb();
		}
		catch (Exception e) when (e.GetType().Name == type)
		{
			return;
		}

		throw new Exception("Expected " + type);
	}

	#endregion
}

public interface INonIntegerIndexer
{
	#region Properties

	string this[string key] { get; set; }

	#endregion
}

public interface INonIntegerIndexerDerived : INonIntegerIndexer
{
}

public interface IHasProperty
{
	#region Properties

	string? StringProperty { get; set; }

	#endregion
}

public interface IHasPropertyDerived : IHasProperty
{
}

public interface IHasExplicitProperty
{
	#region Properties

	string ExplicitProperty { get; }

	#endregion
}

public class AppendConverter : IValueConverter
{
	#region Properties

	public static IValueConverter Instance { get; } = new AppendConverter();

	#endregion

	#region Methods

	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return string.Format("{0}+{1}+{2}", value, parameter, culture);
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}

	#endregion
}

public class TestData
{
	#region Properties

	public string? StringProperty { get; set; }

	#endregion
}

public class TestDataContextBaseClass
{
}

public class TestItemsCollectionDataContext : TestDataContextBaseClass
{
	#region Properties

	public ObservableCollection<TestData> Items { get; } = new();

	#endregion
}

public class TestDataContext : TestDataContextBaseClass, IHasPropertyDerived, IHasExplicitProperty
{
	#region Constants

	public const decimal ExpectedDecimal = 15.756m;

	#endregion

	#region Properties

	public string[]? ArrayProperty { get; set; }
	public bool BoolProperty { get; set; }
	public decimal DecimalValue { get; set; } = ExpectedDecimal;

	public string ExplicitProperty => "Bye";

	public ListItemCollectionView<int> GenericProperty { get; } = new();

	public List<string> ListProperty { get; set; } = new();

	public NestedGeneric<string>? NestedGenericString { get; init; }

	public INonIntegerIndexerDerived NonIntegerIndexerInterfaceProperty => NonIntegerIndexerProperty;

	public NonIntegerIndexer NonIntegerIndexerProperty { get; set; } = new();

	public object[]? ObjectsArrayProperty { get; set; }

	public ObservableCollection<string> ObservableCollectionProperty { get; set; } = new();

	public IObservable<string>? ObservableProperty { get; set; }

	public static string StaticProperty => "World";
	public string? StringProperty { get; set; }

	public Task<string>? TaskProperty { get; set; }

	string IHasExplicitProperty.ExplicitProperty => "Hello";

	#endregion

	#region Classes

	public class NestedGeneric<T>
	{
		#region Properties

		public T? Value { get; set; }

		#endregion
	}

	public class NonIntegerIndexer : NotifyingBase, INonIntegerIndexerDerived
	{
		#region Fields

		private readonly Dictionary<string, string> _storage = new();

		#endregion

		#region Properties

		public string this[string key]
		{
			get => _storage[key];
			set
			{
				_storage[key] = value;
				RaisePropertyChanged(CommonPropertyNames.IndexerName);
			}
		}

		#endregion
	}

	#endregion
}

public class ListItemCollectionView<T> : List<T>
{
	#region Properties

	public T? CurrentItem { get; set; }

	#endregion
}

public class MethodDataContext
{
	#region Methods

	public void Action()
	{
	}

	public object CustomDelegateTypeInt(object i)
	{
		return i;
	}

	public void CustomDelegateTypeVoid(object i)
	{
	}

	public object Func()
	{
		return 1;
	}

	public object Func2(object i)
	{
		return i;
	}

	#endregion
}

public class MethodAsCommandDataContextBase
{
	#region Methods

	public void MethodWithNewSlot(int i)
	{
	}

	public virtual void VirtualInt32Method(int i)
	{
	}

	public virtual void VirtualObjectMethod(object? i)
	{
	}

	public virtual void VirtualStringMethod(string i)
	{
	}

	#endregion
}

public class MethodAsCommandDataContext : MethodAsCommandDataContextBase, INotifyPropertyChanged
{
	#region Fields

	private object? _parameter;

	#endregion

	#region Properties

	public object? Parameter
	{
		get => _parameter;
		set
		{
			if (_parameter == value)
			{
				return;
			}
			_parameter = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Parameter)));
		}
	}

	public string Value { get; private set; } = "Not called";

	#endregion

	#region Methods

	[Metadata.DependsOn(nameof(Parameter))]
	public bool CanDo(object parameter)
	{
		return !ReferenceEquals(null, Parameter);
	}

	public void Do(object parameter)
	{
		Value = $"Do {parameter}";
	}

	public void Int32Method(int i)
	{
		Value = $"Called Int32Method with {i}";
	}

	public void Method()
	{
		Value = "Called";
	}

	public new void MethodWithNewSlot(int i)
	{
		Value = $"Called MethodWithNewSlot with {i}";
	}

	public void MethodWithOverloads()
	{
		Value = "Called MethodWithOverloads without parameter";
	}

	public void MethodWithOverloads(int i)
	{
		Value = $"Called MethodWithOverloads with Int32 {i}";
	}

	public void MethodWithOverloads(string i)
	{
		Value = $"Called MethodWithOverloads with String {i}";
	}

	public void MethodWithOverloads(object i)
	{
		Value = $"Called MethodWithOverloads with Object {i}";
	}

	public void MethodWithOverloads2()
	{
		Value = "Called MethodWithOverloads2 without parameter";
	}

	public void MethodWithOverloads2(int i)
	{
		Value = $"Called MethodWithOverloads2 with Int32 {i}";
	}

	public void MethodWithOverloads2(string i)
	{
		Value = $"Called MethodWithOverloads2 with String {i}";
	}

	public void MethodWithOverloads3()
	{
		Value = "Called MethodWithOverloads3 without parameter";
	}

	public void MethodWithOverloads3(int a, int b)
	{
		throw new InvalidOperationException("MethodWithOverloads3 should not be called");
	}

	public void MethodWithOverloads3(string a, string b)
	{
		throw new InvalidOperationException("MethodWithOverloads3 should not be called");
	}

	public void MethodWithOverloads4(int a, int b)
	{
		throw new InvalidOperationException("MethodWithOverloads4 should not be called");
	}

	public void MethodWithOverloads4(string a, string b)
	{
		throw new InvalidOperationException("MethodWithOverloads4 should not be called");
	}

	public void ObjectMethod(object i)
	{
		Value = $"Called ObjectMethod with {i}";
	}

	public void StringMethod(string i)
	{
		Value = $"Called StringMethod with {i}";
	}

	public override void VirtualInt32Method(int i)
	{
		Value = $"Called VirtualInt32Method with {i}";
	}

	public override void VirtualObjectMethod(object? i)
	{
		Value = $"Called VirtualObjectMethod with {i}";
	}

	public override void VirtualStringMethod(string i)
	{
		Value = $"Called VirtualStringMethod with {i}";
	}

	#endregion

	#region Events

	public event PropertyChangedEventHandler? PropertyChanged;

	#endregion
}

public class CustomDataTemplate : IDataTemplate
{
	#region Properties

	[Content]
	[TemplateContent]
	public object? Content { get; set; }

	[DataType]
	public Type? FancyDataType { get; set; }

	#endregion

	#region Methods

	public Control? Build(object? data)
	{
		return TemplateContent.Load(Content)?.Result;
	}

	public bool Match(object? data)
	{
		return FancyDataType?.IsInstanceOfType(data) ?? true;
	}

	#endregion
}

public class CustomDataTemplateInherit : CustomDataTemplate
{
}

public class AssignBindingControl : Control
{
	#region Properties

	[AssignBinding]
	public BindingBase? X { get; set; }

	#endregion
}

public class DataGridLikeControl : Control
{
	#region Fields

	public static readonly DirectProperty<DataGridLikeControl, IEnumerable?> ItemsProperty =
		PresentationProperty.RegisterDirect<DataGridLikeControl, IEnumerable?>(
			nameof(Items),
			x => x.Items,
			(x, v) => x.Items = v);

	private IEnumerable? _items;

	#endregion

	#region Properties

	public PresentationList<DataGridLikeColumn> Columns { get; } = new();

	public IEnumerable? Items
	{
		get => _items;
		set => SetAndRaise(ItemsProperty, ref _items, value);
	}

	#endregion
}

public class DataGridLikeColumn
{
	#region Properties

	[AssignBinding]
	[InheritDataTypeFromItems(nameof(DataGridLikeControl.Items), AncestorType = typeof(DataGridLikeControl))]
	public BindingBase? Binding { get; set; }

	[InheritDataTypeFromItems(nameof(DataGridLikeControl.Items), AncestorType = typeof(DataGridLikeControl))]
	public IDataTemplate? Template { get; set; }

	#endregion
}

public class DataGridLikeControlInheritor : DataGridLikeControl
{
}

public class ImplicitConvertible
{
	#region Constructors

	public ImplicitConvertible(string value)
	{
		Value = value;
	}

	#endregion

	#region Properties

	public string Value { get; }

	#endregion

	#region Methods

	public static implicit operator Color(ImplicitConvertible value)
	{
		return Color.Parse(value.Value);
	}

	#endregion
}