#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Data;

[TestClass]
public class MultiBindingTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ConverterCanReturnBindingNotification()
	{
		var source = new { A = 1, B = 2, C = 3 };
		var target = new TextBlock { DataContext = source };

		var binding = new MultiBinding
		{
			Converter = new BindingNotificationConverter(),
			Bindings = new[]
			{
				new Binding { Path = "A" },
				new Binding { Path = "B" },
				new Binding { Path = "C" }
			}
		};

		target.Bind(TextBlock.TextProperty, binding);

		CornerstoneTest.AreEqual("1,2,3-BindingNotification", target.Text);
	}

	[PresentationTestMethod]
	public void ConverterShouldBeCalledOnPropertyChangedEvenIfPropertyNotChanged()
	{
		// Issue #16084
		var data = new TestModel();
		var target = new TextBlock { DataContext = data };

		var binding = new MultiBinding
		{
			Converter = new TestModelMemberConverter(),
			Bindings =
			{
				new Binding(),
				new Binding(nameof(data.NotifyingValue))
			}
		};

		target.Bind(TextBlock.TextProperty, binding);
		CornerstoneTest.AreEqual("0", target.Text);

		data.NonNotifyingValue = 1;
		CornerstoneTest.AreEqual("0", target.Text);

		data.NotifyingValue = new object();
		CornerstoneTest.AreEqual("1", target.Text);
	}

	[PresentationTestMethod]
	public void MultiBindingWithoutStringFormatAndConverter()
	{
		var source = new { A = 1, B = 2, C = 3 };
		var target = new ItemsControl();

		var binding = new MultiBinding
		{
			Bindings = new[]
			{
				new Binding { Path = "A", Source = source },
				new Binding { Path = "B", Source = source },
				new Binding { Path = "C", Source = source }
			}
		};

		target.Bind(ItemsControl.ItemsSourceProperty, binding);
		CornerstoneTest.AreEqual(target.ItemCount, 3);
		CornerstoneTest.AreEqual(target.ItemsView[0], source.A);
		CornerstoneTest.AreEqual(target.ItemsView[1], source.B);
		CornerstoneTest.AreEqual(target.ItemsView[2], source.C);
	}

	[PresentationTestMethod]
	public void NestedMultiBindingShouldBeSetUp()
	{
		var source = new { A = 1, B = 2, C = 3 };
		var binding = new MultiBinding
		{
			Converter = new ConcatConverter(),
			Bindings =
			{
				new Binding { Path = "A" },
				new MultiBinding
				{
					Converter = new ConcatConverter(),
					Bindings =
					{
						new Binding { Path = "B" },
						new Binding { Path = "C" }
					}
				}
			}
		};

		var target = new Control { DataContext = source };
		target.Bind(Control.TagProperty, binding);

		CornerstoneTest.AreEqual("1,2,3", target.Tag);
	}

	[PresentationTestMethod]
	public void OneWayBindingShouldBeSetUp()
	{
		var source = new { A = 1, B = 2, C = 3 };
		var binding = new MultiBinding
		{
			Converter = new ConcatConverter(),
			Bindings = new[]
			{
				new Binding { Path = "A" },
				new Binding { Path = "B" },
				new Binding { Path = "C" }
			}
		};

		var target = new Control { DataContext = source };
		target.Bind(Control.TagProperty, binding);

		CornerstoneTest.AreEqual("1,2,3", target.Tag);
	}

	[PresentationTestMethod]
	public void ShouldPassFallbackValueToConverterForBrokenBinding()
	{
		var source = new { A = 1, B = 2, C = 3 };
		var target = new TextBlock { DataContext = source };

		var binding = new MultiBinding
		{
			Converter = new ConcatConverter(),
			Bindings = new[]
			{
				new Binding { Path = "A" },
				new Binding { Path = "B" },
				new Binding { Path = "Missing", FallbackValue = "Fallback" }
			}
		};

		target.Bind(TextBlock.TextProperty, binding);

		CornerstoneTest.IsNotNull(target.Text);
		CornerstoneTest.AreEqual("1,2,Fallback", target.Text);
	}

	[PresentationTestMethod]
	public void ShouldPassUnsetValueToConverterForBrokenBinding()
	{
		var source = new { A = 1, B = 2, C = 3 };
		var target = new TextBlock { DataContext = source };

		var binding = new MultiBinding
		{
			Converter = new ConcatConverter(),
			Bindings = new[]
			{
				new Binding { Path = "A" },
				new Binding { Path = "B" },
				new Binding { Path = "Missing" }
			}
		};

		target.Bind(TextBlock.TextProperty, binding);

		CornerstoneTest.IsNotNull(target.Text);
		CornerstoneTest.AreEqual("1,2,(unset)", target.Text);
	}

	[PresentationTestMethod]
	public void ShouldReturnFallbackValueWhenConverterReturnsUnsetValue()
	{
		var target = new TextBlock();
		var source = new { A = 1, B = 2, C = 3 };
		var binding = new MultiBinding
		{
			Converter = new UnsetValueConverter(),
			Bindings = new[]
			{
				new Binding { Path = "A" },
				new Binding { Path = "B" },
				new Binding { Path = "C" }
			},
			FallbackValue = "fallback"
		};

		target.Bind(TextBlock.TextProperty, binding);

		CornerstoneTest.IsNotNull(target.Text);
		CornerstoneTest.AreEqual("fallback", target.Text);
	}

	[PresentationTestMethod]
	public void ShouldReturnTargetNullValueWhenValueIsNull()
	{
		var target = new TextBlock();

		var binding = new MultiBinding
		{
			Converter = new NullValueConverter(),
			Bindings = new[]
			{
				new Binding { Path = "A" },
				new Binding { Path = "B" },
				new Binding { Path = "C" }
			},
			TargetNullValue = "(null)"
		};

		target.Bind(TextBlock.TextProperty, binding);

		CornerstoneTest.IsNotNull(target.Text);
		CornerstoneTest.AreEqual("(null)", target.Text);
	}

	#endregion

	#region Classes

	private class BindingNotificationConverter : IMultiValueConverter
	{
		#region Methods

		public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
		{
			return new BindingNotification(
				new ArgumentException(),
				BindingErrorType.Error,
				string.Join(",", values) + "-BindingNotification");
		}

		#endregion
	}

	private class ConcatConverter : IMultiValueConverter
	{
		#region Methods

		public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
		{
			return string.Join(",", values);
		}

		#endregion
	}

	private class NullValueConverter : IMultiValueConverter
	{
		#region Methods

		public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}

		#endregion
	}

	private partial class TestModel : NotifyingBase
	{
		#region Fields

		private object _notifyingValue;

		#endregion

		#region Properties

		public int? NonNotifyingValue { get; set; } = 0;

		public object NotifyingValue
		{
			get => _notifyingValue;
			set => SetField(ref _notifyingValue, value);
		}

		#endregion
	}

	private class TestModelMemberConverter : IMultiValueConverter
	{
		#region Methods

		public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
		{
			if (values[0] is not TestModel model)
			{
				return string.Empty;
			}

			return model.NonNotifyingValue.ToString();
		}

		#endregion
	}

	private class UnsetValueConverter : IMultiValueConverter
	{
		#region Methods

		public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
		{
			return PresentationProperty.UnsetValue;
		}

		#endregion
	}

	#endregion
}