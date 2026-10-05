#region References

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Subjects;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class PresentationObjectTestsMultiBinding
{
	#region Fields

	private static readonly IMultiValueConverter StringJoinConverter = new FuncMultiValueConverter<object, string>(v => string.Join(",", v.ToArray()));

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void MultiValueConverterShouldNotSkipValidDefaultValueTypeValue()
	{
		var target = new FuncMultiValueConverter<StringValueTypeWrapper, string>(v => string.Join(",", v.ToArray()));

		IList<object> Create(string[] values)
		{
			return values.Select(v => (object) (v != null ? new StringValueTypeWrapper { Value = v } : default)).ToList();
		}

		var value = target.Convert(Create(new[] { "Foo", "Bar", "Baz" }), typeof(string), null, CultureInfo.InvariantCulture);

		CornerstoneTest.AreEqual("Foo,Bar,Baz", value);

		value = target.Convert(Create(new[] { null, "Bar", "Baz" }), typeof(string), null, CultureInfo.InvariantCulture);

		CornerstoneTest.AreEqual(",Bar,Baz", value);
	}

	[PresentationTestMethod]
	public void MultiValueConverterShouldNotSkipValidNullReferenceTypeValue()
	{
		var target = new FuncMultiValueConverter<string, string>(v => string.Join(",", v.ToArray()));

		var value = target.Convert(new[] { "Foo", "Bar", "Baz" }, typeof(string), null, CultureInfo.InvariantCulture);

		CornerstoneTest.AreEqual("Foo,Bar,Baz", value);

		value = target.Convert(new[] { null, "Bar", "Baz" }, typeof(string), null, CultureInfo.InvariantCulture);

		CornerstoneTest.AreEqual(",Bar,Baz", value);
	}

	[PresentationTestMethod]
	public void MultiValueConverterSupportsIndexingTheParameters()
	{
		var target = new FuncMultiValueConverter<string, string>(v => v[0]);

		var value = target.Convert(new[] { "Foo", "Bar", "Baz" }, typeof(string), null, CultureInfo.InvariantCulture);

		CornerstoneTest.AreEqual("Foo", value);

		value = target.Convert(new[] { null, "Bar", "Baz" }, typeof(string), null, CultureInfo.InvariantCulture);

		CornerstoneTest.IsNull(value);
	}

	[PresentationTestMethod]
	public void ShouldUpdate()
	{
		var target = new Class1();

		var b = new Subject<object>();

		var mb = new MultiBinding
		{
			Converter = StringJoinConverter,
			Bindings = new[]
			{
				b.ToBinding()
			}
		};
		target.Bind(Class1.FooProperty, mb);

		CornerstoneTest.AreEqual(null, target.Foo);

		b.OnNext("Foo");

		CornerstoneTest.AreEqual("Foo", target.Foo);

		b.OnNext("Bar");

		CornerstoneTest.AreEqual("Bar", target.Foo);
	}

	[PresentationTestMethod]
	public void ShouldUpdateWhenNullValueInBindings()
	{
		var target = new Class1();

		var b = new Subject<object>();

		var mb = new MultiBinding
		{
			Converter = StringJoinConverter,
			Bindings = new[]
			{
				b.ToBinding()
			}
		};
		target.Bind(Class1.FooProperty, mb);

		CornerstoneTest.AreEqual(null, target.Foo);

		b.OnNext("Foo");

		CornerstoneTest.AreEqual("Foo", target.Foo);

		b.OnNext(null);

		CornerstoneTest.AreEqual("", target.Foo);
	}

	[PresentationTestMethod]
	public void ShouldUpdateWhenNullValueInBindingsWithStringFormat()
	{
		var target = new Class1();

		var b = new Subject<object>();

		var mb = new MultiBinding
		{
			StringFormat = "Converted: {0}",
			Bindings = new[]
			{
				b.ToBinding()
			}
		};
		target.Bind(Class1.FooProperty, mb);

		CornerstoneTest.AreEqual(null, target.Foo);
		b.OnNext("Foo");
		CornerstoneTest.AreEqual("Converted: Foo", target.Foo);
		b.OnNext(null);
		CornerstoneTest.AreEqual("Converted: ", target.Foo);
	}

	[PresentationTestMethod]
	public void ShouldUpdateWithMultipleBindings()
	{
		var target = new Class1();

		var bindings = Enumerable.Range(0, 3).Select(i => new BehaviorSubject<object>("Empty")).ToArray();

		var mb = new MultiBinding
		{
			Converter = StringJoinConverter,
			Bindings = bindings.Select(b => b.ToBinding()).ToArray()
		};
		target.Bind(Class1.FooProperty, mb);

		CornerstoneTest.AreEqual("Empty,Empty,Empty", target.Foo);

		bindings[0].OnNext("Foo");

		CornerstoneTest.AreEqual("Foo,Empty,Empty", target.Foo);

		bindings[1].OnNext("Bar");

		CornerstoneTest.AreEqual("Foo,Bar,Empty", target.Foo);

		bindings[2].OnNext("Baz");

		CornerstoneTest.AreEqual("Foo,Bar,Baz", target.Foo);
	}

	#endregion

	#region Classes

	private class Class1 : PresentationObject
	{
		#region Fields

		public static readonly StyledProperty<string> FooProperty =
			PresentationProperty.Register<Class1, string>("Foo");

		#endregion

		#region Properties

		public string Foo
		{
			get => GetValue(FooProperty);
			set => SetValue(FooProperty, value);
		}

		#endregion
	}

	#endregion

	#region Structures

	private struct StringValueTypeWrapper
	{
		#region Fields

		public string Value;

		#endregion

		#region Methods

		public override string ToString()
		{
			return Value;
		}

		#endregion
	}

	#endregion
}