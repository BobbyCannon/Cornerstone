#region References

using System;
using System.Collections;
using System.ComponentModel;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Styling;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Data;

[TestClass]
public class BindingTestsDataValidation
{
	#region Classes

	[TestClass]
	public class DirectPropertyTests : TestBase<DirectPropertyBase<int>>
	{
		#region Methods

		private protected override (DataValidationTestControl, DirectPropertyBase<int>) CreateTarget()
		{
			return (new ValidatedDirectPropertyClass(), ValidatedDirectPropertyClass.ValueProperty);
		}

		#endregion
	}

	[TestClass]
	public class StyledPropertyTests : TestBase<StyledProperty<int>>
	{
		#region Methods

		[PresentationTestMethod]
		public void DataValidationCanSwitchBetweenStyleAndLocalValueBinding()
		{
			var (target, property) = CreateTarget();
			var model1 = new IndeiValidatingModel { Value = 200 };
			var model2 = new IndeiValidatingModel { Value = 300 };
			var binding1 = new Binding(nameof(IndeiValidatingModel.Value));
			var binding2 = new Binding(nameof(IndeiValidatingModel.Value)) { Source = model2 };

			var root = new TestRoot
			{
				DataContext = model1,
				Styles =
				{
					new Style(x => x.Is<DataValidationTestControl>())
					{
						Setters =
						{
							new Setter(property, binding1)
						}
					}
				},
				Child = target
			};

			root.LayoutManager.ExecuteInitialLayoutPass();

			CornerstoneTest.AreEqual(200, target.GetValue(property));
			CornerstoneTest.IsType<DataValidationException>(target.DataValidationError);
			CornerstoneTest.AreEqual("Invalid value: 200.", target.DataValidationError?.Message);

			var sub = target.Bind(property, binding2);
			CornerstoneTest.AreEqual(300, target.GetValue(property));
			CornerstoneTest.AreEqual("Invalid value: 300.", target.DataValidationError?.Message);

			sub.Dispose();
			CornerstoneTest.AreEqual(200, target.GetValue(property));
			CornerstoneTest.IsType<DataValidationException>(target.DataValidationError);
			CornerstoneTest.AreEqual("Invalid value: 200.", target.DataValidationError?.Message);
		}

		[PresentationTestMethod]
		public void DataValidationCanSwitchBetweenStyleAndStyleTriggerBinding()
		{
			var (target, property) = CreateTarget();
			var model1 = new IndeiValidatingModel { Value = 200 };
			var model2 = new IndeiValidatingModel { Value = 300 };
			var binding1 = new Binding(nameof(IndeiValidatingModel.Value));
			var binding2 = new Binding(nameof(IndeiValidatingModel.Value)) { Source = model2 };

			var root = new TestRoot
			{
				DataContext = model1,
				Styles =
				{
					new Style(x => x.Is<DataValidationTestControl>())
					{
						Setters =
						{
							new Setter(property, binding1)
						}
					},
					new Style(x => x.Is<DataValidationTestControl>().Class("foo"))
					{
						Setters =
						{
							new Setter(property, binding2)
						}
					}
				},
				Child = target
			};

			root.LayoutManager.ExecuteInitialLayoutPass();

			CornerstoneTest.AreEqual(200, target.GetValue(property));
			CornerstoneTest.IsType<DataValidationException>(target.DataValidationError);
			CornerstoneTest.AreEqual("Invalid value: 200.", target.DataValidationError?.Message);

			target.Classes.Add("foo");
			CornerstoneTest.AreEqual(300, target.GetValue(property));
			CornerstoneTest.AreEqual("Invalid value: 300.", target.DataValidationError?.Message);

			target.Classes.Remove("foo");
			CornerstoneTest.AreEqual(200, target.GetValue(property));
			CornerstoneTest.IsType<DataValidationException>(target.DataValidationError);
			CornerstoneTest.AreEqual("Invalid value: 200.", target.DataValidationError?.Message);
		}

