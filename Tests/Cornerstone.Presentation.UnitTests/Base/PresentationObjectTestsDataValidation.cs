#region References

using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base;

[TestClass]
public class PresentationObjectTestsDataValidation
{
	#region Classes

	[TestClass]
	public class DirectPropertyTests : TestBase<DirectPropertyBase<int>>
	{
		#region Methods

		[PresentationTestMethod]
		public void BoundValidatedStringPropertyCanBeSetToNull()
		{
			var source = new ViewModel
			{
				StringValue = "foo"
			};

			var target = new Class1
			{
				[!Class1.ValidatedDirectStringProperty] = new Binding
				{
					Path = nameof(ViewModel.StringValue),
					Source = source
				}
			};

			CornerstoneTest.AreEqual("foo", target.ValidatedDirectString);

			source.StringValue = null;

			CornerstoneTest.IsNull(target.ValidatedDirectString);
		}

		protected override DirectPropertyBase<int> GetNonValidatedProperty()
		{
			return Class1.NonValidatedDirectIntProperty;
		}

		protected override DirectPropertyBase<int> GetProperty()
		{
			return Class1.ValidatedDirectIntProperty;
		}

		#endregion
	}

	[TestClass]
	public class StyledPropertyTests : TestBase<StyledProperty<int>>
	{
		#region Methods

		[PresentationTestMethod]
		public void BoundValidatedStringPropertyCanBeSetToNull()
		{
			var source = new ViewModel
			{
				StringValue = "foo"
			};

			var target = new Class1
			{
				[!Class1.ValidatedDirectStringProperty] = new Binding
				{
					Path = nameof(ViewModel.StringValue),
					Source = source
				}
			};

			CornerstoneTest.AreEqual("foo", target.ValidatedDirectString);

			source.StringValue = null;

			CornerstoneTest.IsNull(target.ValidatedDirectString);
		}

		protected override StyledProperty<int> GetNonValidatedProperty()
		{
			return Class1.NonValidatedStyledIntProperty;
		}

		protected override StyledProperty<int> GetProperty()
		{
			return Class1.ValidatedStyledIntProperty;
		}

		#endregion
	}

	public abstract class TestBase<T>
		where T : PresentationProperty<int>
	{
		#region Methods

		[PresentationTestMethod]
		public void BindingNonValidatedPropertyDoesNotCallUpdateDataValidation()
		{
			var target = new Class1();
			var source = new Subject<BindingValue<int>>();
			var property = GetNonValidatedProperty();

			target.Bind(property, source);
			source.OnNext(6);
			source.OnNext(BindingValue<int>.BindingError(new Exception()));
			source.OnNext(BindingValue<int>.DataValidationError(new Exception()));
			source.OnNext(6);

			CornerstoneTest.Empty(target.Notifications);
		}

		[PresentationTestMethod]
		public void BindingOverriddenValidatedPropertyCallsUpdateDataValidation()
		{
			var target = new Class2();
			var source = new Subject<BindingValue<int>>();
			var property = GetNonValidatedProperty();

			// Class2 overrides the non-validated property metadata to enable data validation.
			target.Bind(property, source);
			source.OnNext(1);

			CornerstoneTest.AreEqual(1, target.Notifications.Count);
		}

		[PresentationTestMethod]
		public void BindingValidatedPropertyCallsUpdateDataValidation()
		{
			var target = new Class1();
			var source = new Subject<BindingValue<int>>();
			var property = GetProperty();
			var error1 = new Exception();
			var error2 = new Exception();

			target.Bind(property, source);
			source.OnNext(6);
			source.OnNext(BindingValue<int>.DataValidationError(error1));
			source.OnNext(BindingValue<int>.BindingError(error2));
			source.OnNext(7);

			CornerstoneTest.AreEqual(new Notification[]
			{
				new(BindingValueType.Value, 6, null),
				new(BindingValueType.DataValidationError, 6, error1),
				new(BindingValueType.BindingError, 0, error2),
				new(BindingValueType.Value, 7, null)
			}, target.Notifications);
		}

		[PresentationTestMethod]
		public void BindingValidatedPropertyCallsUpdateDataValidationUntyped()
		{
			var target = new Class1();
			var source = new Subject<object>();
			var property = GetProperty();
			var error1 = new Exception();
			var error2 = new Exception();

			target.Bind(property, source);
			source.OnNext(6);
			source.OnNext(new BindingNotification(error1, BindingErrorType.DataValidationError));
			source.OnNext(new BindingNotification(error2, BindingErrorType.Error));
			source.OnNext(7);

			CornerstoneTest.AreEqual(new Notification[]
			{
				new(BindingValueType.Value, 6, null),
				new(BindingValueType.DataValidationError, 6, error1),
				new(BindingValueType.BindingError, 0, error2),
				new(BindingValueType.Value, 7, null)
			}, target.Notifications);
		}

		[PresentationTestMethod]
		public void CompletingBindingClearsDataValidation()
		{
			var target = new Class1();
			var source = new Subject<BindingValue<int>>();
			var property = GetProperty();
			var error = new Exception();

			target.Bind(property, source);
			source.OnNext(6);
			source.OnNext(BindingValue<int>.DataValidationError(error));
			source.OnCompleted();

			CornerstoneTest.AreEqual(new Notification[]
			{
				new(BindingValueType.Value, 6, null),
				new(BindingValueType.DataValidationError, 6, error),
				new(BindingValueType.UnsetValue, 6, null)
			}, target.Notifications);
		}

		[PresentationTestMethod]
		public void DisposingBindingSubscriptionClearsDataValidation()
		{
			var target = new Class1();
			var source = new Subject<BindingValue<int>>();
			var property = GetProperty();
			var error = new Exception();
			var sub = target.Bind(property, source);

			source.OnNext(6);
			source.OnNext(BindingValue<int>.DataValidationError(error));
			sub.Dispose();

			CornerstoneTest.AreEqual(new Notification[]
			{
				new(BindingValueType.Value, 6, null),
				new(BindingValueType.DataValidationError, 6, error),
				new(BindingValueType.UnsetValue, 6, null)
			}, target.Notifications);
		}

		protected abstract T GetNonValidatedProperty();

		protected abstract T GetProperty();

		#endregion
	}

