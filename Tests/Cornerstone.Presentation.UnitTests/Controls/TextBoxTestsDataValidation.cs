#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Templates;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Harfbuzz;
using Cornerstone.Presentation.Markup.Xaml.MarkupExtensions;
using Cornerstone.Presentation.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class TextBoxTestsDataValidation : ScopedTestBase
{
	#region Properties

	private static TestServices Services =>
		TestServices.MockThreadingInterface.With(
			standardCursorFactory: new StubCursorFactory(),
			textShaperImpl: new HarfBuzzTextShaper(),
			fontManagerImpl: new HeadlessFontManagerStub());

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void CompiledBindingToDataValidationPropertyReportsDataValidationErrors()
	{
		// This binding is shape-eligible for the typed binding expression (a directly
		// assignable single-property DataContext binding), which does not support data
		// validation. Because TextBox.Text enables data validation it must fall back to the
		// untyped BindingExpression and still surface validation errors.
		var path = new CompiledBindingPathBuilder()
			.Property(
				new ClrPropertyInfo<IndeiStringTest, string>(
					nameof(IndeiStringTest.Value),
					o => o.Value,
					(o, v) => o.Value = v),
				PropertyInfoAccessorFactory.CreateInpcPropertyAccessor,
				true)
			.Build();

		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				DataContext = new IndeiStringTest(),
				[!TextBox.TextProperty] = new CompiledBindingExtension
				{
					Path = path,
					Mode = BindingMode.TwoWay
				},
				Template = CreateTemplate()
			};

			target.ApplyTemplate();

			CornerstoneTest.IsFalse(DataValidationErrors.GetHasErrors(target));
			target.Text = "bad";
			CornerstoneTest.IsTrue(DataValidationErrors.GetHasErrors(target));
			target.Text = "good";
			CornerstoneTest.IsFalse(DataValidationErrors.GetHasErrors(target));
		}
	}

	[PresentationTestMethod]
	public void CompiledBindingsTypeConverterExceptionsShouldSetDataValidationErrorsHasErrors()
	{
		var path = new CompiledBindingPathBuilder()
			.Property(
				new ClrPropertyInfo(
					nameof(ExceptionTest.LessThan10),
					target => ((ExceptionTest) target).LessThan10,
					(target, value) => ((ExceptionTest) target).LessThan10 = (int) value!,
					typeof(int)),
				PropertyInfoAccessorFactory.CreateInpcPropertyAccessor)
			.Build();

		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				DataContext = new ExceptionTest(),
				[!TextBox.TextProperty] = new CompiledBindingExtension
				{
					Source = new ExceptionTest(),
					Path = path,
					Mode = BindingMode.TwoWay
				},
				Template = CreateTemplate()
			};

			target.ApplyTemplate();

			target.Text = "a";
			CornerstoneTest.IsTrue(DataValidationErrors.GetHasErrors(target));
		}
	}

	[PresentationTestMethod]
	public void SetterExceptionsShouldBeConverterIfErrorConverterSet()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				DataContext = new ExceptionTest(),
				[!TextBox.TextProperty] = new Binding(nameof(ExceptionTest.LessThan10)) { Mode = BindingMode.TwoWay },
				Template = CreateTemplate()
			};
			DataValidationErrors.SetErrorConverter(target, err => "Error: " + err);

			target.ApplyTemplate();

			target.Text = "20";

			var errors = DataValidationErrors.GetErrors(target);
			CornerstoneTest.IsNotNull(errors);
			var error = CornerstoneTest.IsType<string>(CornerstoneTest.Single(errors));
			CornerstoneTest.StartsWith(error, "Error: ");
		}
	}

	[PresentationTestMethod]
	public void SetterExceptionsShouldSetDataValidationErrorsErrors()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				DataContext = new ExceptionTest(),
				[!TextBox.TextProperty] = new Binding(nameof(ExceptionTest.LessThan10)) { Mode = BindingMode.TwoWay },
				Template = CreateTemplate()
			};

			target.ApplyTemplate();

			CornerstoneTest.IsNull(DataValidationErrors.GetErrors(target));
			target.Text = "20";

			var errors = DataValidationErrors.GetErrors(target);
			CornerstoneTest.IsNotNull(errors);
			var error = CornerstoneTest.Single(errors);
			CornerstoneTest.IsType<InvalidOperationException>(error);
			target.Text = "1";
			CornerstoneTest.IsNull(DataValidationErrors.GetErrors(target));
		}
	}

	[PresentationTestMethod]
	public void SetterExceptionsShouldSetDataValidationErrorsHasErrors()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				DataContext = new ExceptionTest(),
				[!TextBox.TextProperty] = new Binding(nameof(ExceptionTest.LessThan10)) { Mode = BindingMode.TwoWay },
				Template = CreateTemplate()
			};

			target.ApplyTemplate();

			CornerstoneTest.IsFalse(DataValidationErrors.GetHasErrors(target));
			target.Text = "20";
			CornerstoneTest.IsTrue(DataValidationErrors.GetHasErrors(target));
			target.Text = "1";
			CornerstoneTest.IsFalse(DataValidationErrors.GetHasErrors(target));
		}
	}

	[PresentationTestMethod]
	public void SetterExceptionsShouldSetErrorPseudoclass()
	{
		using (UnitTestApplication.Start(Services))
		{
			var target = new TextBox
			{
				DataContext = new ExceptionTest(),
				[!TextBox.TextProperty] = new Binding(nameof(ExceptionTest.LessThan10)) { Mode = BindingMode.TwoWay },
				Template = CreateTemplate()
			};

			target.ApplyTemplate();

			CornerstoneTest.DoesNotContain(target.Classes, ":error");
			target.Text = "20";
			CornerstoneTest.Contains(target.Classes, ":error");
			target.Text = "1";
			CornerstoneTest.DoesNotContain(target.Classes, ":error");
		}
	}

	private static IControlTemplate CreateTemplate()
	{
		return new FuncControlTemplate<TextBox>((control, scope) =>
			new TextPresenter
			{
				Name = "PART_TextPresenter",
				[!!TextPresenter.TextProperty] = new Binding
				{
					Path = "Text",
					Mode = BindingMode.TwoWay,
					Priority = BindingPriority.Template,
					RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
				}
			}.RegisterInNameScope(scope));
	}

	#endregion

	#region Classes

	private class ExceptionTest
	{
		#region Fields

		private int _lessThan10;

		#endregion

		#region Properties

		public int LessThan10
		{
			get => _lessThan10;
			set
			{
				if (value < 10)
				{
					_lessThan10 = value;
				}
				else
				{
					throw new InvalidOperationException("More than 10.");
				}
			}
		}

		#endregion
	}

	private class IndeiStringTest : INotifyDataErrorInfo
	{
		#region Fields

		private readonly Dictionary<string, IList<string>> _errors = new();
		private string _value;

		#endregion

		#region Properties

		public bool HasErrors => _errors.Count > 0;

		public string Value
		{
			get => _value;
			set
			{
				_value = value;
				if (value == "bad")
				{
					_errors[nameof(Value)] = new[] { "Invalid" };
				}
				else
				{
					_errors.Remove(nameof(Value));
				}
				ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Value)));
			}
		}

		#endregion

		#region Methods

		public IEnumerable GetErrors(string propertyName)
		{
			if (propertyName is not null && _errors.TryGetValue(propertyName, out var result))
			{
				return result;
			}
			return Array.Empty<string>();
		}

		#endregion

		#region Events

		public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

		#endregion
	}

	private class IndeiTest : INotifyDataErrorInfo
	{
		#region Fields

		private readonly Dictionary<string, IList<string>> _errors = new();
		private int _lessThan10;

		#endregion

		#region Properties

		public bool HasErrors => _lessThan10 >= 10;

		public int LessThan10
		{
			get => _lessThan10;
			set
			{
				if (value < 10)
				{
					_lessThan10 = value;
					_errors.Remove(nameof(LessThan10));
					ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(LessThan10)));
				}
				else
				{
					_errors[nameof(LessThan10)] = new[] { "More than 10" };
					ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(LessThan10)));
				}
			}
		}

		#endregion

		#region Methods

		public IEnumerable GetErrors(string propertyName)
		{
			if (propertyName is not null && _errors.TryGetValue(propertyName, out var result))
			{
				return result;
			}
			return Array.Empty<string>();
		}

		#endregion

		#region Events

		public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

		#endregion
	}

	#endregion
}