		[PresentationTestMethod]
		public void StyleBindingSupportsDataValidation()
		{
			var (target, property) = CreateTarget();
			var binding = new Binding(nameof(IndeiValidatingModel.Value))
			{
				Mode = BindingMode.TwoWay
			};

			var model = new IndeiValidatingModel();
			var root = new TestRoot
			{
				DataContext = model,
				Styles =
				{
					new Style(x => x.Is<DataValidationTestControl>())
					{
						Setters =
						{
							new Setter(property, binding)
						}
					}
				},
				Child = target
			};

			root.LayoutManager.ExecuteInitialLayoutPass();

			CornerstoneTest.AreEqual(20, target.GetValue(property));

			model.Value = 200;

			CornerstoneTest.AreEqual(200, target.GetValue(property));
			CornerstoneTest.IsType<DataValidationException>(target.DataValidationError);
			CornerstoneTest.AreEqual("Invalid value: 200.", target.DataValidationError?.Message);

			model.Value = 10;

			CornerstoneTest.AreEqual(10, target.GetValue(property));
			CornerstoneTest.IsNull(target.DataValidationError);
		}

		[PresentationTestMethod]
		public void StyleWithActivatorBindingSupportsDataValidation()
		{
			var (target, property) = CreateTarget();
			var binding = new Binding(nameof(IndeiValidatingModel.Value))
			{
				Mode = BindingMode.TwoWay
			};

			var model = new IndeiValidatingModel
			{
				Value = 200
			};

			var root = new TestRoot
			{
				DataContext = model,
				Styles =
				{
					new Style(x => x.Is<DataValidationTestControl>().Class("foo"))
					{
						Setters =
						{
							new Setter(property, binding)
						}
					}
				},
				Child = target
			};

			root.LayoutManager.ExecuteInitialLayoutPass();
			target.Classes.Add("foo");

			CornerstoneTest.AreEqual(200, target.GetValue(property));
			CornerstoneTest.IsType<DataValidationException>(target.DataValidationError);
			CornerstoneTest.AreEqual("Invalid value: 200.", target.DataValidationError?.Message);

			target.Classes.Remove("foo");
			CornerstoneTest.AreEqual(0, target.GetValue(property));
			CornerstoneTest.IsNull(target.DataValidationError);

			target.Classes.Add("foo");
			CornerstoneTest.IsType<DataValidationException>(target.DataValidationError);
			CornerstoneTest.AreEqual("Invalid value: 200.", target.DataValidationError?.Message);

			model.Value = 10;

			CornerstoneTest.AreEqual(10, target.GetValue(property));
			CornerstoneTest.IsNull(target.DataValidationError);
		}

		private protected override (DataValidationTestControl, StyledProperty<int>) CreateTarget()
		{
			return (new ValidatedStyledPropertyClass(), ValidatedStyledPropertyClass.ValueProperty);
		}

		#endregion
	}

	public abstract class TestBase<T> : ScopedTestBase
		where T : PresentationProperty<int>
	{
		#region Methods

		[PresentationTestMethod]
		public void DisposingBindingSubscriptionClearsDataValidation()
		{
			var (target, property) = CreateTarget();
			var binding = new Binding(nameof(ExceptionValidatingModel.Value))
			{
				Mode = BindingMode.TwoWay
			};

			target.DataContext = new IndeiValidatingModel
			{
				Value = 200
			};

			var sub = target.Bind(property, binding);

			CornerstoneTest.AreEqual(200, target.GetValue(property));
			CornerstoneTest.IsType<DataValidationException>(target.DataValidationError);

			sub.Dispose();

			CornerstoneTest.IsNull(target.DataValidationError);
		}

		[PresentationTestMethod]
		public void IndeiErrorCausesDataValidationError()
		{
			var (target, property) = CreateTarget();
			var binding = new Binding(nameof(IndeiValidatingModel.Value))
			{
				Mode = BindingMode.TwoWay
			};

			target.DataContext = new IndeiValidatingModel();
			target.Bind(property, binding);

			CornerstoneTest.AreEqual(20, target.GetValue(property));

			target.SetValue(property, 200);

			CornerstoneTest.AreEqual(200, target.GetValue(property));
			CornerstoneTest.IsType<DataValidationException>(target.DataValidationError);
			CornerstoneTest.AreEqual("Invalid value: 200.", target.DataValidationError?.Message);

			target.SetValue(property, 10);

			CornerstoneTest.AreEqual(10, target.GetValue(property));
			CornerstoneTest.IsNull(target.DataValidationError);
		}

		[PresentationTestMethod]
		public void SetterExceptionCausesDataValidationError()
		{
			var (target, property) = CreateTarget();
			var binding = new Binding(nameof(ExceptionValidatingModel.Value))
			{
				Mode = BindingMode.TwoWay
			};

			target.DataContext = new ExceptionValidatingModel();
			target.Bind(property, binding);

			CornerstoneTest.AreEqual(20, target.GetValue(property));

			target.SetValue(property, 200);

			CornerstoneTest.AreEqual(200, target.GetValue(property));
			CornerstoneTest.IsType<ArgumentOutOfRangeException>(target.DataValidationError);

			target.SetValue(property, 10);

			CornerstoneTest.AreEqual(10, target.GetValue(property));
			CornerstoneTest.IsNull(target.DataValidationError);
		}

		private protected abstract (DataValidationTestControl, T) CreateTarget();

		#endregion
	}