	public class ViewModel : NotifyingBase
	{
		#region Fields

		private string _stringValue;

		#endregion

		#region Properties

		public string StringValue
		{
			get => _stringValue;
			set
			{
				_stringValue = value;
				RaisePropertyChanged();
			}
		}

		#endregion
	}

	private class Class1 : PresentationObject
	{
		#region Fields

		public static readonly DirectProperty<Class1, int> NonValidatedDirectIntProperty =
			PresentationProperty.RegisterDirect<Class1, int>(
				nameof(NonValidatedDirectInt),
				o => o.NonValidatedDirectInt,
				(o, v) => o.NonValidatedDirectInt = v);

		public static readonly StyledProperty<int> NonValidatedStyledIntProperty =
			PresentationProperty.Register<Class1, int>(
				nameof(NonValidatedStyledInt));

		public static readonly DirectProperty<Class1, int> ValidatedDirectIntProperty =
			PresentationProperty.RegisterDirect<Class1, int>(
				nameof(ValidatedDirectInt),
				o => o.ValidatedDirectInt,
				(o, v) => o.ValidatedDirectInt = v,
				enableDataValidation: true);

		public static readonly DirectProperty<Class1, string> ValidatedDirectStringProperty =
			PresentationProperty.RegisterDirect<Class1, string>(
				nameof(ValidatedDirectString),
				o => o.ValidatedDirectString,
				(o, v) => o.ValidatedDirectString = v,
				enableDataValidation: true);

		public static readonly StyledProperty<int> ValidatedStyledIntProperty =
			PresentationProperty.Register<Class1, int>(
				nameof(ValidatedStyledInt),
				enableDataValidation: true);

		private int _directInt;
		private string _directString;

		private int _nonValidatedDirect;

		#endregion

		#region Properties

		public int NonValidatedDirectInt
		{
			get => _directInt;
			set => SetAndRaise(NonValidatedDirectIntProperty, ref _nonValidatedDirect, value);
		}

		public int NonValidatedStyledInt
		{
			get => GetValue(NonValidatedStyledIntProperty);
			set => SetValue(NonValidatedStyledIntProperty, value);
		}

		public List<Notification> Notifications { get; } = new();

		public int ValidatedDirectInt
		{
			get => _directInt;
			set => SetAndRaise(ValidatedDirectIntProperty, ref _directInt, value);
		}

		public string ValidatedDirectString
		{
			get => _directString;
			set => SetAndRaise(ValidatedDirectStringProperty, ref _directString, value);
		}

		public int ValidatedStyledInt
		{
			get => GetValue(ValidatedStyledIntProperty);
			set => SetValue(ValidatedStyledIntProperty, value);
		}

		#endregion

		#region Methods

		protected override void UpdateDataValidation(
			PresentationProperty property,
			BindingValueType state,
			Exception error)
		{
			Notifications.Add(new(state, GetValue(property), error));
		}

		#endregion
	}

	private class Class2 : Class1
	{
		#region Constructors

		static Class2()
		{
			NonValidatedDirectIntProperty.OverrideMetadata<Class2>(
				new DirectPropertyMetadata<int>(enableDataValidation: true));
			NonValidatedStyledIntProperty.OverrideMetadata<Class2>(
				new StyledPropertyMetadata<int>(enableDataValidation: true));
		}

		#endregion
	}

	#endregion

	#region Records

	private record class Notification(BindingValueType type, object value, Exception error);

	#endregion
}