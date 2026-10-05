#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Core.Plugins;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core;

public partial class BindingExpressionTests
{
	#region Methods

	[PresentationTestMethod]
	public void ConversionErrorIsClearedWhenValueBecomesValidOneWayToSource()
	{
		// Issue #15378.
		var data = new ViewModel();
		var target = CreateTargetWithSource(
			data,
			o => o.DoubleValue,
			TargetClass.ObjectProperty,
			enableDataValidation: true,
			mode: BindingMode.OneWayToSource);

		target.Object = 5.0;

		CornerstoneTest.AreEqual(5.0, data.DoubleValue);
		AssertNoError(target, TargetClass.ObjectProperty);

		target.Object = null;

		AssertBindingError(
			target,
			TargetClass.ObjectProperty,
			new InvalidCastException("Could not convert '(null)' (null) to System.Double."),
			BindingErrorType.DataValidationError);

		target.Object = 5.0;

		CornerstoneTest.AreEqual(5.0, data.DoubleValue);
		AssertNoError(target, TargetClass.ObjectProperty);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ConversionErrorsUpdateDataValidationWhenWritingToSource()
	{
		var data = new ViewModel { DoubleValue = 5.6 };
		var target = CreateTargetWithSource(
			data,
			o => o.DoubleValue,
			enableDataValidation: true,
			mode: BindingMode.TwoWay,
			targetProperty: TargetClass.TagProperty);

		// Can write a double value.
		target.Tag = 1.2;

		CornerstoneTest.AreEqual(1.2, data.DoubleValue);
		AssertNoError(target, TargetClass.StringProperty);

		// Can write a string value and it gets converted to double.
		target.Tag = "3.4";

		CornerstoneTest.AreEqual(3.4, data.DoubleValue);
		AssertNoError(target, TargetClass.StringProperty);

		// An invalid string value should result in an error. Not sure why this is considered
		// a data validation error rather than a binding error, but preserving semantics.
		target.Tag = "bar";

		CornerstoneTest.AreEqual(3.4, data.DoubleValue);
		AssertBindingError(
			target,
			TargetClass.TagProperty,
			new InvalidCastException("Could not convert 'bar' (System.String) to System.Double."),
			BindingErrorType.DataValidationError);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void DataAnnotationsValidationUpdatesDataValidationWhenWritingToSourceOneWayToSource()
	{
		// Issue #8235: validation attributes should be displayed for OneWayToSource bindings.
		if (!BindingPlugins.DataValidators.Any(x => x is DataAnnotationsValidationPlugin))
		{
			BindingPlugins.DataValidators.Insert(0, new DataAnnotationsValidationPlugin());
		}

		var data = new DataAnnotationsViewModel();
		var target = CreateTargetWithSource(
			data,
			o => o.MaxLengthString,
			enableDataValidation: true,
			mode: BindingMode.OneWayToSource);

		target.String = "1234";

		CornerstoneTest.AreEqual("1234", data.MaxLengthString);
		AssertNoError(target, TargetClass.StringProperty);

		target.String = "123456";

		CornerstoneTest.AreEqual("123456", data.MaxLengthString);
		AssertBindingError(
			target,
			TargetClass.StringProperty,
			new DataValidationException("Too long!"),
			BindingErrorType.DataValidationError);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void DoesNotSubscribeToIndeiOfIntermediateObjectInChain()
	{
		var data = new IndeiContainerViewModel { Inner = new() };

		var target = CreateTargetWithSource(
			data,
			o => o.Inner!.MustBePositive,
			enableDataValidation: true,
			mode: BindingMode.TwoWay);

		// We may want to change this but I've never seen an example of data validation on an
		// intermediate object in a chain so for the moment I'm not sure what the result of 
		// validating such a thing should look like.
		CornerstoneTest.AreEqual(0, data.ErrorsChangedSubscriptionCount);
		CornerstoneTest.AreEqual(1, data.Inner.ErrorsChangedSubscriptionCount);
	}

	[PresentationTestMethod]
	public void HandlesIndeiAndDataAnnotationsOnSameClass()
	{
		// Issue #15201
		var data = new IndeiDataAnnotationsViewModel();
		var target = CreateTargetWithSource(
			data,
			o => o.RequiredString,
			enableDataValidation: true);

		AssertBindingError(
			target,
			TargetClass.StringProperty,
			new DataValidationException("String is required!"),
			BindingErrorType.DataValidationError);
	}

	[PresentationTestMethod]
	public void IndeiValidationDoesNotSubscribeWhenDataValidationNotEnabled()
	{
		var data = new IndeiViewModel { MustBePositive = 5 };
		var target = CreateTargetWithSource(
			data,
			o => o.MustBePositive,
			enableDataValidation: false,
			mode: BindingMode.TwoWay);

		CornerstoneTest.AreEqual(0, data.ErrorsChangedSubscriptionCount);
	}

	[PresentationTestMethod]
	public void IndeiValidationSubscribesAndUnsubscribes()
	{
		var data = new IndeiViewModel { MustBePositive = 5 };
		var (target, expression) = CreateTargetAndExpression<IndeiViewModel, int>(
			o => o.MustBePositive,
			enableDataValidation: true,
			mode: BindingMode.TwoWay,
			source: data);

		CornerstoneTest.AreEqual(1, data.ErrorsChangedSubscriptionCount);

		expression.Dispose();

		CornerstoneTest.AreEqual(0, data.ErrorsChangedSubscriptionCount);
	}

	[PresentationTestMethod]
	public void IndeiValidationUpdatesDataValidationWhenWritingToSource()
	{
		var data = new IndeiViewModel();
		var target = CreateTargetWithSource(
			data,
			o => o.MustBePositive,
			enableDataValidation: true,
			mode: BindingMode.TwoWay);

		CornerstoneTest.AreEqual(0, target.Int);
		CornerstoneTest.AreEqual(0, data.MustBePositive);
		AssertNoError(target, TargetClass.IntProperty);

		target.Int = 5;

		CornerstoneTest.AreEqual(5, target.Int);
		CornerstoneTest.AreEqual(5, data.MustBePositive);
		AssertNoError(target, TargetClass.IntProperty);

		target.Int = -5;

		CornerstoneTest.AreEqual(-5, target.Int);
		CornerstoneTest.AreEqual(-5, data.MustBePositive);
		AssertBindingError(target, TargetClass.IntProperty, new DataValidationException("Must be positive"), BindingErrorType.DataValidationError);

		target.Int = 5;

		CornerstoneTest.AreEqual(5, target.Int);
		CornerstoneTest.AreEqual(5, data.MustBePositive);
		AssertNoError(target, TargetClass.IntProperty);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void IndeiValidationUpdatesDataValidationWhenWritingToSourceOneWayToSource()
	{
		// Issue #8235: validation errors should be displayed for OneWayToSource bindings.
		var data = new IndeiViewModel();
		var target = CreateTargetWithSource(
			data,
			o => o.MustBePositive,
			enableDataValidation: true,
			mode: BindingMode.OneWayToSource);

		CornerstoneTest.AreEqual(0, data.MustBePositive);
		AssertNoError(target, TargetClass.IntProperty);

		target.Int = 5;

		CornerstoneTest.AreEqual(5, data.MustBePositive);
		AssertNoError(target, TargetClass.IntProperty);

		target.Int = -5;

		CornerstoneTest.AreEqual(-5, data.MustBePositive);
		AssertBindingError(target, TargetClass.IntProperty, new DataValidationException("Must be positive"), BindingErrorType.DataValidationError);

		target.Int = 5;

		CornerstoneTest.AreEqual(5, data.MustBePositive);
		AssertNoError(target, TargetClass.IntProperty);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void InvalidDoubleStringShouldRevertToFallbackValue()
	{
		var data = new ViewModel { StringValue = "foo" };
		var target = CreateTargetWithSource(
			data,
			o => o.StringValue,
			enableDataValidation: true,
			fallbackValue: 42.0,
			targetProperty: TargetClass.DoubleProperty);

		CornerstoneTest.AreEqual(42.0, target.Double);
		AssertBindingError(
			target,
			TargetClass.DoubleProperty,
			new InvalidCastException("Could not convert 'foo' (System.String) to 'System.Double'."),
			BindingErrorType.Error);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void InvalidDoubleStringShouldUpdateDataValidation()
	{
		var data = new ViewModel { StringValue = "foo" };
		var target = CreateTargetWithSource(
			data,
			o => o.StringValue,
			enableDataValidation: true,
			targetProperty: TargetClass.DoubleProperty);

		AssertBindingError(
			target,
			TargetClass.DoubleProperty,
			new InvalidCastException("Could not convert 'foo' (System.String) to 'System.Double'."),
			BindingErrorType.Error);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void NullValueInPathShouldUpdateDataValidation()
	{
		var data = new { Foo = default(ViewModel) };
		var target = CreateTargetWithSource(
			data,
			o => o.Foo!.StringValue!.Length,
			enableDataValidation: true);

		AssertBindingError(
			target,
			TargetClass.IntProperty,
			new BindingChainException("Value is null.", "Foo.StringValue.Length", "Foo"),
			BindingErrorType.Error);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void RootNullShouldUpdateDataValidation()
	{
		var target = CreateTargetWithSource<ViewModel, string>(
			null,
			o => o!.StringValue,
			enableDataValidation: true);

		AssertBindingError(
			target,
			TargetClass.StringProperty,
			new BindingChainException("Binding Source is null.", "StringValue", "(source)"),
			BindingErrorType.Error);
	}

	[PresentationTestMethod]
	public void SetterExceptionDoesNotCauseDataValidationErrorWhenDataValidationNotEnabled()
	{
		var data = new ExceptionViewModel { MustBePositive = 5 };

		var target = CreateTargetWithSource(
			data,
			o => o.MustBePositive,
			enableDataValidation: false,
			mode: BindingMode.TwoWay);

		target.Int = -5;

		// TODO: Should this be 5
		CornerstoneTest.AreEqual(-5, target.Int);

		CornerstoneTest.AreEqual(5, data.MustBePositive);
		AssertNoError(target, TargetClass.IntProperty);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void SetterExceptionErrorIsClearedWhenRevertingToSameValidValue()
	{
		// Issue #20534: When a setter throws on an invalid value and the user
		// reverts to the same valid value that was last successfully set, the
		// validation error should be cleared.
		var data = new ExceptionViewModel { MustBePositive = 5 };

		var target = CreateTargetWithSource(
			data,
			o => o.MustBePositive,
			enableDataValidation: true,
			mode: BindingMode.TwoWay);

		// Step 1: Set a valid value.
		target.Int = 10;
		CornerstoneTest.AreEqual(10, data.MustBePositive);
		AssertNoError(target, TargetClass.IntProperty);

		// Step 2: Set an invalid value — setter throws, error appears.
		target.Int = -5;
		CornerstoneTest.AreEqual(10, data.MustBePositive);
		AssertBindingError(
			target,
			TargetClass.IntProperty,
			new ArgumentOutOfRangeException("value"),
			BindingErrorType.DataValidationError);

		// Step 3: Revert to the same valid value (10). The error must clear.
		target.Int = 10;
		CornerstoneTest.AreEqual(10, data.MustBePositive);
		AssertNoError(target, TargetClass.IntProperty);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void SetterExceptionUpdatesDataValidation()
	{
		var data = new ExceptionViewModel { MustBePositive = 5 };

		var target = CreateTargetWithSource(
			data,
			o => o.MustBePositive,
			enableDataValidation: true,
			mode: BindingMode.TwoWay);

		target.Int = -5;

		// TODO: Should this be 5
		CornerstoneTest.AreEqual(-5, target.Int);

		CornerstoneTest.AreEqual(5, data.MustBePositive);
		AssertBindingError(
			target,
			TargetClass.IntProperty,
			new ArgumentOutOfRangeException("value"),
			BindingErrorType.DataValidationError);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void SettingValidValueShouldClearBindingError()
	{
		var data = new ViewModel { DoubleValue = 5.6 };
		var target = CreateTargetWithSource(
			data,
			o => o.DoubleValue,
			enableDataValidation: true,
			mode: BindingMode.TwoWay,
			targetProperty: TargetClass.StringProperty);

		target.String = "5.6";
		target.String = "5.6a";
		target.String = "5.6";

		AssertNoError(target, TargetClass.StringProperty);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void UpdatesDataValidationForNullValueInPropertyChain()
	{
		var data = new IndeiContainerViewModel();
		var target = CreateTargetWithSource(
			data,
			o => o.Inner!.MustBePositive,
			enableDataValidation: true,
			mode: BindingMode.TwoWay);

		AssertBindingError(
			target,
			TargetClass.IntProperty,
			new BindingChainException("Value is null.", "Inner.MustBePositive", "Inner"),
			BindingErrorType.Error);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void UpdatesDataValidationForRequiredDataAnnotation()
	{
		if (!BindingPlugins.DataValidators.Any(x => x is DataAnnotationsValidationPlugin))
		{
			BindingPlugins.DataValidators.Insert(0, new DataAnnotationsValidationPlugin());
		}

		var data = new DataAnnotationsViewModel();
		var target = CreateTargetWithSource(
			data,
			o => o.RequiredString,
			enableDataValidation: true);

		AssertBindingError(
			target,
			TargetClass.StringProperty,
			new DataValidationException("String is required!"),
			BindingErrorType.DataValidationError);
	}

	private static void AssertBindingError(
		TargetClass target,
		PresentationProperty property,
		Exception expectedException,
		BindingErrorType errorType)
	{
		CornerstoneTest.IsTrue(target.BindingNotifications.TryGetValue(property, out var notification));
		CornerstoneTest.AreEqual(errorType, notification.ErrorType);
		CornerstoneTest.IsNotNull(notification.Error);
		CornerstoneTest.IsType(expectedException.GetType(), notification.Error);
		CornerstoneTest.AreEqual(expectedException.Message, notification.Error.Message);
	}

	private static void AssertNoError(TargetClass target, PresentationProperty property)
	{
		CornerstoneTest.IsFalse(target.BindingNotifications.TryGetValue(property, out var notification));
	}

	#endregion

	#region Classes

	public class ExceptionViewModel : NotifyingBase
	{
		#region Fields

		private int _mustBePositive;

		#endregion

		#region Properties

		public int MustBePositive
		{
			get => _mustBePositive;
			set
			{
				if (value <= 0)
				{
					throw new ArgumentOutOfRangeException(nameof(value));
				}

				_mustBePositive = value;
				RaisePropertyChanged();
			}
		}

		#endregion
	}

	private class DataAnnotationsViewModel : NotifyingBase
	{
		#region Fields

		private string _maxLengthString;
		private string _requiredString;

		#endregion

		#region Properties

		[MaxLength(5, ErrorMessage = "Too long!")]
		public string MaxLengthString
		{
			get => _maxLengthString;
			set
			{
				_maxLengthString = value;
				RaisePropertyChanged();
			}
		}

		[Required(ErrorMessage = "String is required!")]
		public string RequiredString
		{
			get => _requiredString;
			set
			{
				_requiredString = value;
				RaisePropertyChanged();
			}
		}

		#endregion
	}

	private class IndeiContainerViewModel : IndeiBase
	{
		#region Fields

		private IndeiViewModel _inner;

		#endregion

		#region Properties

		public override bool HasErrors => false;

		public IndeiViewModel Inner
		{
			get => _inner;
			set
			{
				_inner = value;
				RaisePropertyChanged();
			}
		}

		#endregion

		#region Methods

		public override IEnumerable GetErrors(string propertyName)
		{
			return Array.Empty<string>();
		}

		#endregion
	}

	private class IndeiDataAnnotationsViewModel : IndeiBase
	{
		#region Fields

		private string _requiredString;

		#endregion

		#region Properties

		public override bool HasErrors => RequiredString is null;

		[Required(ErrorMessage = "String is required!")]
		public string RequiredString
		{
			get => _requiredString;
			set
			{
				_requiredString = value;
				RaisePropertyChanged();
			}
		}

		#endregion

		#region Methods

		public override IEnumerable GetErrors(string propertyName)
		{
			if ((propertyName == nameof(RequiredString)) && RequiredString is null)
			{
				return new[] { "String is required!" };
			}

			return Array.Empty<string>();
		}

		#endregion
	}

	private class IndeiViewModel : IndeiBase
	{
		#region Fields

		private readonly Dictionary<string, IList<string>> _errors = new();
		private int _mustBePositive;

		#endregion

		#region Properties

		public override bool HasErrors => _mustBePositive >= 0;

		public int MustBePositive
		{
			get => _mustBePositive;
			set
			{
				_mustBePositive = value;
				RaisePropertyChanged();

				if (value >= 0)
				{
					_errors.Remove(nameof(MustBePositive));
					RaiseErrorsChanged(nameof(MustBePositive));
				}
				else
				{
					_errors[nameof(MustBePositive)] = new[] { "Must be positive" };
					RaiseErrorsChanged(nameof(MustBePositive));
				}
			}
		}

		#endregion

		#region Methods

		public override IEnumerable GetErrors(string propertyName)
		{
			if (propertyName is not null && _errors.TryGetValue(propertyName, out var result))
			{
				return result;
			}

			return Array.Empty<string>();
		}

		#endregion
	}

	#endregion
}