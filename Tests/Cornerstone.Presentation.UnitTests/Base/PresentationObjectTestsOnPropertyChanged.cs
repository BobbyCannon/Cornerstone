#region References

using System.Collections.Generic;
using System.Reactive.Subjects;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class PresentationObjectTestsOnPropertyChanged
{
	#region Methods

	[PresentationTestMethod]
	public void OnPropertyChangedCoreIsCalledOnNonEffectivePropertyBindingValueChange()
	{
		var target = new Class1();
		var source = new BehaviorSubject<BindingValue<string>>("styled1");

		target.Bind(Class1.FooProperty, source, BindingPriority.Style);
		target.SetValue(Class1.FooProperty, "newvalue", BindingPriority.Animation);
		source.OnNext("styled2");

		CornerstoneTest.AreEqual(3, target.CoreChanges.Count);

		var change = (PresentationPropertyChangedEventArgs<string>) target.CoreChanges[2];

		CornerstoneTest.AreEqual("styled2", change.NewValue.Value);
		CornerstoneTest.IsFalse(change.OldValue.HasValue);
		CornerstoneTest.AreEqual(BindingPriority.Style, change.Priority);
		CornerstoneTest.IsFalse(change.IsEffectiveValueChange);
	}

	[PresentationTestMethod]
	public void OnPropertyChangedCoreIsCalledOnNonEffectivePropertyValueChange()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "newvalue", BindingPriority.Animation);
		target.SetValue(Class1.FooProperty, "styled", BindingPriority.Style);

		CornerstoneTest.AreEqual(2, target.CoreChanges.Count);

		var change = (PresentationPropertyChangedEventArgs<string>) target.CoreChanges[1];

		CornerstoneTest.AreEqual("styled", change.NewValue.Value);
		CornerstoneTest.IsFalse(change.OldValue.HasValue);
		CornerstoneTest.AreEqual(BindingPriority.Style, change.Priority);
		CornerstoneTest.IsFalse(change.IsEffectiveValueChange);
	}

	[PresentationTestMethod]
	public void OnPropertyChangedCoreIsCalledOnPropertyChange()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "newvalue");

		CornerstoneTest.AreEqual(1, target.CoreChanges.Count);

		var change = (PresentationPropertyChangedEventArgs<string>) target.CoreChanges[0];

		CornerstoneTest.AreEqual("newvalue", change.NewValue.Value);
		CornerstoneTest.AreEqual("foodefault", change.OldValue.Value);
		CornerstoneTest.AreEqual(BindingPriority.LocalValue, change.Priority);
		CornerstoneTest.IsTrue(change.IsEffectiveValueChange);
	}

	[PresentationTestMethod]
	public void OnPropertyChangedIsCalledOnlyForEffectiveValueChanges()
	{
		var target = new Class1();

		target.SetValue(Class1.FooProperty, "newvalue", BindingPriority.Animation);
		target.SetValue(Class1.FooProperty, "styled", BindingPriority.Style);

		CornerstoneTest.AreEqual(1, target.Changes.Count);
		CornerstoneTest.AreEqual(2, target.CoreChanges.Count);
	}

	#endregion

	#region Classes

	private class Class1 : PresentationObject
	{
		#region Fields

		public static readonly StyledProperty<string> FooProperty =
			PresentationProperty.Register<Class1, string>("Foo", "foodefault");

		#endregion

		#region Constructors

		public Class1()
		{
			Changes = new List<PresentationPropertyChangedEventArgs>();
			CoreChanges = new List<PresentationPropertyChangedEventArgs>();
		}

		#endregion

		#region Properties

		public List<PresentationPropertyChangedEventArgs> Changes { get; }
		public List<PresentationPropertyChangedEventArgs> CoreChanges { get; }

		#endregion

		#region Methods

		protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
		{
			Changes.Add(Clone(change));
			base.OnPropertyChanged(change);
		}

		protected override void OnPropertyChangedCore(PresentationPropertyChangedEventArgs change)
		{
			CoreChanges.Add(Clone(change));
			base.OnPropertyChangedCore(change);
		}

		private static PresentationPropertyChangedEventArgs Clone(PresentationPropertyChangedEventArgs change)
		{
			var e = (PresentationPropertyChangedEventArgs<string>) change;
			return new PresentationPropertyChangedEventArgs<string>(
				change.Sender,
				e.Property,
				e.OldValue,
				e.NewValue,
				change.Priority,
				change.IsEffectiveValueChange);
		}

		#endregion
	}

	#endregion
}