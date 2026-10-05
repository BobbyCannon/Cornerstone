#nullable enable

#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Immutable;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.VisualTree;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Data;

[TestClass]
public class BindingTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void BindingToDoNothingWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock Name='textBlock' Text='{Binding}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			window.ApplyTemplate();

			window.DataContext = "foo";
			CornerstoneTest.AreEqual("foo", textBlock.Text);

			window.DataContext = BindingOperations.DoNothing;
			CornerstoneTest.AreEqual("foo", textBlock.Text);

			window.DataContext = "bar";
			CornerstoneTest.AreEqual("bar", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void BindingWithNullPathWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock Name='textBlock' Text='{Binding}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			window.DataContext = "foo";
			window.ApplyTemplate();

			CornerstoneTest.AreEqual("foo", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void CanBindBrushtoHexString()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Data;assembly=Cornerstone.Presentation.UnitTests'>
    <Border Background='{Binding HexString}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var border = (Border) window.Content!;
			window.DataContext = new { HexString = "#ff0000" };

			window.ApplyTemplate();

			var brush = CornerstoneTest.IsType<ImmutableSolidColorBrush>(border.Background);
			CornerstoneTest.AreEqual(Colors.Red, brush.Color);
		}
	}

	[PresentationTestMethod]
	public void MultiBindingTemplatedParentWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Data;assembly=Cornerstone.Presentation.UnitTests'>
    <TextBox Name='textBox' Text='Foo' PlaceholderText='Bar'>
        <TextBox.Template>
            <ControlTemplate>
                <TextPresenter Name='PART_TextPresenter'>
                    <TextPresenter.Text>
                        <MultiBinding Converter='{x:Static local:ConcatConverter.Instance}'>
                            <Binding RelativeSource='{RelativeSource TemplatedParent}' Path='Text'/>
                            <Binding RelativeSource='{RelativeSource TemplatedParent}' Path='PlaceholderText'/>
                        </MultiBinding>
                    </TextPresenter.Text>
                </TextPresenter>
            </ControlTemplate>
        </TextBox.Template>
    </TextBox>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBox = window.GetControl<TextBox>("textBox");

			window.ApplyTemplate();
			textBox.ApplyTemplate();

			var target = (TextPresenter) textBox.GetVisualChildren().Single();
			CornerstoneTest.AreEqual("Foo,Bar", target.Text);
		}
	}

	#endregion
}

public class ConcatConverter : IMultiValueConverter
{
	#region Properties

	public static ConcatConverter Instance { get; } = new();

	#endregion

	#region Methods

	public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
	{
		return string.Join(",", values);
	}

	#endregion
}