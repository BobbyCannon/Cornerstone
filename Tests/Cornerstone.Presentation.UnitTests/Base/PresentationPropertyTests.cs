#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.PropertyStore;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class PresentationPropertyTests
{
	#region Methods

	[PresentationTestMethod]
	public void ChangedObservableFired()
	{
		var target = new Class1();
		string value = null;

		Class1.FooProperty.Changed.Subscribe(x => value = x.NewValue.GetValueOrDefault());
		target.SetValue(Class1.FooProperty, "newvalue");

		CornerstoneTest.AreEqual("newvalue", value);
	}

	[PresentationTestMethod]
	public void ChangedObservableFiredOnlyOnEffectiveValueChange()
	{
		var target = new Class1();
		var result = new List<string>();

		Class1.FooProperty.Changed.Subscribe(x => result.Add(x.NewValue.GetValueOrDefault()));
		target.SetValue(Class1.FooProperty, "animated", BindingPriority.Animation);
		target.SetValue(Class1.FooProperty, "local");

		CornerstoneTest.AreEqual(new[] { "animated" }, result);
	}

	[PresentationTestMethod]
	public void ConstructorSetsProperties()
	{
		var target = new TestProperty<string>("test", typeof(Class1));

		CornerstoneTest.AreEqual("test", target.Name);
		CornerstoneTest.AreEqual(typeof(string), target.PropertyType);
		CornerstoneTest.AreEqual(typeof(Class1), target.OwnerType);
	}

	[PresentationTestMethod]
	public void DefaultMetadataCannotBeChangedAfterPropertyInitialization()
	{
		var metadata = new TestMetadata();
		var property = new TestProperty<string>("test", typeof(Class1), metadata);

		Assert.Throws<InvalidOperationException>(() => metadata.Merge(new TestMetadata(), property));
	}

	[PresentationTestMethod]
	public void GetMetadataReturnsOverriddenValue()
	{
		var metadata = new TestMetadata();
		var overridden = new TestMetadata();
		var target = new TestProperty<string>("test", typeof(Class1), metadata);

		target.OverrideMetadata<Class2>(overridden);

		CornerstoneTest.Same(overridden, target.GetMetadata<Class2>());
	}

	[PresentationTestMethod]
	public void GetMetadataReturnsSuppliedValue()
	{
		var metadata = new TestMetadata();
		var target = new TestProperty<string>("test", typeof(Class1), metadata);

		CornerstoneTest.Same(metadata, target.GetMetadata<Class1>());
	}

	[PresentationTestMethod]
	public void GetMetadataReturnsSuppliedValueForDerivedClass()
	{
		var metadata = new TestMetadata();
		var target = new TestProperty<string>("test", typeof(Class1), metadata);

		CornerstoneTest.Same(metadata, target.GetMetadata<Class2>());
	}

	[PresentationTestMethod]
	public void GetMetadataReturnsTypeSafeMetadataForUnrelatedClass()
	{
		var metadata = new TestMetadata(BindingMode.OneWayToSource, true, x => { _ = (StyledElement) x; });
		var target = new TestProperty<string>("test", typeof(Class3), metadata);

		var targetMetadata = (TestMetadata) target.GetMetadata<Class2>();

		CornerstoneTest.AreEqual(metadata.DefaultBindingMode, targetMetadata.DefaultBindingMode);
		CornerstoneTest.AreEqual(metadata.EnableDataValidation, targetMetadata.EnableDataValidation);
		CornerstoneTest.AreEqual(null, targetMetadata.OwnerSpecificAction);
	}

	[PresentationTestMethod]
	public void NameCannotContainPeriods()
	{
		Assert.Throws<ArgumentException>(() => new TestProperty<string>("Foo.Bar", typeof(Class1)));
	}

	[PresentationTestMethod]
	public void NotifyFiredOnlyOnEffectiveValueChange()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "animated", BindingPriority.Animation);
		target.SetValue(Class1.FooProperty, "local");

		CornerstoneTest.AreEqual(2, target.NotifyCount);
	}

	[PresentationTestMethod]
	public void OverriddenMetadataCannotBeChangedAfterOverrideMetadata()
	{
		var metadata = new TestMetadata(BindingMode.TwoWay);
		var overridden = new TestMetadata();
		var property = new TestProperty<string>("test", typeof(Class1), metadata);

		property.OverrideMetadata<Class2>(overridden);

		Assert.Throws<InvalidOperationException>(() => overridden.Merge(new TestMetadata(), property));
	}

	[PresentationTestMethod]
	public void OverrideMetadataShouldMergeValues()
	{
		var metadata = new TestMetadata(BindingMode.TwoWay);
		var overridden = new TestMetadata();
		var target = new TestProperty<string>("test", typeof(Class1), metadata);

		target.OverrideMetadata<Class2>(overridden);

		var result = target.GetMetadata<Class2>();
		CornerstoneTest.AreEqual(BindingMode.TwoWay, result.DefaultBindingMode);
	}

	[PresentationTestMethod]
	public void PropertyEqualsShouldHandleNull()
	{
		var p1 = new TestProperty<string>("p1", typeof(Class1));

		CornerstoneTest.IsNotNull(p1);
		CornerstoneTest.IsNotNull(p1);
		CornerstoneTest.IsFalse(p1 == null);
		CornerstoneTest.IsFalse(null == p1);
		CornerstoneTest.IsFalse(p1.Equals(null));
		CornerstoneTest.IsTrue(null == (PresentationProperty) null);
	}

	[PresentationTestMethod]
	public void PropertyMetadataBindingModeDefaultReturnsOneWay()
	{
		var data = new TestMetadata(BindingMode.Default);

		CornerstoneTest.AreEqual(BindingMode.OneWay, data.DefaultBindingMode);
	}

	#endregion

	#region Classes

	private class Class1 : PresentationObject
	{
		#region Fields

		public static readonly StyledProperty<string> FooProperty =
			PresentationProperty.Register<Class1, string>("Foo", "default",
				true,
				BindingMode.OneWay,
				null,
				null,
				false,
				FooNotifying);

		#endregion

		#region Properties

		public int NotifyCount { get; private set; }

		#endregion

		#region Methods

		private static void FooNotifying(PresentationObject o, bool n)
		{
			++((Class1) o).NotifyCount;
		}

		#endregion
	}

	private class Class2 : Class1
	{
	}

	private class Class3 : PresentationObject
	{
	}

	private class TestMetadata : PresentationPropertyMetadata
	{
		#region Constructors

		public TestMetadata(BindingMode defaultBindingMode = BindingMode.Default,
			bool? enableDataValidation = null,
			Action<PresentationObject> ownerSpecificAction = null)
			: base(defaultBindingMode, enableDataValidation)
		{
			OwnerSpecificAction = ownerSpecificAction;
		}

		#endregion

		#region Properties

		public Action<PresentationObject> OwnerSpecificAction { get; }

		#endregion

		#region Methods

		public override PresentationPropertyMetadata GenerateTypeSafeMetadata()
		{
			return new TestMetadata(DefaultBindingMode, EnableDataValidation, null);
		}

		#endregion
	}

	private class TestProperty<TValue> : PresentationProperty<TValue>
	{
		#region Constructors

		public TestProperty(string name, Type ownerType, TestMetadata metadata = null)
			: base(name, ownerType, ownerType, metadata ?? new TestMetadata())
		{
		}

		#endregion

		#region Methods

		public void OverrideMetadata<T>(PresentationPropertyMetadata metadata)
		{
			OverrideMetadata(typeof(T), metadata);
		}

		internal override EffectiveValue CreateEffectiveValue(PresentationObject o)
		{
			throw new NotImplementedException();
		}

		internal override IDisposable RouteBind(
			PresentationObject o,
			IObservable<object> source,
			BindingPriority priority)
		{
			throw new NotImplementedException();
		}

		internal override void RouteClearValue(PresentationObject o)
		{
			throw new NotImplementedException();
		}

		internal override void RouteCoerceDefaultValue(PresentationObject o)
		{
			throw new NotImplementedException();
		}

		internal override object RouteGetBaseValue(PresentationObject o)
		{
			throw new NotImplementedException();
		}

		internal override object RouteGetValue(PresentationObject o)
		{
			throw new NotImplementedException();
		}

		internal override void RouteSetCurrentValue(PresentationObject o, object value)
		{
			throw new NotImplementedException();
		}

		internal override IDisposable RouteSetValue(
			PresentationObject o,
			object value,
			BindingPriority priority)
		{
			throw new NotImplementedException();
		}

		#endregion
	}

	#endregion
}