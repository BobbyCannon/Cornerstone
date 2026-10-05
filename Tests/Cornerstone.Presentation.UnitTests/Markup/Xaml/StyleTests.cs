#nullable enable

#region References

using System.Linq;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml;

[TestClass]
public class StyleTests : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void BindingAsAttributeShouldBeAssignedToSetterValueInsteadOfBound()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
		{
			var xaml = "<Style Selector='Button' xmlns='https://github.com/BobbyCannon/Cornerstone'><Setter Property='Content' Value='{Binding}'/></Style>";
			var style = (Style) CornerstoneRuntimeXamlLoader.Load(xaml);
			var setter = (Setter) style.Setters.First();

			CornerstoneTest.IsType<ReflectionBinding>(setter.Value);
		}
	}

	[PresentationTestMethod]
	[DataRow(nameof(ContentControl.Content))] // standard property
	[DataRow(nameof(Layoutable.Margin))] // primitive property which can be directly parsed
	public void BindingAsElementShouldBeAssignedToSetterValue(string propertyName)
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
		{
			var style = (Style) CornerstoneRuntimeXamlLoader.Load(
				$"""
				<Style Selector="Button" xmlns="https://github.com/BobbyCannon/Cornerstone">
				    <Setter Property="{propertyName}">
				        <Binding />
				    </Setter>
				</Style>
				""");
			var setter = (Setter) style.Setters.First();

			CornerstoneTest.IsType<ReflectionBinding>(setter.Value);
		}
	}

	[PresentationTestMethod]
	public void SetterWithTwoWayBindingShouldUpdateSource()
	{
		using (UnitTestApplication.Start(TestServices.MockThreadingInterface))
		{
			var data = new Data
			{
				Foo = "foo"
			};

			var control = new TextBox
			{
				DataContext = data
			};

			var style = new Style
			{
				Setters =
				{
					new Setter
					{
						Property = TextBox.TextProperty,
						Value = new Binding
						{
							Path = "Foo",
							Mode = BindingMode.TwoWay
						}
					}
				}
			};

			StyleHelpers.TryAttach(style, control);
			CornerstoneTest.AreEqual("foo", control.Text);

			control.Text = "bar";
			CornerstoneTest.AreEqual("bar", data.Foo);
		}
	}

	[PresentationTestMethod]
	public void XmlValueShouldBeAssignedToSetterValue()
	{
		using (UnitTestApplication.Start(TestServices.MockPlatformWrapper))
		{
			var style = (Style) CornerstoneRuntimeXamlLoader.Load(@"
<Style Selector='Button' xmlns='https://github.com/BobbyCannon/Cornerstone'>
    <Setter Property='Margin'>
        10, 4, 0, 4
    </Setter>
</Style>");
			var setter = (Setter) style.Setters.First();

			var thickness = CornerstoneTest.IsType<Thickness>(setter.Value);
			CornerstoneTest.AreEqual(new Thickness(10, 4, 0, 4), thickness);
		}
	}

	#endregion

	#region Classes

	private class Data
	{
		#region Properties

		public string? Foo { get; set; }

		#endregion
	}

	#endregion
}