#region References

using System;
using System.Globalization;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core;

public abstract partial class BindingExpressionTests
{
	#region Methods

	[PresentationTestMethod]
	public void SetterShouldConvertDoubleToString()
	{
		var data = new ViewModel { StringValue = $"{5.6}" };
		var target = CreateTargetWithSource(
			data,
			o => o.StringValue,
			mode: BindingMode.TwoWay,
			targetProperty: TargetClass.DoubleProperty);

		target.Double = 6.7;

		CornerstoneTest.AreEqual($"{6.7}", data.StringValue);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void SetterShouldConvertStringToDouble()
	{
		var data = new ViewModel { DoubleValue = 5.6 };
		var target = CreateTargetWithSource(
			data,
			o => o.DoubleValue,
			mode: BindingMode.TwoWay,
			targetProperty: TargetClass.StringProperty);

		target.String = $"{6.7}";

		CornerstoneTest.AreEqual(6.7, data.DoubleValue);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void SettingInvalidDoubleStringShouldNotChangeTarget()
	{
		var data = new ViewModel { DoubleValue = 5.6 };
		var target = CreateTargetWithSource(
			data,
			o => o.DoubleValue,
			mode: BindingMode.TwoWay,
			targetProperty: TargetClass.StringProperty);

		target.String = "foo";

		CornerstoneTest.AreEqual(5.6, data.DoubleValue);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void SettingInvalidDoubleStringShouldUseFallbackValue()
	{
		var data = new ViewModel { DoubleValue = 5.6 };
		var target = CreateTargetWithSource(
			data,
			o => o.DoubleValue,
			mode: BindingMode.TwoWay,
			fallbackValue: 9.8,
			targetProperty: TargetClass.StringProperty);

		target.String = "foo";

		CornerstoneTest.AreEqual(9.8, data.DoubleValue);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldPassConverterParameterToConverterConvertBack()
	{
		var data = new ViewModel { StringValue = "Initial" };
		var converter = new PrefixConverter();
		var target = CreateTargetWithSource(
			data,
			o => o.StringValue,
			converter: converter,
			converterParameter: "foo",
			mode: BindingMode.TwoWay);

		target.String = "fooBar";

		CornerstoneTest.AreEqual("Bar", data.StringValue);
	}

	[PresentationTestMethod]
	public void ShouldUseConverterWhenWritingToSource()
	{
		var data = new ViewModel { StringValue = "foo" };
		var target = CreateTargetWithSource(
			data,
			o => o.StringValue,
			mode: BindingMode.TwoWay,
			converter: new CaseConverter());

		target.String = "BaR";
		CornerstoneTest.AreEqual("bar", data.StringValue);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldWriteIndexedValueToSource()
	{
		var data = new { Foo = new[] { "foo" } };
		var target = CreateTargetWithSource(data, o => o.Foo[0], mode: BindingMode.TwoWay);

		target.String = "bar";

		CornerstoneTest.AreEqual("bar", data.Foo[0]);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldWriteValueToAttachedPropertyOnSource()
	{
		var data = new PresentationObject();
		var target = CreateTargetWithSource(
			data,
			o => o[DockPanel.DockProperty],
			mode: BindingMode.TwoWay,
			targetProperty: Control.TagProperty);

		target.Tag = Dock.Right;

		CornerstoneTest.AreEqual(Dock.Right, data[DockPanel.DockProperty]);
	}

	[PresentationTestMethod]
	public void ShouldWriteValueToSource()
	{
		var data = new ViewModel { StringValue = "foo" };
		var target = CreateTargetWithSource(data, o => o.StringValue, mode: BindingMode.TwoWay);

		target.String = "bar";

		CornerstoneTest.AreEqual("bar", data.StringValue);
	}

	[PresentationTestMethod]
	public void ShouldWriteValueToSourceOnSimplePropertyChain()
	{
		var data = new ViewModel { Next = new() { StringValue = "foo" } };
		var target = CreateTargetWithSource(data, o => o.Next!.StringValue, mode: BindingMode.TwoWay);

		target.String = "bar";

		CornerstoneTest.AreEqual("bar", data.Next!.StringValue);
	}

	[PresentationTestMethod]
	public void TargetValueCanBeSetOnBrokenChain()
	{
		var data = new ViewModel { Next = new() { StringValue = "foo" } };
		var target = CreateTargetWithSource(data, o => o.Next!.StringValue, mode: BindingMode.TwoWay);

		data.Next = null;
		target.String = "bar";

		CornerstoneTest.AreEqual("bar", target.String);
	}

	[PresentationTestMethod]
	public void TwoWayBindingShouldNotWriteUnchangedValueBackToPropertyWithConverter()
	{
		var data = new Cat();
		var target = CreateTargetWithSource(
			data,
			o => o.WhiskerCount,
			converter: new CaseConverter(),
			mode: BindingMode.TwoWay);

		CornerstoneTest.AreEqual(4, target.Int);
		CornerstoneTest.AreEqual(9, data.Lives);

		data.WhiskerCount = 3;

		CornerstoneTest.AreEqual(3, target.Int);
		CornerstoneTest.AreEqual(8, data.Lives);

		GC.KeepAlive(data);
	}

	#endregion

	#region Classes

	private class CaseConverter : IValueConverter
	{
		#region Fields

		public static readonly CaseConverter Instance = new();

		#endregion

		#region Methods

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value?.ToString()?.ToUpper();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value?.ToString()?.ToLower();
		}

		#endregion
	}

	private class Cat : NotifyingBase
	{
		#region Fields

		private int _whiskerCount = 4;

		#endregion

		#region Properties

		public int Lives { get; private set; } = 9;

		public int WhiskerCount
		{
			get => _whiskerCount;
			set
			{
				_whiskerCount = value;
				RaisePropertyChanged(nameof(WhiskerCount));
				--Lives;
			}
		}

		#endregion
	}

	#endregion
}