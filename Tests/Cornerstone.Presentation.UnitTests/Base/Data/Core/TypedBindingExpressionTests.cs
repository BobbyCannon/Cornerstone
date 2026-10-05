#region References

using System;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core;

[TestClass]
public partial class TypedBindingExpressionTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void CanBindReadonlyPropertyOneWayToSource()
	{
		var data = new ViewModel();
		var target = new SelectableTextBlock
		{
			DataContext = data,
			Text = "foobar",
			SelectionStart = 0,
			SelectionEnd = 3
		};

		CornerstoneTest.AreEqual("foo", target.SelectedText);

		var binding = CreateBinding(BindingMode.OneWayToSource);
		target.Bind(SelectableTextBlock.SelectedTextProperty, binding);

		CornerstoneTest.AreEqual("foo", data.StringValue);

		target.SelectionEnd = 4;

		// TODO: Uncomment when https://github.com/AvaloniaUI/Avalonia/issues/21461 fixed.
		//CornerstoneTest.AreEqual("foob", data.StringValue);
	}

	[PresentationTestMethod]
	public void CanBindStringToObject()
	{
		var log = string.Empty;
		using var logger = TestLogSink.Start((_, _, _, m, _) => log += m);
		var source = new ViewModel { StringValue = "Hello" };
		var binding = CreateBinding();
		var target = new TextBlock { DataContext = source };
		var expression = target.Bind(TextBlock.TagProperty, binding);

		CornerstoneTest.IsType<TypedBindingExpression<ViewModel, string>>(expression);
	}

	[PresentationTestMethod]
	public void DisposingBindingUnsubscribesFromSource()
	{
		var data = new ViewModel { StringValue = "foo" };
		var target = new TextBlock { DataContext = data };
		var binding = CreateBinding();
		var expression = target.Bind(TextBlock.TextProperty, binding);

		CornerstoneTest.AreEqual("foo", target.Text);
		CornerstoneTest.AreEqual(1, data.PropertyChangedSubscriptionCount);

		expression.Dispose();

		CornerstoneTest.AreEqual(0, data.PropertyChangedSubscriptionCount);

		// Source changes no longer propagate to the (now unbound) target.
		data.StringValue = "bar";
		CornerstoneTest.AreNotEqual("bar", target.Text);
	}

	[PresentationTestMethod]
	public void GetterExceptionDoesNotPropagateWhenSourceRaisesPropertyChanged()
	{
		// The untyped binding path swallows getter exceptions to avoid crashing the UI thread; the
		// typed path must do the same rather than letting them escape into the event handler.
		var data = new ViewModel { StringValue = "foo" };
		var target = CreateTarget(data, BindingMode.OneWay);

		CornerstoneTest.AreEqual("foo", target.Text);

		data.ThrowOnGet = true;

		var ex = Record.Exception(() => data.RaisePropertyChanged(nameof(ViewModel.StringValue)));

		CornerstoneTest.IsNull(ex);
	}

	[PresentationTestMethod]
	public void OneTimeBindingSetsTargetOnlyOnceIfDataContextDoesNotChange()
	{
		var data = new ViewModel { StringValue = "foo" };
		var target = CreateTarget(data, BindingMode.OneTime);

		CornerstoneTest.AreEqual("foo", target.Text);

		data.StringValue = "bar";

		CornerstoneTest.AreEqual("foo", target.Text);
	}

	[PresentationTestMethod]
	public void OneTimeBindingSetsTargetWhenDataContextChanges()
	{
		var data = new ViewModel { StringValue = "foo" };
		var target = CreateTarget(data, BindingMode.OneTime);

		CornerstoneTest.AreEqual("foo", target.Text);

		target.DataContext = new ViewModel { StringValue = "bar" };

		CornerstoneTest.AreEqual("bar", target.Text);
	}

	[PresentationTestMethod]
	public void OneTimeBindingWaitsForDataContext()
	{
		var target = CreateTarget(null, BindingMode.OneTime);

		CornerstoneTest.IsNull(target.Text);
	}

	[PresentationTestMethod]
	public void OneTimeBindingWaitsForDataContextWithMatchingPropertyName()
	{
		var data1 = new { Baz = "baz" };
		var data2 = new ViewModel { StringValue = "foo" };
		var target = CreateTarget(null, BindingMode.OneTime);

		target.DataContext = data1;
		CornerstoneTest.IsNull(target.Text);

		target.DataContext = data2;
		CornerstoneTest.AreEqual("foo", target.Text);

		data2.StringValue = "bar";
		CornerstoneTest.AreEqual("foo", target.Text);
	}

	[PresentationTestMethod]
	public void OneTimeBindingWaitsForDataContextWithMatchingPropertyType()
	{
		var data1 = new { StringValue = 1.5 };
		var data2 = new ViewModel { StringValue = "foo" };
		var target = CreateTarget(null, BindingMode.OneTime);

		target.DataContext = data1;
		CornerstoneTest.IsNull(target.Text);

		target.DataContext = data2;
		CornerstoneTest.AreEqual("foo", target.Text);

		data2.StringValue = "bar";
		CornerstoneTest.AreEqual("foo", target.Text);
	}

	[PresentationTestMethod]
	public void OneWayBindingShouldTrackDataContext()
	{
		var data1 = new ViewModel { StringValue = "Hello" };
		var data2 = new ViewModel { StringValue = "World" };
		var target = CreateTarget(data1, BindingMode.OneWay);

		CornerstoneTest.AreEqual("Hello", target.Text);

		target.DataContext = data2;

		CornerstoneTest.AreEqual("World", target.Text);
	}

	[PresentationTestMethod]
	public void OneWayBindingShouldTrackStringValue()
	{
		var data = new ViewModel { StringValue = "Hello" };
		var target = CreateTarget(data, BindingMode.OneWay);

		CornerstoneTest.AreEqual("Hello", target.Text);

		data.StringValue = "World";

		CornerstoneTest.AreEqual("World", target.Text);
	}

	// The name of this test makes no sense in English but keeping it as it matches the name of
	// the test in BindingExpressionTests.
	[PresentationTestMethod]
	public void OneWayBindingUpdatesTargetWhenChangesAndSourceRaisesPropertyChanged()
	{
		var data = new ViewModel { StringValue = "foo" };
		var target = CreateTarget(data, BindingMode.OneWay);

		CornerstoneTest.AreEqual("foo", target.Text);

		target.SetCurrentValue(TextBlock.TextProperty, "bar");

		CornerstoneTest.AreEqual("bar", target.Text);

		data.RaisePropertyChanged(nameof(data.StringValue));

		CornerstoneTest.AreEqual("foo", target.Text);
	}

	[PresentationTestMethod]
	[DataRow(null)]
	[DataRow("")]
	public void OneWayBindingUpdatesTargetWhenSourceRaisesPropertyChangedForAllProperties(
		string allPropertiesName)
	{
		// A null or empty PropertyName means "all properties changed" per the INotifyPropertyChanged
		// contract, so the binding must re-read its source value.
		var data = new ViewModel { StringValue = "foo" };
		var target = CreateTarget(data, BindingMode.OneWay);

		CornerstoneTest.AreEqual("foo", target.Text);

		data.SetStringValueWithoutNotification("bar");
		data.RaisePropertyChanged(allPropertiesName);

		CornerstoneTest.AreEqual("bar", target.Text);
	}

	[PresentationTestMethod]
	public void OneWayToSourceBindingDoesNotUpdateTargetWhenSourceChanges()
	{
		var data = new ViewModel();
		var target = CreateTarget(data, BindingMode.OneWayToSource);

		target.Text = "foo";
		CornerstoneTest.AreEqual("foo", data.StringValue);

		data.StringValue = "bar";
		CornerstoneTest.AreEqual("foo", target.Text);
	}

	[PresentationTestMethod]
	public void OneWayToSourceBindingUpdatesSourceWhenDataContextChanges()
	{
		var data1 = new ViewModel();
		var data2 = new ViewModel();
		var target = CreateTarget(data1, BindingMode.OneWayToSource);

		target.Text = "foo";
		CornerstoneTest.AreEqual("foo", data1.StringValue);

		target.DataContext = data2;
		CornerstoneTest.AreEqual("foo", data2.StringValue);
	}

	[PresentationTestMethod]
	public void OneWayToSourceBindingUpdatesSourceWhenTargetChanges()
	{
		var data = new ViewModel();
		var target = CreateTarget(data, BindingMode.OneWayToSource);

		CornerstoneTest.IsNull(data.StringValue);

		target.Text = "foo";
		CornerstoneTest.AreEqual("foo", data.StringValue);
	}

	[PresentationTestMethod]
	public void RebindingSamePropertyUnsubscribesPreviousBinding()
	{
		var data = new ViewModel { StringValue = "foo" };
		var target = new TextBlock { DataContext = data };

		target.Bind(TextBlock.TextProperty, CreateBinding());
		target.Bind(TextBlock.TextProperty, CreateBinding());

		// The first binding should have been disposed when the second was applied, leaving a
		// single subscription rather than two.
		CornerstoneTest.AreEqual(1, data.PropertyChangedSubscriptionCount);
	}

	[PresentationTestMethod]
	public void ShouldBindStringValue()
	{
		var data = new ViewModel { StringValue = "Hello" };
		var target = CreateTarget(data);

		CornerstoneTest.AreEqual("Hello", target.Text);
	}

	[PresentationTestMethod]
	public void ShouldNotProduceTypedBindingExpressionForNonStyledElementTarget()
	{
		// TypedBindingExpression only supports StyledElement targets; other PresentationObjects (e.g.
		// Application, which is an IDataContextProvider but not a StyledElement) must use the untyped
		// path rather than throwing at runtime.
		var binding = CreateBinding();
		var target = new NonStyledTarget { DataContext = new ViewModel { StringValue = "Hello" } };

		var expression = target.Bind(NonStyledTarget.ValueProperty, binding);

		CornerstoneTest.IsType<BindingExpression>(expression);
	}

	[PresentationTestMethod]
	public void ShouldNotProduceTypedBindingExpressionForReadOnlySourceInTwoWay()
	{
		// A read-only source property cannot be written back to in TwoWay/OneWayToSource modes, so
		// the untyped path (which fails silently) must be used instead.
		var propertyInfo = new ClrPropertyInfo<ViewModel, string>(
			nameof(ViewModel.StringValue),
			v => v.StringValue,
			null);
		var binding = CreateBinding(propertyInfo, BindingMode.TwoWay);
		var target = new TextBlock { DataContext = new ViewModel() };

		var expression = target.Bind(TextBlock.TextProperty, binding);

		CornerstoneTest.IsType<BindingExpression>(expression);
	}

	[PresentationTestMethod]
	public void ShouldNotProduceTypedBindingExpressionWhenBindingDataContext()
	{
		var log = string.Empty;
		using var logger = TestLogSink.Start((_, _, _, m, _) => log += m);
		var source = new ViewModel { StringValue = "Hello" };
		var binding = CreateBinding();
		var target = new TextBlock();
		var expression = target.Bind(TextBlock.DataContextProperty, binding);

		CornerstoneTest.IsType<BindingExpression>(expression);
	}

	[PresentationTestMethod]
	public void ShouldNotProduceTypedBindingExpressionWhenBindingStringToDouble()
	{
		var log = string.Empty;
		using var logger = TestLogSink.Start((_, _, _, m, _) => log += m);
		var source = new ViewModel { StringValue = "Hello" };
		var binding = CreateBinding();
		var target = new TextBlock { DataContext = source };
		var expression = target.Bind(TextBlock.OpacityProperty, binding);

		CornerstoneTest.IsType<BindingExpression>(expression);
	}

	[PresentationTestMethod]
	public void ShouldNotProduceTypedBindingExpressionWhenConverterIsPresent()
	{
		var binding = CreateBinding();
		binding.Converter = new FuncValueConverter<string, string>(s => s);

		var target = new TextBlock();
		var expression = target.Bind(TextBlock.TextProperty, binding);

		CornerstoneTest.IsType<BindingExpression>(expression);
	}

	[PresentationTestMethod]
	public void ShouldNotProduceTypedBindingExpressionWhenTargetTypeIsWiderInTwoWay()
	{
		// The source is a string but the target property is object. The forward assignment is valid
		// but writing an arbitrary object back to the string source could throw, so the untyped path
		// must be used.
		var source = new ViewModel { StringValue = "Hello" };
		var binding = CreateBinding(BindingMode.TwoWay);
		var target = new TextBlock { DataContext = source };

		var expression = target.Bind(TextBlock.TagProperty, binding);

		CornerstoneTest.IsType<BindingExpression>(expression);
	}

	[PresentationTestMethod]
	public void ShouldProduceTypedBindingExpression()
	{
		var binding = CreateBinding();
		var target = new TextBlock();

		BindAndAssert(target, binding);
	}

	[PresentationTestMethod]
	public void TwoWayBindingDoesNotEchoSourceChangeBackToSource()
	{
		var source = new ViewModel { StringValue = "Hello" };
		var target = CreateTarget(source, BindingMode.TwoWay);

		var before = source.StringValueSetCount;

		source.StringValue = "World"; // One setter call: this assignment.

		CornerstoneTest.AreEqual("World", target.Text);
		CornerstoneTest.AreEqual(before + 1, source.StringValueSetCount);
	}

	[PresentationTestMethod]
	public void TwoWayBindingDoesNotWriteBackToSourceOnAttach()
	{
		var source = new ViewModel { StringValue = "Hello" };
		var setsAfterConstruction = source.StringValueSetCount;

		var target = CreateTarget(source, BindingMode.TwoWay);

		CornerstoneTest.AreEqual("Hello", target.Text);

		// Pushing the source value to the target must not echo it straight back to the source.
		CornerstoneTest.AreEqual(setsAfterConstruction, source.StringValueSetCount);
	}

	[PresentationTestMethod]
	public void TwoWayBindingWritesValueToSource()
	{
		var source = new ViewModel { StringValue = "Hello" };
		var target = CreateTarget(source, BindingMode.TwoWay);

		CornerstoneTest.AreEqual("Hello", target.Text);

		source.StringValue = "World";

		CornerstoneTest.AreEqual("World", target.Text);

		target.Text = "Goodbye";

		CornerstoneTest.AreEqual("Goodbye", source.StringValue);
	}

	private static TypedBindingExpression<ViewModel, string> BindAndAssert(StyledElement target, BindingBase binding)
	{
		var expression = target.Bind(TextBlock.TextProperty, binding);
		return CornerstoneTest.IsType<TypedBindingExpression<ViewModel, string>>(expression);
	}

	private static CompiledBinding CreateBinding(BindingMode mode = BindingMode.OneWay)
	{
		var propertyInfo = new ClrPropertyInfo<ViewModel, string>(
			nameof(ViewModel.StringValue),
			v => v.StringValue,
			(o, v) => o.StringValue = v);
		return CreateBinding(propertyInfo, mode);
	}

	private static CompiledBinding CreateBinding(
		IPropertyInfo<ViewModel, string> propertyInfo,
		BindingMode mode = BindingMode.OneWay)
	{
		var path = new CompiledBindingPathBuilder().Property(
			propertyInfo,
			PropertyInfoAccessorFactory.CreateInpcPropertyAccessor,
			false).Build();
		return new CompiledBinding(path) { Mode = mode };
	}

	private static TextBlock CreateTarget(ViewModel data, BindingMode mode = BindingMode.OneWay)
	{
		var result = new TextBlock { DataContext = data };
		var binding = CreateBinding(mode);
		BindAndAssert(result, binding);
		return result;
	}

	#endregion

	#region Classes

	private class NonStyledTarget : PresentationObject, IDataContextProvider
	{
		#region Fields

		public static readonly StyledProperty<object> DataContextProperty =
			StyledElement.DataContextProperty.AddOwner<NonStyledTarget>();

		public static readonly StyledProperty<string> ValueProperty =
			PresentationProperty.Register<NonStyledTarget, string>(nameof(Value));

		#endregion

		#region Properties

		public object DataContext
		{
			get => GetValue(DataContextProperty);
			set => SetValue(DataContextProperty, value);
		}

		public string Value
		{
			get => GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		#endregion
	}

	private class ViewModel : NotifyingBase
	{
		#region Fields

		private double _doubleValue;
		private string _stringValue;

		#endregion

		#region Properties

		public double DoubleValue
		{
			get => _doubleValue;
			set => SetField(ref _doubleValue, value);
		}

		public string StringValue
		{
			get
			{
				if (ThrowOnGet)
				{
					throw new InvalidOperationException("Getter failed.");
				}
				return _stringValue;
			}
			set
			{
				++StringValueSetCount;
				SetField(ref _stringValue, value);
			}
		}

		// Counts every setter invocation so tests can assert the binding doesn't write spurious
		// values back to the source. PropertyChanged is only raised on a real change.
		public int StringValueSetCount { get; private set; }

		// When set, the getter throws so tests can verify getter exceptions don't escape the
		// binding's PropertyChanged handler.
		public bool ThrowOnGet { get; set; }

		#endregion

		#region Methods

		// Mutates the backing field without raising PropertyChanged, so tests can then raise an
		// "all properties changed" notification (null/empty name) and observe the binding react.
		public void SetStringValueWithoutNotification(string value)
		{
			_stringValue = value;
		}

		#endregion
	}

	#endregion
}