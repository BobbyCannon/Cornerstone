#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Data;

[TestClass]
public class BindingTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void BindingCanResolvePropertyFromIReflectableTypeType()
	{
		var source = new DynamicReflectableType { ["Foo"] = "foo" };
		var target = new TwoWayBindingTest { DataContext = source };
		var binding = new Binding
		{
			Path = "Foo"
		};

		target.Bind(TwoWayBindingTest.TwoWayProperty, binding);

		CornerstoneTest.AreEqual("foo", target.TwoWay);
		source["Foo"] = "bar";
		CornerstoneTest.AreEqual("bar", target.TwoWay);
		target.TwoWay = "baz";
		CornerstoneTest.AreEqual("baz", source["Foo"]);
	}

	[PresentationTestMethod]
	public void BindingNonNullableValueTypeToNullRevertsToDefaultValue()
	{
		var source = new NullableValuesViewModel { NullableDouble = 42 };
		var target = new StyledPropertyClass();
		var binding = new Binding(nameof(source.NullableDouble)) { Source = source };

		target.Bind(StyledPropertyClass.DoubleValueProperty, binding);
		CornerstoneTest.AreEqual(42, target.DoubleValue);

		source.NullableDouble = null;

		CornerstoneTest.AreEqual(12.3, target.DoubleValue);
	}

	[PresentationTestMethod]
	public void BindingNullableValueTypeToNullSetsValueToNull()
	{
		var source = new NullableValuesViewModel { NullableDouble = 42 };
		var target = new StyledPropertyClass();
		var binding = new Binding(nameof(source.NullableDouble)) { Source = source };

		target.Bind(StyledPropertyClass.NullableDoubleProperty, binding);
		CornerstoneTest.AreEqual(42, target.NullableDouble);

		source.NullableDouble = null;

		CornerstoneTest.IsNull(target.NullableDouble);
	}

	[PresentationTestMethod]
	public void BindingProducingDefaultValueShouldResultInCorrectPriority()
	{
		var defaultValue = StyledPropertyClass.NullableDoubleProperty.GetDefaultValue(typeof(StyledPropertyClass));

		var vm = new NullableValuesViewModel { NullableDouble = defaultValue };
		var target = new StyledPropertyClass();

		target.Bind(StyledPropertyClass.NullableDoubleProperty, new Binding(nameof(NullableValuesViewModel.NullableDouble)) { Source = vm });

		CornerstoneTest.AreEqual(BindingPriority.LocalValue, target.GetDiagnosticInternal(StyledPropertyClass.NullableDoubleProperty).Priority);
		CornerstoneTest.AreEqual(defaultValue, target.GetValue(StyledPropertyClass.NullableDoubleProperty));
	}

	[PresentationTestMethod]
	public void BindingToTypesShouldWork()
	{
		var type = typeof(string);
		var textBlock = new TextBlock { DataContext = type };
		using (textBlock.Bind(TextBlock.TextProperty, new Binding("Name")))
		{
			CornerstoneTest.AreEqual("String", textBlock.Text);
		}
		;
	}

	[PresentationTestMethod]
	public void CombinedOneTimeAndOneWayToSourceBindingsShouldReleaseSubscriptions()
	{
		var target1 = new TextBlock();
		var target2 = new TextBlock();
		var root = new Panel { Children = { target1, target2 } };
		var source = new Source { Foo = "foo" };

		using (target1.Bind(TextBlock.TextProperty, new Binding("Foo") { Mode = BindingMode.OneTime }))
		using (target2.Bind(TextBlock.TextProperty, new Binding("Foo") { Mode = BindingMode.OneWayToSource }))
		{
			root.DataContext = source;
		}

		// Forces WeakEvent compact
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

		CornerstoneTest.AreEqual(0, source.SubscriberCount);
	}

	[PresentationTestMethod]
	public void DataContextBindingShouldProduceCorrectResults()
	{
		var viewModel = new { Foo = "bar" };
		var root = new Decorator
		{
			DataContext = viewModel
		};

		var child = new Control();
		var values = new List<object>();

		child.GetObservable(Control.DataContextProperty).Subscribe(x => values.Add(x));
		child.Bind(Control.DataContextProperty, new Binding("Foo"));

		// When binding to DataContext and the source isn't found, the binding should produce
		// null rather than UnsetValue in order to not propagate incorrect DataContexts from
		// parent controls while things are being set up. This logic is implemented in 
		// `UntypedBindingExpressionBase.PublishValue`.
		CornerstoneTest.IsTrue(child.IsSet(Control.DataContextProperty));

		root.Child = child;

		CornerstoneTest.AreEqual(new[] { null, "bar" }, values);
	}

	[PresentationTestMethod]
	public void DataContextBindingShouldTrackParent()
	{
		var parent = new Decorator
		{
			DataContext = new { Foo = "foo" }
		};

		var child = new Control();

		var binding = new Binding
		{
			Path = "Foo"
		};

		child.Bind(Control.DataContextProperty, binding);

		CornerstoneTest.IsNull(child.DataContext);
		parent.Child = child;
		CornerstoneTest.AreEqual("foo", child.DataContext);
	}

	[PresentationTestMethod]
	public void DataContextBindingShouldUseParentDataContext()
	{
		var parentDataContext = new StubHeadered();
		parentDataContext.Header = "Foo";

		var parent = new Decorator
		{
			Child = new Control(),
			DataContext = parentDataContext
		};

		var binding = new Binding
		{
			Path = "Header"
		};

		parent.Child.Bind(Control.DataContextProperty, binding);

		CornerstoneTest.AreEqual("Foo", parent.Child.DataContext);

		parentDataContext = new StubHeadered();
		parentDataContext.Header = "Bar";
		parent.DataContext = parentDataContext;
		CornerstoneTest.AreEqual("Bar", parent.Child.DataContext);
	}

	[PresentationTestMethod]
	public void DefaultBindingModeShouldBeUsed()
	{
		var source = new Source { Foo = "foo" };
		var target = new TwoWayBindingTest { DataContext = source };
		var binding = new Binding
		{
			Path = "Foo"
		};

		target.Bind(TwoWayBindingTest.TwoWayProperty, binding);

		CornerstoneTest.AreEqual("foo", target.TwoWay);
		source.Foo = "bar";
		CornerstoneTest.AreEqual("bar", target.TwoWay);
		target.TwoWay = "baz";
		CornerstoneTest.AreEqual("baz", source.Foo);
	}

	[PresentationTestMethod]
	public void DotPathShouldBindToDataContext()
	{
		var target = new TextBlock { DataContext = "foo" };
		var binding = new Binding { Path = "." };

		target.Bind(TextBlock.TextProperty, binding);

		CornerstoneTest.AreEqual("foo", target.Text);
	}

	[PresentationTestMethod]
	public void EmptyPathShouldBindToDataContext()
	{
		var target = new TextBlock { DataContext = "foo" };
		var binding = new Binding { Path = string.Empty };

		target.Bind(TextBlock.TextProperty, binding);

		CornerstoneTest.AreEqual("foo", target.Text);
	}

	[PresentationTestMethod]
	public void NullPathShouldBindToDataContext()
	{
		var target = new TextBlock { DataContext = "foo" };
		var binding = new Binding();

		target.Bind(TextBlock.TextProperty, binding);

		CornerstoneTest.AreEqual("foo", target.Text);
	}

	[PresentationTestMethod]
	public void OneTimeBindingShouldBeSetUp()
	{
		var source = new Source { Foo = "foo" };
		var target = new TextBlock { DataContext = source };
		var binding = new Binding
		{
			Path = "Foo",
			Mode = BindingMode.OneTime
		};

		target.Bind(TextBox.TextProperty, binding);

		CornerstoneTest.AreEqual("foo", target.Text);
		source.Foo = "bar";
		CornerstoneTest.AreEqual("foo", target.Text);
		target.Text = "baz";
		CornerstoneTest.AreEqual("bar", source.Foo);
	}

	[PresentationTestMethod]
	public void OneWayBindingShouldBeSetUp()
	{
		var source = new Source { Foo = "foo" };
		var target = new TextBlock { DataContext = source };
		var binding = new Binding
		{
			Path = "Foo",
			Mode = BindingMode.OneWay
		};

		target.Bind(TextBox.TextProperty, binding);

		CornerstoneTest.AreEqual("foo", target.Text);
		source.Foo = "bar";
		CornerstoneTest.AreEqual("bar", target.Text);
		target.Text = "baz";
		CornerstoneTest.AreEqual("bar", source.Foo);
	}

	[PresentationTestMethod]
	public void OneWayToSourceBindingDoesNotOverrideTwoWayBinding()
	{
		// Issue #2983
		var target1 = new TextBlock();
		var target2 = new TextBlock { Text = "OneWayToSource" };
		var source = new Source { Foo = "foo" };
		var root = new Panel
		{
			DataContext = source,
			Children = { target1, target2 }
		};

		target1.Bind(TextBlock.TextProperty, new Binding("Foo") { Mode = BindingMode.TwoWay });
		target2.Bind(TextBlock.TextProperty, new Binding("Foo") { Mode = BindingMode.OneWayToSource });

		CornerstoneTest.AreEqual("OneWayToSource", source.Foo);

		target1.Text = "TwoWay";

		CornerstoneTest.AreEqual("TwoWay", source.Foo);
	}

	[PresentationTestMethod]
	public void OneWayToSourceBindingShouldBeSetUp()
	{
		var source = new Source { Foo = "foo" };
		var target = new TextBlock { DataContext = source, Text = "bar" };
		var binding = new Binding
		{
			Path = "Foo",
			Mode = BindingMode.OneWayToSource
		};

		target.Bind(TextBox.TextProperty, binding);

		CornerstoneTest.AreEqual("bar", source.Foo);
		target.Text = "baz";
		CornerstoneTest.AreEqual("baz", source.Foo);
		source.Foo = "quz";
		CornerstoneTest.AreEqual("baz", target.Text);
	}

	[PresentationTestMethod]
	public void OneWayToSourceBindingShouldNotStackOverflowWithNullValue()
	{
		// Issue #2912
		var target = new TextBlock { Text = null };
		var binding = new Binding
		{
			Path = "Foo",
			Mode = BindingMode.OneWayToSource
		};

		target.Bind(TextBox.TextProperty, binding);

		var source = new Source { Foo = "foo" };
		target.DataContext = source;

		CornerstoneTest.IsNull(source.Foo);

		// When running tests under NCrunch, NCrunch replaces the standard StackOverflowException
		// with its own, which will be caught by our code. Detect the stackoverflow anyway, by
		// making sure the target property was only set once.
		CornerstoneTest.AreEqual(2, source.FooSetCount);
	}

	[PresentationTestMethod]
	public void OneWayToSourceBindingShouldReactToDataContextChanged()
	{
		var target = new TextBlock { Text = "bar" };
		var binding = new Binding
		{
			Path = "Foo",
			Mode = BindingMode.OneWayToSource
		};

		target.Bind(TextBox.TextProperty, binding);

		var source = new Source { Foo = "foo" };
		target.DataContext = source;

		CornerstoneTest.AreEqual("bar", source.Foo);
		target.Text = "baz";
		CornerstoneTest.AreEqual("baz", source.Foo);
		source.Foo = "quz";
		CornerstoneTest.AreEqual("baz", target.Text);
	}

	[PresentationTestMethod]
	public void PresentationObjectthisOperatorAcceptsBinding()
	{
		var target = new ContentControl
		{
			DataContext = new { Foo = "foo" }
		};

		target[!ContentControl.ContentProperty] = new Binding("Foo");

		CornerstoneTest.AreEqual("foo", target.Content);
	}

	[PresentationTestMethod]
	public void SetValueShouldNotCauseStackOverflowAndHaveCorrectValues()
	{
		var viewModel = new TestStackOverflowViewModel
		{
			Value = 50
		};

		var target = new DirectPropertyClass();

		target.Bind(DirectPropertyClass.DoubleValueProperty, new Binding("Value")
		{
			Mode = BindingMode.TwoWay,
			Source = viewModel
		});

		var child = new DirectPropertyClass();

		child.Bind(DirectPropertyClass.DoubleValueProperty,
			new Binding("DoubleValue")
			{
				Mode = BindingMode.TwoWay,
				Source = target
			});

		CornerstoneTest.AreEqual(1, viewModel.SetterInvokedCount);

		//here in real life stack overflow exception is thrown issue #855 and #824
		target.DoubleValue = 51.001;

		CornerstoneTest.AreEqual(2, viewModel.SetterInvokedCount);

		double expected = 51;

		CornerstoneTest.AreEqual(expected, viewModel.Value);
		CornerstoneTest.AreEqual(expected, target.DoubleValue);
		CornerstoneTest.AreEqual(expected, child.DoubleValue);
	}

	/// <summary>
	/// Tests a problem discovered with ListBox with selection.
	/// </summary>
	/// <remarks>
	/// - Items is bound to DataContext first, followed by say SelectedIndex
	/// - When the ListBox is removed from the logical tree, DataContext becomes null (as it's
	/// inherited)
	/// - This changes Items to null, which changes SelectedIndex to null as there are no
	/// longer any items
	/// - However, the news that DataContext is now null hasn't yet reached the SelectedIndex
	/// binding and so the unselection is sent back to the ViewModel
	/// </remarks>
	[PresentationTestMethod]
	public void ShouldNotWriteToOldDataContext()
	{
		var vm = new OldDataContextViewModel();
		var target = new OldDataContextTest();

		var fooBinding = new Binding
		{
			Path = "Foo",
			Mode = BindingMode.TwoWay
		};

		var barBinding = new Binding
		{
			Path = "Bar",
			Mode = BindingMode.TwoWay
		};

		// Bind Foo and Bar to the VM.
		target.Bind(OldDataContextTest.FooProperty, fooBinding);
		target.Bind(OldDataContextTest.BarProperty, barBinding);
		target.DataContext = vm;

		// Make sure the control's Foo and Bar properties are read from the VM
		CornerstoneTest.AreEqual(1, target.GetValue(OldDataContextTest.FooProperty));
		CornerstoneTest.AreEqual(2, target.GetValue(OldDataContextTest.BarProperty));

		// Set DataContext to null.
		target.DataContext = null;

		// Foo and Bar are no longer bound so they return 0, their default value.
		CornerstoneTest.AreEqual(0, target.GetValue(OldDataContextTest.FooProperty));
		CornerstoneTest.AreEqual(0, target.GetValue(OldDataContextTest.BarProperty));

		// The problem was here - DataContext is now null, setting Foo to 0. Bar is bound to 
		// Foo so Bar also gets set to 0. However the Bar binding still had a reference to
		// the VM and so vm.Bar was set to 0 erroneously.
		CornerstoneTest.AreEqual(1, vm.Foo);
		CornerstoneTest.AreEqual(2, vm.Bar);
	}

	[PresentationTestMethod]
	public void ShouldReturnFallbackValueWhenInvalidSourceType()
	{
		var target = new ProgressBar();
		var source = new Source { Foo = "foo" };
		var binding = new Binding
		{
			Source = source,
			Path = "Foo",
			FallbackValue = 42
		};

		target.Bind(ProgressBar.ValueProperty, binding);

		CornerstoneTest.AreEqual(42, target.Value);
	}

	[PresentationTestMethod]
	public void ShouldReturnFallbackValueWhenPathNotResolved()
	{
		var target = new TextBlock();
		var source = new Source();
		var binding = new Binding
		{
			Source = source,
			Path = "BadPath",
			FallbackValue = "foofallback"
		};

		target.Bind(TextBlock.TextProperty, binding);

		CornerstoneTest.AreEqual("foofallback", target.Text);
	}

	[PresentationTestMethod]
	public void ShouldReturnTargetNullValueWhenValueIsNull()
	{
		var target = new TextBlock();
		var source = new Source { Foo = null };

		var binding = new Binding
		{
			Source = source,
			Path = "Foo",
			TargetNullValue = "(null)"
		};

		target.Bind(TextBlock.TextProperty, binding);

		CornerstoneTest.AreEqual("(null)", target.Text);
	}

	[PresentationTestMethod]
	public void StyledPropertySetValueShouldNotCauseStackOverflowAndHaveCorrectValues()
	{
		var viewModel = new TestStackOverflowViewModel
		{
			Value = 50
		};

		var target = new StyledPropertyClass();

		target.Bind(StyledPropertyClass.DoubleValueProperty,
			new Binding("Value") { Mode = BindingMode.TwoWay, Source = viewModel });

		var child = new StyledPropertyClass();

		child.Bind(StyledPropertyClass.DoubleValueProperty,
			new Binding("DoubleValue")
			{
				Mode = BindingMode.TwoWay,
				Source = target
			});

		CornerstoneTest.AreEqual(1, viewModel.SetterInvokedCount);

		//here in real life stack overflow exception is thrown issue #855 and #824
		target.DoubleValue = 51.001;

		CornerstoneTest.AreEqual(2, viewModel.SetterInvokedCount);

		double expected = 51;

		CornerstoneTest.AreEqual(expected, viewModel.Value);
		CornerstoneTest.AreEqual(expected, target.DoubleValue);
		CornerstoneTest.AreEqual(expected, child.DoubleValue);
	}

	[PresentationTestMethod]
	public void TargetUndoingPropertyChangeDuringTwoWayBindingDoesNotCauseStackOverflow()
	{
		var source = new TestStackOverflowViewModel { BoolValue = true };
		var target = new TwoWayBindingTest();

		source.ResetSetterInvokedCount();

		// The AlwaysFalse property is set to false in the PropertyChanged callback. Ensure
		// that binding it to an initial `true` value with a two-way binding does not cause a
		// stack overflow.
		target.Bind(
			TwoWayBindingTest.AlwaysFalseProperty,
			new Binding(nameof(TestStackOverflowViewModel.BoolValue))
			{
				Mode = BindingMode.TwoWay
			});

		target.DataContext = source;

		CornerstoneTest.AreEqual(1, source.SetterInvokedCount);
		CornerstoneTest.IsFalse(source.BoolValue);
		CornerstoneTest.IsFalse(target.AlwaysFalse);
	}

	[PresentationTestMethod]
	public void TwoWayBindingShouldBeSetUp()
	{
		var source = new Source { Foo = "foo" };
		var target = new TextBlock { DataContext = source };
		var binding = new Binding
		{
			Path = "Foo",
			Mode = BindingMode.TwoWay
		};

		target.Bind(TextBox.TextProperty, binding);

		CornerstoneTest.AreEqual("foo", target.Text);
		source.Foo = "bar";
		CornerstoneTest.AreEqual("bar", target.Text);
		target.Text = "baz";
		CornerstoneTest.AreEqual("baz", source.Foo);
	}

	[PresentationTestMethod]
	public void TwoWayBindingShouldBeSetUpGCCollect()
	{
		var source = new WeakRefSource { Foo = null };
		var target = new TestControl { DataContext = source };

		var binding = new Binding
		{
			Path = "Foo",
			Mode = BindingMode.TwoWay
		};

		target.Bind(TestControl.ValueProperty, binding);

		var ref1 = AssignValue(target, "ref1");

		CornerstoneTest.AreEqual(ref1.Target, source.Foo);

		GC.Collect();
		GC.WaitForPendingFinalizers();

		var ref2 = AssignValue(target, "ref2");

		GC.Collect();
		GC.WaitForPendingFinalizers();

		target.Value = null;

		CornerstoneTest.IsNull(source.Foo);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference AssignValue(TestControl source, string val)
	{
		var obj = new DummyObject(val);

		source.Value = obj;

		return new WeakReference(obj);
	}

	#endregion

	#region Classes

	public class Source : INotifyPropertyChanged
	{
		#region Fields

		private string _foo;
		private PropertyChangedEventHandler _propertyChanged;

		#endregion

		#region Properties

		public string Foo
		{
			get => _foo;
			set
			{
				_foo = value;
				++FooSetCount;
				RaisePropertyChanged();
			}
		}

		public int FooSetCount { get; private set; }

		public int SubscriberCount { get; private set; }

		#endregion

		#region Methods

		private void RaisePropertyChanged([CallerMemberName] string prop = "")
		{
			_propertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
		}

		#endregion

		#region Events

		public event PropertyChangedEventHandler PropertyChanged
		{
			add
			{
				_propertyChanged += value;
				++SubscriberCount;
			}
			remove
			{
				_propertyChanged += value;
				--SubscriberCount;
			}
		}

		#endregion
	}

	public class WeakRefSource : INotifyPropertyChanged
	{
		#region Fields

		private WeakReference<object> _foo;

		#endregion

		#region Properties

		public object Foo
		{
			get
			{
				if (_foo == null)
				{
					return null;
				}

				if (_foo.TryGetTarget(out var target))
				{
					if (target is ICloneable cloneable)
					{
						return cloneable.Clone();
					}

					return target;
				}

				return null;
			}
			set
			{
				_foo = new WeakReference<object>(value);

				RaisePropertyChanged();
			}
		}

		#endregion

		#region Methods

		private void RaisePropertyChanged([CallerMemberName] string prop = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
		}

		#endregion

		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}

	private class DirectPropertyClass : PresentationObject
	{
		#region Fields

		public static readonly DirectProperty<DirectPropertyClass, double> DoubleValueProperty =
			PresentationProperty.RegisterDirect<DirectPropertyClass, double>(
				nameof(DoubleValue),
				o => o.DoubleValue,
				(o, v) => o.DoubleValue = v);

		private double _doubleValue;

		#endregion

		#region Properties

		public double DoubleValue
		{
			get => _doubleValue;
			set => SetAndRaise(DoubleValueProperty, ref _doubleValue, value);
		}

		#endregion
	}

	private class DummyObject : ICloneable
	{
		#region Fields

		private readonly string _val;

		#endregion

		#region Constructors

		public DummyObject(string val)
		{
			_val = val;
		}

		#endregion

		#region Methods

		public object Clone()
		{
			return new DummyObject(_val);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}
			if (ReferenceEquals(this, obj))
			{
				return true;
			}
			if (obj.GetType() != GetType())
			{
				return false;
			}
			return Equals((DummyObject) obj);
		}

		public override int GetHashCode()
		{
			return _val != null ? _val.GetHashCode() : 0;
		}

		protected bool Equals(DummyObject other)
		{
			return string.Equals(_val, other._val);
		}

		#endregion
	}

	private class InheritanceTest : Decorator
	{
		#region Fields

		public static readonly StyledProperty<int> BazProperty =
			PresentationProperty.Register<InheritanceTest, int>(nameof(Baz), 6, true);

		#endregion

		#region Properties

		public int Baz
		{
			get => GetValue(BazProperty);
			set => SetValue(BazProperty, value);
		}

		#endregion
	}

	private class NullableValuesViewModel : INotifyPropertyChanged
	{
		#region Fields

		private double? _nullableDouble;

		#endregion

		#region Properties

		public double? NullableDouble
		{
			get => _nullableDouble;
			set
			{
				_nullableDouble = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NullableDouble)));
			}
		}

		#endregion

		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}

	private class OldDataContextTest : Control
	{
		#region Fields

		public static readonly StyledProperty<int> BarProperty =
			PresentationProperty.Register<OldDataContextTest, int>("Bar");

		public static readonly StyledProperty<int> FooProperty =
			PresentationProperty.Register<OldDataContextTest, int>("Foo");

		#endregion

		#region Constructors

		public OldDataContextTest()
		{
			Bind(BarProperty, this.GetObservable(FooProperty));
		}

		#endregion
	}

	private class OldDataContextViewModel
	{
		#region Properties

		public int Bar { get; } = 2;
		public int Foo { get; } = 1;

		#endregion
	}

	private class StyledPropertyClass : PresentationObject
	{
		#region Fields

		public static readonly StyledProperty<double> DoubleValueProperty =
			PresentationProperty.Register<StyledPropertyClass, double>(nameof(DoubleValue), 12.3);

		public static readonly StyledProperty<double?> NullableDoubleProperty =
			PresentationProperty.Register<StyledPropertyClass, double?>(nameof(NullableDoubleProperty), -1);

		#endregion

		#region Properties

		public double DoubleValue
		{
			get => GetValue(DoubleValueProperty);
			set => SetValue(DoubleValueProperty, value);
		}

		public double? NullableDouble
		{
			get => GetValue(NullableDoubleProperty);
			set => SetValue(NullableDoubleProperty, value);
		}

		#endregion
	}

	private class TestControl : Control
	{
		#region Fields

		public static readonly DirectProperty<TestControl, object> ValueProperty =
			PresentationProperty.RegisterDirect<TestControl, object>(
				nameof(Value),
				o => o.Value,
				(o, v) => o.Value = v);

		private object _value;

		#endregion

		#region Properties

		public object Value
		{
			get => _value;
			set => SetAndRaise(ValueProperty, ref _value, value);
		}

		#endregion
	}

	private class TestStackOverflowViewModel : INotifyPropertyChanged
	{
		#region Constants

		public const int MaxInvokedCount = 1000;

		#endregion

		#region Fields

		private bool _boolValue;
		private double _value;

		#endregion

		#region Properties

		public bool BoolValue
		{
			get => _boolValue;
			set
			{
				if (_boolValue != value)
				{
					_boolValue = value;
					SetterInvokedCount++;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BoolValue)));
				}
			}
		}

		public int SetterInvokedCount { get; private set; }

		public double Value
		{
			get => _value;
			set
			{
				if (_value != value)
				{
					SetterInvokedCount++;
					if (SetterInvokedCount < MaxInvokedCount)
					{
						_value = (int) value;
						if (_value > 75)
						{
							_value = 75;
						}
						if (_value < 25)
						{
							_value = 25;
						}
					}
					else
					{
						_value = value;
					}

					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
				}
			}
		}

		#endregion

		#region Methods

		public void ResetSetterInvokedCount()
		{
			SetterInvokedCount = 0;
		}

		#endregion

		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}

	private class TwoWayBindingTest : Control
	{
		#region Fields

		public static readonly StyledProperty<bool> AlwaysFalseProperty =
			PresentationProperty.Register<StyledPropertyClass, bool>(nameof(AlwaysFalse));

		public static readonly StyledProperty<string> TwoWayProperty =
			PresentationProperty.Register<TwoWayBindingTest, string>(
				"TwoWay",
				defaultBindingMode: BindingMode.TwoWay);

		#endregion

		#region Properties

		public bool AlwaysFalse
		{
			get => GetValue(AlwaysFalseProperty);
			set => SetValue(AlwaysFalseProperty, value);
		}

		public string TwoWay
		{
			get => GetValue(TwoWayProperty);
			set => SetValue(TwoWayProperty, value);
		}

		#endregion

		#region Methods

		protected override void OnPropertyChanged(PresentationPropertyChangedEventArgs change)
		{
			base.OnPropertyChanged(change);

			if (change.Property == AlwaysFalseProperty)
			{
				SetCurrentValue(AlwaysFalseProperty, false);
			}
		}

		#endregion
	}

	#endregion
}