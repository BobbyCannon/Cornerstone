#nullable enable

#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Controls.Elements;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Xaml.Data;

[TestClass]
public class BindingTestsMethod : XamlTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void BindingMethodToCommandCollected()
	{
		using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

		WeakReference<ViewModel?> MakeRef()
		{
			var weakVm = new WeakReference<ViewModel?>(null);
			{
				var vm = new ViewModel
				{
					Parameter = null
				};
				weakVm.SetTarget(vm);
				var canExecuteCount = 0;
				var action = new Action<object>(vm.Do);
				var command = new MethodToCommandConverter(action);
				command.CanExecuteChanged += (s, e) => canExecuteCount++;
				vm.Parameter = 0;
				Dispatcher.UIThread.RunJobs();
				vm.Parameter = null;
				Dispatcher.UIThread.RunJobs();
				CornerstoneTest.AreEqual(2, canExecuteCount);
			}
			return weakVm;
		}

		bool IsAlive(WeakReference<ViewModel?> @ref)
		{
			return @ref.TryGetTarget(out var instance)
				&& !(instance is null);
		}

		var vmref = MakeRef();

		var beforeCollect = IsAlive(vmref);

		GC.Collect();
		GC.WaitForPendingFinalizers();

		var afterCollect = IsAlive(vmref);

		CornerstoneTest.IsTrue(beforeCollect, "Invalid ViewModel instance, it is already collected.");
		CornerstoneTest.IsFalse(afterCollect, "ViewModel instance was not collected");
	}

	[PresentationTestMethod]
	public void BindingMethodToCommandWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Button Name='button' Command='{Binding Method}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");
			var vm = new ViewModel();

			button.DataContext = vm;
			window.ApplyTemplate();

			CornerstoneTest.IsNotNull(button.Command);
			PerformClick(button);
			CornerstoneTest.AreEqual("Called", vm.Value);
		}
	}

	[PresentationTestMethod]
	public void BindingMethodToTextBlockTextWorks()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <TextBlock Name='textBlock' Text='{Binding Method}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var textBlock = window.GetControl<TextBlock>("textBlock");
			var vm = new ViewModel();

			textBlock.DataContext = vm;
			window.ApplyTemplate();

			CornerstoneTest.IsNotNull(textBlock.Text);
		}
	}

	[PresentationTestMethod]
	[DataRow(null, "Not called")]
	[DataRow("A", "Do A")]
	public void BindingMethodWithParameterToCommandCanExecute(object? commandParameter, string result)
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Button Name='button' Command='{Binding Do}' CommandParameter='{Binding Parameter, Mode=OneTime}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");
			var vm = new ViewModel
			{
				Parameter = commandParameter
			};

			button.DataContext = vm;
			window.ApplyTemplate();

			CornerstoneTest.IsNotNull(button.Command);
			PerformClick(button);
			CornerstoneTest.AreEqual(vm.Value, result);
		}
	}

	[PresentationTestMethod]
	public void BindingMethodWithParameterToCommandCanExecuteDependsOn()
	{
		using (UnitTestApplication.Start(TestServices.StyledWindow))
		{
			var xaml = @"
<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Cornerstone.Presentation.UnitTests.Markup.Xaml.Xaml;assembly=Cornerstone.Presentation.UnitTests'>
    <Button Name='button' Command='{Binding Do}' CommandParameter='{Binding Parameter, Mode=OneWay}'/>
</Window>";
			var window = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
			var button = window.GetControl<Button>("button");
			var vm = new ViewModel
			{
				Parameter = null
			};

			button.DataContext = vm;
			window.ApplyTemplate();

			CornerstoneTest.IsNotNull(button.Command);

			CornerstoneTest.AreEqual(button.IsEffectivelyEnabled, false);

			vm.Parameter = true;
			Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

			CornerstoneTest.AreEqual(button.IsEffectivelyEnabled, true);
		}
	}

	[PresentationTestMethod]
	public void BindingMethodWithParameterToCommandFailsWithMultipleSingleParameterOverloadsWithoutObject()
	{
		AssertBindingFails(
			"MethodWithOverloads2",
			"Unable to resolve method of name 'MethodWithOverloads2' on type " +
			"'Cornerstone.Presentation.UnitTests.Markup.Xaml.Data.BindingTestsMethod+ViewModel'. " +
			"Found 2 overloads accepting one parameter: 'System.Int32', 'System.String'. " +
			"Expected either a single overload with one parameter, or an overload accepting System.Object.");
	}

	[PresentationTestMethod]
	public void BindingMethodWithParameterToCommandFailsWithoutValidOverloads()
	{
		AssertBindingFails(
			"MethodWithOverloads4",
			"Unable to resolve method of name 'MethodWithOverloads4' on type " +
			"'Cornerstone.Presentation.UnitTests.Markup.Xaml.Data.BindingTestsMethod+ViewModel'. " +
			"Found 2 overloads accepting more than one parameter. " +
			"Expected a method with zero or one parameter.");
	}

	[PresentationTestMethod]
	public void BindingMethodWithParameterToCommandPrefersObjectOverload()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var window = (Window) CornerstoneRuntimeXamlLoader.Load(
			"""
			<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
			        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
			    <Button Name='button' Command='{Binding MethodWithOverloads}' CommandParameter='foo' />
			</Window>
			""");
		var button = window.GetControl<Button>("button");
		var vm = new ViewModel();

		button.DataContext = vm;
		window.ApplyTemplate();

		CornerstoneTest.IsNotNull(button.Command);
		PerformClick(button);
		CornerstoneTest.AreEqual("Called MethodWithOverloads with Object foo", vm.Value);
	}

	[PresentationTestMethod]
	public void BindingMethodWithParameterToCommandUsesParameterlessOverloadWhenNoOverloadsWithParameterExist()
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var window = (Window) CornerstoneRuntimeXamlLoader.Load(
			"""
			<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
			        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
			    <Button Name='button' Command='{Binding MethodWithOverloads3}' CommandParameter='foo' />
			</Window>
			""");
		var button = window.GetControl<Button>("button");
		var vm = new ViewModel();

		button.DataContext = vm;
		window.ApplyTemplate();

		CornerstoneTest.IsNotNull(button.Command);
		PerformClick(button);
		CornerstoneTest.AreEqual("Called MethodWithOverloads3 without parameter", vm.Value);
	}

	[PresentationTestMethod]
	[DataRow("ObjectMethod", "<x:String>hello</x:String>", "Called ObjectMethod with hello")]
	[DataRow("StringMethod", "<x:String>hello</x:String>", "Called StringMethod with hello")]
	[DataRow("Int32Method", "<x:Int32>42</x:Int32>", "Called Int32Method with 42")]
	[DataRow("Int32Method", "<x:String>42</x:String>", "Called Int32Method with 42")]
	[DataRow("VirtualObjectMethod", "<x:String>hello</x:String>", "Called VirtualObjectMethod with hello")]
	[DataRow("VirtualStringMethod", "<x:String>hello</x:String>", "Called VirtualStringMethod with hello")]
	[DataRow("VirtualStringMethod", "<x:Null />", "Called VirtualStringMethod with ")]
	[DataRow("VirtualInt32Method", "<x:Int32>42</x:Int32>", "Called VirtualInt32Method with 42")]
	[DataRow("MethodWithNewSlot", "<x:Int32>42</x:Int32>", "Called MethodWithNewSlot with 42")]
	public void BindingMethodWithParameterToCommandUsesSingleParameterOverload(
		string methodName,
		string xamlParameter,
		string expected)
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var window = (Window) CornerstoneRuntimeXamlLoader.Load(
			$$"""
			<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
			        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
			    <Button Name='button' Command='{Binding {{methodName}}}'>
			      <Button.CommandParameter>
			        {{xamlParameter}}
			      </Button.CommandParameter>
			    </Button>
			</Window>
			""");
		var button = window.GetControl<Button>("button");
		var vm = new ViewModel();

		button.DataContext = vm;
		window.ApplyTemplate();

		CornerstoneTest.IsNotNull(button.Command);
		PerformClick(button);
		CornerstoneTest.AreEqual(expected, vm.Value);
	}

	private static void AssertBindingFails(string methodName, string expectedError)
	{
		using var app = UnitTestApplication.Start(TestServices.StyledWindow);

		var errors = new List<string>();
		using var logSink = TestLogSink.Start((level, area, _, template, values) =>
		{
			if ((level >= LogEventLevel.Warning) && (area == LogArea.Binding))
			{
				errors.Add(template + " " + string.Join(" ", values));
			}
		});

		var window = (Window) CornerstoneRuntimeXamlLoader.Load(
			$$"""
			<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
			        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
			    <Button Name='button' Command='{Binding {{methodName}}}' CommandParameter='foo' />
			</Window>
			""");
		var button = window.GetControl<Button>("button");
		var vm = new ViewModel();

		button.DataContext = vm;
		window.ApplyTemplate();

		CornerstoneTest.IsNull(button.Command);
		CornerstoneTest.Contains(errors, error => error.Contains(expectedError, StringComparison.Ordinal));
	}

	private static void PerformClick(Button button)
	{
		button.RaiseEvent(new KeyEventArgs
		{
			RoutedEvent = InputElement.KeyDownEvent,
			Key = Key.Enter
		});
	}

	#endregion

	#region Classes

	private class ViewModel : ViewModelBase, INotifyPropertyChanged
	{
		#region Fields

		private object? _parameter;

		#endregion

		#region Properties

		public object? Parameter
		{
			get => _parameter;
			set
			{
				if (_parameter == value)
				{
					return;
				}
				_parameter = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Parameter)));
			}
		}

		public string Value { get; private set; } = "Not called";

		#endregion

		#region Methods

		[Metadata.DependsOn(nameof(Parameter))]
		public bool CanDo(object parameter)
		{
			return !ReferenceEquals(null, parameter);
		}

		public void Do(object parameter)
		{
			Value = $"Do {parameter}";
		}

		public void Int32Method(int i)
		{
			Value = $"Called Int32Method with {i}";
		}

		public void Method()
		{
			Value = "Called";
		}

		public new void MethodWithNewSlot(int i)
		{
			Value = $"Called MethodWithNewSlot with {i}";
		}

		public void MethodWithOverloads()
		{
			Value = "Called MethodWithOverloads without parameter";
		}

		public void MethodWithOverloads(int i)
		{
			Value = $"Called MethodWithOverloads with Int32 {i}";
		}

		public void MethodWithOverloads(string i)
		{
			Value = $"Called MethodWithOverloads with String {i}";
		}

		public void MethodWithOverloads(object i)
		{
			Value = $"Called MethodWithOverloads with Object {i}";
		}

		public void MethodWithOverloads2()
		{
			Value = "Called MethodWithOverloads2 without parameter";
		}

		public void MethodWithOverloads2(int i)
		{
			Value = $"Called MethodWithOverloads2 with Int32 {i}";
		}

		public void MethodWithOverloads2(string i)
		{
			Value = $"Called MethodWithOverloads2 with String {i}";
		}

		public void MethodWithOverloads3()
		{
			Value = "Called MethodWithOverloads3 without parameter";
		}

		public void MethodWithOverloads3(int a, int b)
		{
			throw new InvalidOperationException("MethodWithOverloads3 should not be called");
		}

		public void MethodWithOverloads3(string a, string b)
		{
			throw new InvalidOperationException("MethodWithOverloads3 should not be called");
		}

		public void MethodWithOverloads4(int a, int b)
		{
			throw new InvalidOperationException("MethodWithOverloads4 should not be called");
		}

		public void MethodWithOverloads4(string a, string b)
		{
			throw new InvalidOperationException("MethodWithOverloads4 should not be called");
		}

		public void ObjectMethod(object i)
		{
			Value = $"Called ObjectMethod with {i}";
		}

		public void StringMethod(string i)
		{
			Value = $"Called StringMethod with {i}";
		}

		public override void VirtualInt32Method(int i)
		{
			Value = $"Called VirtualInt32Method with {i}";
		}

		public override void VirtualObjectMethod(object? i)
		{
			Value = $"Called VirtualObjectMethod with {i}";
		}

		public override void VirtualStringMethod(string i)
		{
			Value = $"Called VirtualStringMethod with {i}";
		}

		#endregion

		#region Events

		public event PropertyChangedEventHandler? PropertyChanged;

		#endregion
	}

	private class ViewModelBase
	{
		#region Methods

		public void MethodWithNewSlot(int i)
		{
		}

		public virtual void VirtualInt32Method(int i)
		{
		}

		public virtual void VirtualObjectMethod(object? i)
		{
		}

		public virtual void VirtualStringMethod(string i)
		{
		}

		#endregion
	}

	#endregion
}