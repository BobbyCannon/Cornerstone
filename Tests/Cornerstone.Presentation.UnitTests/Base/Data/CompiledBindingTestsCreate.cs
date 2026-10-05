#region References

using System;
using System.Globalization;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data;

[TestClass]
public class CompiledBindingTestsCreate
{
	#region Methods

	[PresentationTestMethod]
	public void BindingShouldUpdateWhenSourcePropertyChanges()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new TextBlock();
			var viewModel = new TestViewModel { StringProperty = "Initial" };
			var binding = CompiledBinding.Create<TestViewModel, string>(
				vm => vm.StringProperty,
				viewModel);

			target.Bind(TextBlock.TextProperty, binding);
			CornerstoneTest.AreEqual("Initial", target.Text);

			viewModel.StringProperty = "Updated";
			CornerstoneTest.AreEqual("Updated", target.Text);
		}
	}

	[PresentationTestMethod]
	public void BindingShouldUseDataContextWhenNoSourceSpecified()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new TextBlock();
			var viewModel = new TestViewModel { StringProperty = "FromDataContext" };
			var binding = CompiledBinding.Create<TestViewModel, string>(vm => vm.StringProperty);

			target.DataContext = viewModel;
			target.Bind(TextBlock.TextProperty, binding);

			CornerstoneTest.AreEqual("FromDataContext", target.Text);
		}
	}

	[PresentationTestMethod]
	public void BindingShouldWorkWhenAppliedToControl()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var target = new TextBlock();
			var viewModel = new TestViewModel { StringProperty = "Hello" };
			var binding = CompiledBinding.Create<TestViewModel, string>(
				vm => vm.StringProperty,
				viewModel);

			target.Bind(TextBlock.TextProperty, binding);

			CornerstoneTest.AreEqual("Hello", target.Text);
		}
	}

	[PresentationTestMethod]
	public void CreateShouldApplyConverter()
	{
		var converter = new TestConverter();
		var binding = CompiledBinding.Create<TestViewModel, string>(
			vm => vm.StringProperty,
			converter: converter);

		CornerstoneTest.Same(converter, binding.Converter);
	}

	[PresentationTestMethod]
	public void CreateShouldApplyMode()
	{
		var binding = CompiledBinding.Create<TestViewModel, string>(
			vm => vm.StringProperty,
			mode: BindingMode.TwoWay);

		CornerstoneTest.AreEqual(BindingMode.TwoWay, binding.Mode);
	}

	[PresentationTestMethod]
	public void CreateShouldCreateBindingWithSimpleProperty()
	{
		var binding = CompiledBinding.Create<TestViewModel, string>(vm => vm.StringProperty);

		CornerstoneTest.IsNotNull(binding);
		CornerstoneTest.IsNotNull(binding.Path);
		CornerstoneTest.AreEqual("StringProperty", binding.Path.ToString());
		CornerstoneTest.AreEqual(PresentationProperty.UnsetValue, binding.Source);
		CornerstoneTest.AreEqual(BindingMode.Default, binding.Mode);
	}

	[PresentationTestMethod]
	public void CreateShouldCreateBindingWithSource()
	{
		var source = new TestViewModel { StringProperty = "Test" };
		var binding = CompiledBinding.Create<TestViewModel, string>(
			vm => vm.StringProperty,
			source);

		CornerstoneTest.IsNotNull(binding);
		CornerstoneTest.IsNotNull(binding.Path);
		CornerstoneTest.AreEqual("StringProperty", binding.Path.ToString());
		CornerstoneTest.Same(source, binding.Source);
	}

	[PresentationTestMethod]
	public void CreateShouldWorkWithIndexer()
	{
		var binding = CompiledBinding.Create<TestViewModel, string>(vm => vm.Items[0]);

		CornerstoneTest.IsNotNull(binding);
		CornerstoneTest.IsNotNull(binding.Path);
		CornerstoneTest.AreEqual("Items[0]", binding.Path.ToString());
	}

	[PresentationTestMethod]
	public void CreateShouldWorkWithNestedProperties()
	{
		var binding = CompiledBinding.Create<TestViewModel, string>(vm => vm.Child!.StringProperty);

		CornerstoneTest.IsNotNull(binding);
		CornerstoneTest.IsNotNull(binding.Path);
		CornerstoneTest.AreEqual("Child.StringProperty", binding.Path.ToString());
	}

	#endregion

	#region Classes

	private class TestConverter : IValueConverter
	{
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

	private class TestViewModel : NotifyingBase
	{
		#region Fields

		private TestViewModel _child;
		private string _stringProperty;

		#endregion

		#region Properties

		public TestViewModel Child
		{
			get => _child;
			set
			{
				_child = value;
				RaisePropertyChanged();
			}
		}

		public string[] Items { get; } = Array.Empty<string>();

		public string StringProperty
		{
			get => _stringProperty;
			set
			{
				_stringProperty = value;
				RaisePropertyChanged();
			}
		}

		#endregion
	}

	#endregion
}