#nullable enable

#region References

using System.ComponentModel;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Markup.Xaml.Converters;
using Cornerstone.Presentation.Markup.Xaml.XamlIl.Runtime;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Converters;

[TestClass]
public class PresentationPropertyConverterTest : XamlTestBase
{
	#region Constructors

	public PresentationPropertyConverterTest()
	{
		// Ensure properties are registered.
		_ = Class1.FooProperty;
		_ = AttachedOwner.AttachedProperty;
	}

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void ConvertFromFindsAttachedProperty()
	{
		var target = new PresentationPropertyTypeConverter();
		var style = new Style(x => x.OfType<Class1>());
		var context = CreateContext(style);
		var result = target.ConvertFrom(context, null, "AttachedOwner.Attached");

		CornerstoneTest.AreEqual(AttachedOwner.AttachedProperty, result);
	}

	[PresentationTestMethod]
	public void ConvertFromFindsAttachedPropertyWithParentheses()
	{
		var target = new PresentationPropertyTypeConverter();
		var style = new Style(x => x.OfType<Class1>());
		var context = CreateContext(style);
		var result = target.ConvertFrom(context, null, "(AttachedOwner.Attached)");

		CornerstoneTest.AreEqual(AttachedOwner.AttachedProperty, result);
	}

	[PresentationTestMethod]
	public void ConvertFromFindsFullyQualifiedProperty()
	{
		var target = new PresentationPropertyTypeConverter();
		var style = new Style(x => x.OfType<Class1>());
		var context = CreateContext(style);
		var result = target.ConvertFrom(context, null, "Class1.Foo");

		CornerstoneTest.AreEqual(Class1.FooProperty, result);
	}

	[PresentationTestMethod]
	public void ConvertFromThrowsForNonexistentAttachedProperty()
	{
		var target = new PresentationPropertyTypeConverter();
		var style = new Style(x => x.OfType<Class1>());
		var context = CreateContext(style);

		var ex = Assert.Throws<XamlLoadException>(() => target.ConvertFrom(context, null, "AttachedOwner.NonExistent"));

		CornerstoneTest.AreEqual("Could not find property 'AttachedOwner.NonExistent'.", ex.Message);
	}

	[PresentationTestMethod]
	public void ConvertFromThrowsForNonexistentProperty()
	{
		var target = new PresentationPropertyTypeConverter();
		var style = new Style(x => x.OfType<Class1>());
		var context = CreateContext(style);

		var ex = Assert.Throws<XamlLoadException>(() => target.ConvertFrom(context, null, "Nonexistent"));

		CornerstoneTest.AreEqual("Could not find property 'Class1.Nonexistent'.", ex.Message);
	}

	[PresentationTestMethod]
	public void ConvertFromUsesSelectorTargetType()
	{
		var target = new PresentationPropertyTypeConverter();
		var style = new Style(x => x.OfType<Class1>());
		var context = CreateContext(style);
		var result = target.ConvertFrom(context, null, "Foo");

		CornerstoneTest.AreEqual(Class1.FooProperty, result);
	}

	private ITypeDescriptorContext CreateContext(Style? style = null)
	{
		var tdMock = new StubTypeDescriptorContext();
		var tr = new StubXamlTypeResolver();
		var ps = new StubParentStackProvider();

		tdMock.SetService(typeof(IXamlTypeResolver), tr);
		tdMock.SetService(typeof(ICornerstoneXamlIlParentStackProvider), ps);
		ps.Parents = style is null ? [] : [style];
		tr.SetType(nameof(Class1), typeof(Class1));
		tr.SetType(nameof(AttachedOwner), typeof(AttachedOwner));

		return tdMock;
	}

	#endregion

	#region Classes

	private class AttachedOwner
	{
		#region Fields

		public static readonly AttachedProperty<string> AttachedProperty =
			PresentationProperty.RegisterAttached<AttachedOwner, Class1, string>("Attached");

		#endregion
	}

	private class Class1 : StyledElement
	{
		#region Fields

		public static readonly StyledProperty<string> FooProperty =
			PresentationProperty.Register<Class1, string>("Foo");

		#endregion
	}

	#endregion
}