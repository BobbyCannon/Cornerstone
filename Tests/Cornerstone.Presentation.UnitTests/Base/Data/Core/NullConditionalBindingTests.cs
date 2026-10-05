#region References

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Logging;
using Cornerstone.Presentation.Markup.Xaml;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core;

/// <summary>
/// Tests for null-conditional operator in binding paths.
/// </summary>
/// <remarks>
/// Ideally these would be part of the <see cref="BindingExpressionTests" /> suite but that uses
/// C# expression trees as an abstraction to represent both reflection and compiled binding paths.
/// This is a problem because expression trees don't support the C# null-conditional operator
/// and I have no desire to refactor all of those tests right now.
/// </remarks>
[TestClass]
[SkipInAot("Runtime XAML compiles with SRE (Reflection.Emit), which Native AOT does not support.")]
public class NullConditionalBindingTests
{
	#region Methods

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void ShouldNotReportErrorWithNullConditionalOperatorBeforeMethodForAvaloniaProperty(bool compileBindings)
	{
		using var app = Start();
		using var log = TestLogger.Create();
		var xaml = $$$"""
					<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
					        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
					        xmlns:local='using:Cornerstone.Presentation.UnitTests.Base.Data.Core'
					        x:DataType='local:NullConditionalBindingTests+First'
					        x:CompileBindings='{{{compileBindings}}}'>
					    <local:ErrorCollectingTextBox Text='{Binding StyledSecond.StyledThird?.Greeting}'/>
					</Window>
					""";
		var data = new First { StyledSecond = new Second() };
		var window = CreateTarget(xaml, data);
		var textBox = CornerstoneTest.IsType<ErrorCollectingTextBox>(window.Content);

		CornerstoneTest.IsNull(textBox.Text);
		CornerstoneTest.IsNull(textBox.Error);
		CornerstoneTest.AreEqual(BindingValueType.Value, textBox.ErrorState);
		CornerstoneTest.Empty(log.Messages);
	}

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void ShouldNotReportErrorWithNullConditionalOperatorBeforeMethodForClrProperty(bool compileBindings)
	{
		using var app = Start();
		using var log = TestLogger.Create();
		var xaml = $$$"""
					<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
					        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
					        xmlns:local='using:Cornerstone.Presentation.UnitTests.Base.Data.Core'
					        x:DataType='local:NullConditionalBindingTests+First'
					        x:CompileBindings='{{{compileBindings}}}'>
					    <local:ErrorCollectingTextBox Text='{Binding Second.Third?.Greeting}'/>
					</Window>
					""";
		var data = new First { Second = new Second() };
		var window = CreateTarget(xaml, data);
		var textBox = CornerstoneTest.IsType<ErrorCollectingTextBox>(window.Content);

		CornerstoneTest.IsNull(textBox.Text);
		CornerstoneTest.IsNull(textBox.Error);
		CornerstoneTest.AreEqual(BindingValueType.Value, textBox.ErrorState);
		CornerstoneTest.Empty(log.Messages);
	}

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void ShouldNotReportErrorWithNullConditionalOperatorForAttachedProperty(bool compileBindings)
	{
		using var app = Start();
		using var log = TestLogger.Create();
		var xaml = $$$"""
					<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
					        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
					        xmlns:local='using:Cornerstone.Presentation.UnitTests.Base.Data.Core'
					        x:DataType='local:NullConditionalBindingTests+First'
					        x:CompileBindings='{{{compileBindings}}}'>
					    <local:ErrorCollectingTextBox Text='{Binding Second?.(Grid.Row)}'/>
					</Window>
					""";
		var data = new First { Second = null };
		var window = CreateTarget(xaml, data);
		var textBox = CornerstoneTest.IsType<ErrorCollectingTextBox>(window.Content);

		CornerstoneTest.IsNull(textBox.Text);
		CornerstoneTest.IsNull(textBox.Error);
		CornerstoneTest.AreEqual(BindingValueType.Value, textBox.ErrorState);
		CornerstoneTest.Empty(log.Messages);
	}

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void ShouldNotReportErrorWithNullConditionalOperatorForAvaloniaProperty(bool compileBindings)
	{
		using var app = Start();
		using var log = TestLogger.Create();
		var xaml = $$$"""
					<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
					        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
					        xmlns:local='using:Cornerstone.Presentation.UnitTests.Base.Data.Core'
					        x:DataType='local:NullConditionalBindingTests+First'
					        x:CompileBindings='{{{compileBindings}}}'>
					    <local:ErrorCollectingTextBox Text='{Binding StyledSecond.StyledThird?.StyledFinal}'/>
					</Window>
					""";
		var data = new First { StyledSecond = new Second() };
		var window = CreateTarget(xaml, data);
		var textBox = CornerstoneTest.IsType<ErrorCollectingTextBox>(window.Content);

		CornerstoneTest.IsNull(textBox.Text);
		CornerstoneTest.IsNull(textBox.Error);
		CornerstoneTest.AreEqual(BindingValueType.Value, textBox.ErrorState);
		CornerstoneTest.Empty(log.Messages);
	}

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void ShouldNotReportErrorWithNullConditionalOperatorForClrProperty1(bool compileBindings)
	{
		using var app = Start();
		using var log = TestLogger.Create();
		var xaml = $$$"""
					<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
					        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
					        xmlns:local='using:Cornerstone.Presentation.UnitTests.Base.Data.Core'
					        x:DataType='local:NullConditionalBindingTests+First'
					        x:CompileBindings='{{{compileBindings}}}'>
					    <local:ErrorCollectingTextBox Text='{Binding Second?.Third.Final}'/>
					</Window>
					""";
		var data = new First { Second = null };
		var window = CreateTarget(xaml, data);
		var textBox = CornerstoneTest.IsType<ErrorCollectingTextBox>(window.Content);

		CornerstoneTest.IsNull(textBox.Text);
		CornerstoneTest.IsNull(textBox.Error);
		CornerstoneTest.AreEqual(BindingValueType.Value, textBox.ErrorState);
		CornerstoneTest.Empty(log.Messages);
	}

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void ShouldNotReportErrorWithNullConditionalOperatorForClrProperty2(bool compileBindings)
	{
		using var app = Start();
		using var log = TestLogger.Create();
		var xaml = $$$"""
					<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
					        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
					        xmlns:local='using:Cornerstone.Presentation.UnitTests.Base.Data.Core'
					        x:DataType='local:NullConditionalBindingTests+First'
					        x:CompileBindings='{{{compileBindings}}}'>
					    <local:ErrorCollectingTextBox Text='{Binding Second.Third?.Final}'/>
					</Window>
					""";
		var data = new First { Second = new Second() };
		var window = CreateTarget(xaml, data);
		var textBox = CornerstoneTest.IsType<ErrorCollectingTextBox>(window.Content);

		CornerstoneTest.IsNull(textBox.Text);
		CornerstoneTest.IsNull(textBox.Error);
		CornerstoneTest.AreEqual(BindingValueType.Value, textBox.ErrorState);
		CornerstoneTest.Empty(log.Messages);
	}

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void ShouldNotReportErrorWithNullConditionalOperatorForStream(bool compileBindings)
	{
		using var app = Start();
		using var log = TestLogger.Create();
		var xaml = $$$"""
					<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
					        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
					        xmlns:local='using:Cornerstone.Presentation.UnitTests.Base.Data.Core'
					        x:DataType='local:NullConditionalBindingTests+First'
					        x:CompileBindings='{{{compileBindings}}}'>
					    <local:ErrorCollectingTextBox Text='{Binding Second?.Task^}'/>
					</Window>
					""";
		var data = new First { Second = null };
		var window = CreateTarget(xaml, data);
		var textBox = CornerstoneTest.IsType<ErrorCollectingTextBox>(window.Content);

		CornerstoneTest.IsNull(textBox.Text);
		CornerstoneTest.IsNull(textBox.Error);
		CornerstoneTest.AreEqual(BindingValueType.Value, textBox.ErrorState);
		CornerstoneTest.Empty(log.Messages);
	}

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void ShouldReportErrorWithoutNullConditionalOperatorForAvaloniaProperty(bool compileBindings)
	{
		// Testing the baseline: should report a null error without null conditionals.
		using var app = Start();
		using var log = TestLogger.Create();

		var xaml = $$$"""
					<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
					        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
					        xmlns:local='using:Cornerstone.Presentation.UnitTests.Base.Data.Core'
					        x:DataType='local:NullConditionalBindingTests+First'
					        x:CompileBindings='{{{compileBindings}}}'>
					    <local:ErrorCollectingTextBox Text='{Binding StyledSecond.StyledThird.StyledFinal}'/>
					</Window>
					""";
		var data = new First { StyledSecond = new Second() };
		var window = CreateTarget(xaml, data);
		var textBox = CornerstoneTest.IsType<ErrorCollectingTextBox>(window.Content);
		var error = CornerstoneTest.IsType<BindingChainException>(textBox.Error);
		var message = CornerstoneTest.Single(log.Messages);

		CornerstoneTest.IsNull(textBox.Text);
		CornerstoneTest.AreEqual("StyledSecond.StyledThird.StyledFinal", error.Expression);
		CornerstoneTest.AreEqual("StyledThird", error.ExpressionErrorPoint);
		CornerstoneTest.AreEqual(BindingValueType.BindingError, textBox.ErrorState);
		CornerstoneTest.AreEqual("An error occurred binding {Property} to {Expression} at {ExpressionErrorPoint}: {Message}", message);
	}

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void ShouldReportErrorWithoutNullConditionalOperatorForClrProperty(bool compileBindings)
	{
		// Testing the baseline: should report a null error without null conditionals.
		using var app = Start();
		using var log = TestLogger.Create();

		var xaml = $$$"""
					<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
					        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
					        xmlns:local='using:Cornerstone.Presentation.UnitTests.Base.Data.Core'
					        x:DataType='local:NullConditionalBindingTests+First'
					        x:CompileBindings='{{{compileBindings}}}'>
					    <local:ErrorCollectingTextBox Text='{Binding Second.Third.Final}'/>
					</Window>
					""";
		var data = new First { Second = new Second() };
		var window = CreateTarget(xaml, data);
		var textBox = CornerstoneTest.IsType<ErrorCollectingTextBox>(window.Content);
		var error = CornerstoneTest.IsType<BindingChainException>(textBox.Error);
		var message = CornerstoneTest.Single(log.Messages);

		CornerstoneTest.IsNull(textBox.Text);
		CornerstoneTest.AreEqual("Second.Third.Final", error.Expression);
		CornerstoneTest.AreEqual("Third", error.ExpressionErrorPoint);
		CornerstoneTest.AreEqual(BindingValueType.BindingError, textBox.ErrorState);
		CornerstoneTest.AreEqual("An error occurred binding {Property} to {Expression} at {ExpressionErrorPoint}: {Message}", message);
	}

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void ShouldUseTargetNullValueWithNullConditionalOperatorForAvaloniaProperty(bool compileBindings)
	{
		using var app = Start();
		using var log = TestLogger.Create();
		var xaml = $$$"""
					<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
					        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
					        xmlns:local='using:Cornerstone.Presentation.UnitTests.Base.Data.Core'
					        x:DataType='local:NullConditionalBindingTests+First'
					        x:CompileBindings='{{{compileBindings}}}'>
					    <local:ErrorCollectingTextBox Text='{Binding StyledSecond.StyledThird?.StyledFinal, TargetNullValue=ItsNull}'/>
					</Window>
					""";
		var data = new First { StyledSecond = new Second() };
		var window = CreateTarget(xaml, data);
		var textBox = CornerstoneTest.IsType<ErrorCollectingTextBox>(window.Content);

		CornerstoneTest.AreEqual("ItsNull", textBox.Text);
		CornerstoneTest.IsNull(textBox.Error);
		CornerstoneTest.AreEqual(BindingValueType.Value, textBox.ErrorState);
		CornerstoneTest.Empty(log.Messages);
	}

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void ShouldUseTargetNullValueWithNullConditionalOperatorForClrProperty(bool compileBindings)
	{
		using var app = Start();
		using var log = TestLogger.Create();
		var xaml = $$$"""
					<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
					        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
					        xmlns:local='using:Cornerstone.Presentation.UnitTests.Base.Data.Core'
					        x:DataType='local:NullConditionalBindingTests+First'
					        x:CompileBindings='{{{compileBindings}}}'>
					    <local:ErrorCollectingTextBox Text='{Binding Second.Third?.Final, TargetNullValue=ItsNull}'/>
					</Window>
					""";
		var data = new First { Second = new Second() };
		var window = CreateTarget(xaml, data);
		var textBox = CornerstoneTest.IsType<ErrorCollectingTextBox>(window.Content);

		CornerstoneTest.AreEqual("ItsNull", textBox.Text);
		CornerstoneTest.IsNull(textBox.Error);
		CornerstoneTest.AreEqual(BindingValueType.Value, textBox.ErrorState);
		CornerstoneTest.Empty(log.Messages);
	}

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void ShouldUseTargetNullValueWithShortCircuitedNullConditionalOperatorForAvaloniaProperty(bool compileBindings)
	{
		using var app = Start();
		using var log = TestLogger.Create();
		var xaml = $$$"""
					<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
					        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
					        xmlns:local='using:Cornerstone.Presentation.UnitTests.Base.Data.Core'
					        x:DataType='local:NullConditionalBindingTests+First'
					        x:CompileBindings='{{{compileBindings}}}'>
					    <local:ErrorCollectingTextBox Text='{Binding StyledSecond?.StyledThird.StyledFinal, TargetNullValue=ItsNull}'/>
					</Window>
					""";
		var data = new First { StyledSecond = null };
		var window = CreateTarget(xaml, data);
		var textBox = CornerstoneTest.IsType<ErrorCollectingTextBox>(window.Content);

		CornerstoneTest.AreEqual("ItsNull", textBox.Text);
		CornerstoneTest.IsNull(textBox.Error);
		CornerstoneTest.AreEqual(BindingValueType.Value, textBox.ErrorState);
		CornerstoneTest.Empty(log.Messages);
	}

