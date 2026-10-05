#region References

using System;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class DirectPropertyTests
{
	#region Methods

	[PresentationTestMethod]
	public void AddOwneredGetMetadataCannotBeChanged()
	{
		var p1 = Class1.FooProperty;
		var p2 = p1.AddOwner<Class2>(_ => null, (_, _) => { });
		var metadata = p2.GetMetadata<Class2>();

		Assert.Throws<InvalidOperationException>(() => metadata.Merge(new DirectPropertyMetadata<string>(), p2));
	}

	[PresentationTestMethod]
	public void AddOwneredPropertiesShouldShareObservables()
	{
		var p1 = Class1.FooProperty;
		var p2 = p1.AddOwner<Class2>(o => null, (o, v) => { });

		CornerstoneTest.Same(p1.Changed, p2.Changed);
	}

	[PresentationTestMethod]
	public void AddOwneredPropertyShouldEqualOriginal()
	{
		var p1 = Class1.FooProperty;
		var p2 = p1.AddOwner<Class2>(o => null, (o, v) => { });

		CornerstoneTest.NotSame(p1, p2);
		CornerstoneTest.IsTrue(p1.Equals(p2));
		CornerstoneTest.AreEqual(p1.GetHashCode(), p2.GetHashCode());
		CornerstoneTest.IsTrue(p1 == p2);
	}

	[PresentationTestMethod]
	public void AddOwneredPropertyShouldHaveOwnerTypeSet()
	{
		var p1 = Class1.FooProperty;
		var p2 = p1.AddOwner<Class2>(o => null, (o, v) => { });

		CornerstoneTest.AreEqual(typeof(Class2), p2.OwnerType);
	}

	[PresentationTestMethod]
	public void DefaultGetMetadataCannotBeChanged()
	{
		var p1 = Class1.FooProperty;
		var metadata = p1.GetMetadata<Class1>();

		Assert.Throws<InvalidOperationException>(() => metadata.Merge(new DirectPropertyMetadata<string>(), p1));
	}

	[PresentationTestMethod]
	public void IsDirectPropertyReturnsTrue()
	{
		var target = new DirectProperty<Class1, string>(
			"test",
			o => null,
			null,
			new DirectPropertyMetadata<string>());

		CornerstoneTest.IsTrue(target.IsDirect);
	}

	#endregion

	#region Classes

	private class Class1 : PresentationObject
	{
		#region Fields

		public static readonly DirectProperty<Class1, string> FooProperty =
			PresentationProperty.RegisterDirect<Class1, string>(nameof(Foo), o => o.Foo, (o, v) => o.Foo = v);

		private string _foo = "foo";

		#endregion

		#region Properties

		public string Foo
		{
			get => _foo;
			set => SetAndRaise(FooProperty, ref _foo, value);
		}

		#endregion
	}

	private class Class2 : PresentationObject
	{
	}

	#endregion
}