	internal class DataValidationTestControl : Control
	{
		#region Properties

		public Exception DataValidationError { get; protected set; }

		#endregion
	}

	private class ExceptionValidatingModel
	{
		#region Constants

		public const int MaxValue = 100;

		#endregion

		#region Fields

		private int _value = 20;

		#endregion

		#region Properties

		public int Value
		{
			get => _value;
			set
			{
				if (value > MaxValue)
				{
					throw new ArgumentOutOfRangeException(nameof(value));
				}
				_value = value;
			}
		}

		#endregion
	}

	private class IndeiValidatingModel : INotifyDataErrorInfo
	{
		#region Constants

		public const int MaxValue = 100;

		#endregion

		#region Fields

		private bool _hasErrors;
		private int _value = 20;

		#endregion

		#region Properties

		public bool HasErrors
		{
			get => _hasErrors;
			private set
			{
				if (_hasErrors != value)
				{
					_hasErrors = value;
					ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Value)));
				}
			}
		}

		public int Value
		{
			get => _value;
			set
			{
				_value = value;
				HasErrors = value > MaxValue;
			}
		}

		#endregion

		#region Methods

		public IEnumerable GetErrors(string propertyName)
		{
			if ((propertyName == nameof(Value)) && (_value > MaxValue))
			{
				yield return $"Invalid value: {_value}.";
			}
		}

		#endregion

		#region Events

		public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

		#endregion
	}

	private class ValidatedDirectPropertyClass : DataValidationTestControl
	{
		#region Fields

		public static readonly DirectProperty<ValidatedDirectPropertyClass, int> ValueProperty =
			PresentationProperty.RegisterDirect<ValidatedDirectPropertyClass, int>(
				"Value",
				o => o.Value,
				(o, v) => o.Value = v,
				enableDataValidation: true);

		private int _value;

		#endregion

		#region Properties

		public int Value
		{
			get => _value;
			set => SetAndRaise(ValueProperty, ref _value, value);
		}

		#endregion

		#region Methods

		protected override void UpdateDataValidation(PresentationProperty property, BindingValueType state, Exception error)
		{
			if (property == ValueProperty)
			{
				DataValidationError = state.HasAnyFlag(BindingValueType.DataValidationError) ? error : null;
			}
		}

		#endregion
	}

	private class ValidatedStyledPropertyClass : DataValidationTestControl
	{
		#region Fields

		public static readonly StyledProperty<int> ValueProperty =
			PresentationProperty.Register<ValidatedStyledPropertyClass, int>(
				"Value",
				enableDataValidation: true);

		#endregion

		#region Properties

		public int Value
		{
			get => GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		#endregion

		#region Methods

		protected override void UpdateDataValidation(PresentationProperty property, BindingValueType state, Exception error)
		{
			if (property == ValueProperty)
			{
				DataValidationError = state.HasAnyFlag(BindingValueType.DataValidationError) ? error : null;
			}
		}

		#endregion
	}

	#endregion
}