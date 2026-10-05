#nullable enable

#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;

[TestClass]
public class BindingExtensionTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void BindingExtensionBindsToSource()
	{
		using (StyledWindow())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <x:String x:Key='text'>foobar</x:String>
    </Window.Resources>

    <TextBlock Name='textBlock' Text='{Binding Source={StaticResource text}}'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			window.Show();

			CornerstoneTest.AreEqual("foobar", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void BindingExtensionBindsToTargetNullValue()
	{
		using (StyledWindow())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
             xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Window.Resources>
        <x:String x:Key='text'>foobar</x:String>
    </Window.Resources>

    <TextBlock Name='textBlock' Text='{Binding Foo, TargetNullValue={StaticResource text}}'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			window.DataContext = new FooBar();
			window.Show();

			CornerstoneTest.AreEqual("foobar", textBlock.Text);
		}
	}

	[PresentationTestMethod]
	public void BindingExtensionTargetNullValueUnsetByDefault()
	{
		using (StyledWindow())
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock Name='textBlock' IsVisible='{Binding Foo, Converter={x:Static ObjectConverters.IsNotNull}}'/>
</Window>";

			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");

			window.DataContext = new FooBar();
			window.Show();

			CornerstoneTest.AreEqual(false, textBlock.IsVisible);
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
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        >
    <ContentControl Content='{Binding $parent.((local:TestDataContext)DataContext).StringProperty}' Name='contentControl' />
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
	public void SupportCastToTypeInExpressionDifferentTypeEvaluatesToNull()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.MarkupExtensions;assembly=Cornerstone.Presentation.UnitTests'
        >
    <ContentControl Content='{Binding $parent.((local:TestDataContext)DataContext)}' Name='contentControl' />
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var contentControl = window.GetControl<ContentControl>("contentControl");

			var dataContext = "foo";

			window.DataContext = dataContext;

			CornerstoneTest.AreEqual(null, contentControl.Content);
		}
	}

	private static IDisposable StyledWindow()
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
						new VisualLayerManager
						{
							Child =
								new ContentPresenter
								{
									Name = "PART_ContentPresenter",
									[!ContentPresenter.ContentProperty] = x[!Window.ContentProperty]
								}.RegisterInNameScope(scope)
						}))
			}
		};
	}

	#endregion

	#region Classes

	private class FooBar
	{
		#region Properties

		public object? Foo { get; } = null;

		#endregion
	}

	#endregion
}