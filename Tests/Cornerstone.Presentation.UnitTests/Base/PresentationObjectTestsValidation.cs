#region References

using System;
using System.Reactive.Subjects;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class PresentationObjectTestsValidation
{
	#region Methods

	[PresentationTestMethod]
	public void MetadataOverrideThrowsIfDefaultValueFailsValidation()
	{
		Assert.Throws<ArgumentException>(() => Class1.FooProperty.OverrideDefaultValue<Class2>(101));
	}

	[PresentationTestMethod]
	public void RegistrationThrowsIfDefaultValueFailsValidation()
	{
		Assert.Throws<ArgumentException>(() =>
			new StyledProperty<int>(
				"BadDefault",
				typeof(Class1),
				typeof(Class1),
				new StyledPropertyMetadata<int>(101),
				validate: Class1.ValidateFoo));
	}

	[PresentationTestMethod]
	public void RevertsToDefaultValueEvenInPresenceOfOtherBindings()
	{
		var target = new Class1();
		var source1 = new Subject<int>();
		var source2 = new Subject<int>();

		target.Bind(Class1.FooProperty, source1);
		target.Bind(Class1.FooProperty, source2);
		source1.OnNext(42);
		source2.OnNext(150);

		CornerstoneTest.AreEqual(11, target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void RevertsToDefaultValueIfLocalValueBindingFailsValidation()
	{
		var target = new Class1();
		var source = new Subject<int>();

		target.Bind(Class1.FooProperty, source);
		source.OnNext(150);

		CornerstoneTest.AreEqual(11, target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void RevertsToDefaultValueIfStyleBindingFailsValidation()
	{
		var target = new Class1();
		var source = new Subject<int>();

		target.Bind(Class1.FooProperty, source, BindingPriority.Style);
		source.OnNext(150);

		CornerstoneTest.AreEqual(11, target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void RevertsToDefaultValueIfStyleBindingFailsValidation2()
	{
		var target = new Class1();
		var source = new Subject<int>();

		target.SetValue(Class1.FooProperty, 10, BindingPriority.Style);
		target.Bind(Class1.FooProperty, source, BindingPriority.StyleTrigger);
		source.OnNext(150);

		CornerstoneTest.AreEqual(11, target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	[DataRow(BindingPriority.LocalValue)]
	[DataRow(BindingPriority.Style)]
	public void RevertsToDefaultValueIfStyleBindingFailsValidation3(BindingPriority priority)
	{
		var target = new Class1();
		var source = new Subject<BindingValue<int>>();

		target.Bind(Class1.FooProperty, source, priority);
		source.OnNext(150);

		CornerstoneTest.AreEqual(11, target.GetValue(Class1.FooProperty));
	}

	[PresentationTestMethod]
	public void SetValueThrowsIfFailsValidation()
	{
		var target = new Class1();

		Assert.Throws<ArgumentException>(() => target.SetValue(Class1.FooProperty, 101));
	}

	[PresentationTestMethod]
	public void SetValueThrowsIfFailsValidationAttached()
	{
		var target = new Class1();

		Assert.Throws<ArgumentException>(() => target.SetValue(Class1.AttachedProperty, 101));
	}

	#endregion

	#region Classes

	private class Class1 : PresentationObject
	{
		#region Fields

		public static readonly StyledProperty<int> FooProperty =
			PresentationProperty.Register<Class1, int>(
				"Qux",
				11,
				validate: ValidateFoo);

		public static readonly AttachedProperty<int> AttachedProperty =
			PresentationProperty.RegisterAttached<Class1, Class1, int>(
				"Attached",
				11,
				validate: ValidateFoo);

		#endregion

		#region Methods

		public static bool ValidateFoo(int value)
		{
			return value < 100;
		}

		#endregion
	}

	private class Class2 : PresentationObject
	{
	}

	#endregion
}