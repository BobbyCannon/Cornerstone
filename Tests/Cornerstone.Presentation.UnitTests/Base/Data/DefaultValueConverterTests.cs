#region References

using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Layout;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data;

[TestClass]
public class DefaultValueConverterTests
{
	#region Methods

	[PresentationTestMethod]
	public void CanConvertCustomTypeToInt()
	{
		var result = DefaultValueConverter.Instance.Convert(
			new CustomType(123),
			typeof(int),
			null,
			CultureInfo.InvariantCulture);

		CornerstoneTest.AreEqual(123, result);
	}

	[PresentationTestMethod]
	public void CanConvertDecimalToNullableDouble()
	{
		var result = DefaultValueConverter.Instance.Convert(
			5m,
			typeof(double?),
			null,
			CultureInfo.InvariantCulture);

		CornerstoneTest.AreEqual(5.0, result);
	}

	[PresentationTestMethod]
	public void CanConvertDoubleToString()
	{
		var result = DefaultValueConverter.Instance.Convert(
			5.0,
			typeof(string),
			null,
			CultureInfo.InvariantCulture);

		CornerstoneTest.AreEqual("5", result);
	}

	[PresentationTestMethod]
	public void CanConvertEnumToInt()
	{
		var result = DefaultValueConverter.Instance.Convert(
			TestEnum.Bar,
			typeof(int),
			null,
			CultureInfo.InvariantCulture);

		CornerstoneTest.AreEqual(1, result);
	}

	[PresentationTestMethod]
	public void CanConvertEnumToString()
	{
		var result = DefaultValueConverter.Instance.Convert(
			TestEnum.Bar,
			typeof(string),
			null,
			CultureInfo.InvariantCulture);

		CornerstoneTest.AreEqual("Bar", result);
	}

	[PresentationTestMethod]
	public void CanConvertFromDelegateToCommand()
	{
		var commandResult = 0;

		var result = DefaultValueConverter.Instance.Convert(
			(Action<int>) (i => { commandResult = i; }),
			typeof(ICommand),
			null,
			CultureInfo.InvariantCulture);

		var command = CornerstoneTest.IsAssignableFrom<ICommand>(result);

		command.Execute(5);

		CornerstoneTest.AreEqual(5, commandResult);
	}

	[PresentationTestMethod]
	public void CanConvertFromDelegateToCommandNoParameters()
	{
		var commandResult = 0;

		var result = DefaultValueConverter.Instance.Convert(
			(Action) (() => { commandResult = 1; }),
			typeof(ICommand),
			null,
			CultureInfo.InvariantCulture);

		var command = CornerstoneTest.IsAssignableFrom<ICommand>(result);

		command.Execute(null);

		CornerstoneTest.AreEqual(1, commandResult);
	}

	[PresentationTestMethod]
	public void CanConvertIntToCustomType()
	{
		var result = DefaultValueConverter.Instance.Convert(
			123,
			typeof(CustomType),
			null,
			CultureInfo.InvariantCulture);

		CornerstoneTest.AreEqual(new CustomType(123), result);
	}

	[PresentationTestMethod]
	public void CanConvertIntToEnum()
	{
		var result = DefaultValueConverter.Instance.Convert(
			1,
			typeof(TestEnum),
			null,
			CultureInfo.InvariantCulture);

		CornerstoneTest.AreEqual(TestEnum.Bar, result);
	}

	[PresentationTestMethod]
	public void CanConvertStringToDouble()
	{
		var result = DefaultValueConverter.Instance.Convert(
			"5",
			typeof(double),
			null,
			CultureInfo.InvariantCulture);

		CornerstoneTest.AreEqual(5.0, result);
	}

	[PresentationTestMethod]
	public void CanConvertStringToEnum()
	{
		var result = DefaultValueConverter.Instance.Convert(
			"Bar",
			typeof(TestEnum),
			null,
			CultureInfo.InvariantCulture);

		CornerstoneTest.AreEqual(TestEnum.Bar, result);
	}

	[PresentationTestMethod]
	public void CanConvertStringToInt()
	{
		var result = DefaultValueConverter.Instance.Convert(
			"5",
			typeof(int),
			null,
			CultureInfo.InvariantCulture);

		CornerstoneTest.AreEqual(5, result);
	}

	[PresentationTestMethod]
	public void CanConvertStringToTimeSpan()
	{
		var result = DefaultValueConverter.Instance.Convert(
			"00:00:10",
			typeof(TimeSpan),
			null,
			CultureInfo.InvariantCulture);

		CornerstoneTest.AreEqual(TimeSpan.FromSeconds(10), result);
	}

	[PresentationTestMethod]
	public void CanUseExplicitCast()
	{
		var result = DefaultValueConverter.Instance.Convert(
			new ExplicitDouble(5.0),
			typeof(double),
			null,
			CultureInfo.InvariantCulture);

		CornerstoneTest.AreEqual(5.0, result);
	}

	[PresentationTestMethod]
	public void CannotConvertBetweenDifferentEnumTypes()
	{
		var result = DefaultValueConverter.Instance.Convert(
			TestEnum.Foo,
			typeof(Orientation),
			null,
			CultureInfo.InvariantCulture);

		CornerstoneTest.IsType<BindingNotification>(result);
	}

	[PresentationTestMethod]
	public void DoNotThrowOnInvalidInputForNullableInt()
	{
		var result = DefaultValueConverter.Instance.Convert(
			"<not-a-number>",
			typeof(int?),
			null,
			CultureInfo.InvariantCulture);

		CornerstoneTest.IsType(typeof(BindingNotification), result);
	}

	#endregion

	#region Classes

	[TypeConverter(typeof(CustomTypeConverter))]
	private class CustomType
	{
		#region Constructors

		public CustomType(int value)
		{
			Value = value;
		}

		#endregion

		#region Properties

		public int Value { get; }

		#endregion

		#region Methods

		public override bool Equals(object obj)
		{
			return obj is CustomType other && (Value == other.Value);
		}

		public override int GetHashCode()
		{
			return 8399587 ^ Value.GetHashCode();
		}

		#endregion
	}

	private class CustomTypeConverter : TypeConverter
	{
		#region Methods

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType == typeof(int);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType == typeof(int);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			return new CustomType((int) value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			return ((CustomType) value!).Value;
		}

		#endregion
	}

	private class ExplicitDouble
	{
		#region Constructors

		public ExplicitDouble(double value)
		{
			Value = value;
		}

		#endregion

		#region Properties

		public double Value { get; }

		#endregion

		#region Methods

		public static explicit operator double(ExplicitDouble v)
		{
			return v.Value;
		}

		#endregion
	}

	#endregion

	#region Enumerations

	private enum TestEnum
	{
		Foo,
		Bar
	}

	#endregion
}