	[PresentationTestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void ShouldUseTargetNullValueWithShortCircuitedNullConditionalOperatorForClrProperty(bool compileBindings)
	{
		using var app = Start();
		using var log = TestLogger.Create();
		var xaml = $$$"""
					<Window xmlns='https://github.com/BobbyCannon/Cornerstone'
					        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
					        xmlns:local='using:Cornerstone.Presentation.UnitTests.Base.Data.Core'
					        x:DataType='local:NullConditionalBindingTests+First'
					        x:CompileBindings='{{{compileBindings}}}'>
					    <local:ErrorCollectingTextBox Text='{Binding Second?.Third.Final, TargetNullValue=ItsNull}'/>
					</Window>
					""";
		var data = new First { Second = null };
		var window = CreateTarget(xaml, data);
		var textBox = CornerstoneTest.IsType<ErrorCollectingTextBox>(window.Content);

		CornerstoneTest.AreEqual("ItsNull", textBox.Text);
		CornerstoneTest.IsNull(textBox.Error);
		CornerstoneTest.AreEqual(BindingValueType.Value, textBox.ErrorState);
		CornerstoneTest.Empty(log.Messages);
	}

	private Window CreateTarget(string xaml, object data)
	{
		var result = (Window) CornerstoneRuntimeXamlLoader.Load(xaml);
		result.DataContext = data;
		result.Show();
		return result;
	}

	private static IDisposable Start()
	{
		return UnitTestApplication.Start(TestServices.StyledWindow);
	}

	#endregion

	#region Classes

	public class First : StyledElement
	{
		#region Fields

		public static readonly StyledProperty<Second> StyledSecondProperty =
			PresentationProperty.Register<First, Second>(nameof(StyledSecond));

		#endregion

		#region Properties

		public Second Second { get; set; }

		public Second StyledSecond
		{
			get => GetValue(StyledSecondProperty);
			set => SetValue(StyledSecondProperty, value);
		}

		#endregion
	}

	public class Second : StyledElement
	{
		#region Fields

		public static readonly StyledProperty<Third> StyledThirdProperty =
			PresentationProperty.Register<Second, Third>(nameof(StyledThird));

		#endregion

		#region Properties

		public Third StyledThird
		{
			get => GetValue(StyledThirdProperty);
			set => SetValue(StyledThirdProperty, value);
		}

		public Task<string> Task { get; set; }

		public Third Third { get; set; }

		#endregion
	}

	public class Third : StyledElement
	{
		#region Fields

		public static readonly StyledProperty<string> StyledFinalProperty =
			PresentationProperty.Register<Third, string>(nameof(StyledFinal));

		#endregion

		#region Properties

		public string Final { get; set; }

		public string StyledFinal
		{
			get => GetValue(StyledFinalProperty);
			set => SetValue(StyledFinalProperty, value);
		}

		#endregion

		#region Methods

		public string Greeting()
		{
			return "Hello!";
		}

		#endregion
	}

	private class TestLogger : ILogSink, IDisposable
	{
		#region Constructors

		private TestLogger()
		{
		}

		#endregion

		#region Properties

		public IList<string> Messages { get; } = [];

		#endregion

		#region Methods

		public static TestLogger Create()
		{
			var result = new TestLogger();
			Logger.Sink = result;
			return result;
		}

		public void Dispose()
		{
			Logger.Sink = null;
		}

		public bool IsEnabled(LogEventLevel level, string area)
		{
			return (level >= LogEventLevel.Warning) && (area == LogArea.Binding);
		}

		public void Log(LogEventLevel level, string area, object source, string messageTemplate)
		{
			Messages.Add(messageTemplate);
		}

		public void Log(LogEventLevel level, string area, object source, string messageTemplate, params object[] propertyValues)
		{
			Messages.Add(messageTemplate);
		}

		#endregion
	}

	#endregion
}