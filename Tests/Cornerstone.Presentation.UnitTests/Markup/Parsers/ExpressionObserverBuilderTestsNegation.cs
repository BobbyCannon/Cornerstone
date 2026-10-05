#region References

using System;
using System.Collections;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Data.Core.Parsers;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Parsers;

[TestClass]
public class ExpressionObserverBuilderTestsNegation : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void SetValueShouldReturnFalseForInvalidValue()
	{
		var data = new { Foo = "foo" };
		var target = Build(data, "!Foo");
		target.ToObservable().Subscribe(_ => { });

		CornerstoneTest.IsFalse(target.WriteValueToSource("bar"));

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public async Task ShouldNegate0()
	{
		var data = new { Foo = 0 };
		var target = BuildAsObservable(data, "!Foo");
		var result = await target.Take(1);

		CornerstoneTest.IsTrue((bool) result!);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public async Task ShouldNegate1()
	{
		var data = new { Foo = 1 };
		var target = BuildAsObservable(data, "!Foo");
		var result = await target.Take(1);

		CornerstoneTest.IsFalse((bool) result!);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public async Task ShouldNegateBindingNotificationErrorFallbackValue()
	{
		var data = new Test { DataValidationError = "Test error" };
		var target = BuildAsObservable(data, "!Foo", true);
		var result = await target.Take(1);

		CornerstoneTest.AreEqual(new BindingNotification(
			new DataValidationException("Test error"),
			BindingErrorType.DataValidationError,
			true), result);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public async Task ShouldNegateBindingNotificationValue()
	{
		var data = new { Foo = true };
		var target = BuildAsObservable(data, "!Foo", true);
		var result = await target.Take(1);

		CornerstoneTest.AreEqual(new BindingNotification(false), result);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public async Task ShouldNegateFalseString()
	{
		var data = new { Foo = "false" };
		var target = BuildAsObservable(data, "!Foo");
		var result = await target.Take(1);

		CornerstoneTest.IsTrue((bool) result!);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public async Task ShouldNegateTrueString()
	{
		var data = new { Foo = "True" };
		var target = BuildAsObservable(data, "!Foo");
		var result = await target.Take(1);

		CornerstoneTest.IsFalse((bool) result!);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public async Task ShouldPassThroughBindingNotificationError()
	{
		var data = new object();
		var target = BuildAsObservable(data, "!Foo", true);
		var result = await target.Take(1);

		CornerstoneTest.AreEqual(new BindingNotification(
			new BindingChainException("Could not find a matching property accessor for 'Foo' on 'System.Object'.", "!Foo", "Foo"),
			BindingErrorType.Error), result);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public async Task ShouldReturnBindingNotificationForStringNotConvertibleToBoolean()
	{
		var data = new { Foo = "foo" };
		var target = BuildAsObservable(data, "!Foo");
		var result = await target.Take(1);

		CornerstoneTest.AreEqual(new BindingNotification(
			new BindingChainException("Unable to convert 'foo' to bool.", "!Foo", "!"),
			BindingErrorType.Error), result);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public async Task ShouldReturnBindingNotificationForValueNotConvertibleToBoolean()
	{
		var data = new { Foo = new object() };
		var target = BuildAsObservable(data, "!Foo");
		var result = await target.Take(1);

		CornerstoneTest.AreEqual(new BindingNotification(
			new BindingChainException("Unable to convert 'System.Object' to bool.", "!Foo", "!"),
			BindingErrorType.Error), result);

		GC.KeepAlive(data);
	}

	private static BindingExpression Build(object source, string path, bool enableDataValidation = false)
	{
		var r = new CharacterReader(path);
		var grammar = BindingExpressionGrammar.Parse(ref r).Nodes;
		var nodes = ExpressionNodeFactory.CreateFromAst(grammar, null, null, out _);
		return new BindingExpression(
			source,
			nodes,
			PresentationProperty.UnsetValue,
			enableDataValidation: enableDataValidation);
	}

	private static IObservable<object> BuildAsObservable(object source, string path, bool enableDataValidation = false)
	{
		return Build(source, path, enableDataValidation).ToObservable();
	}

	#endregion

	#region Classes

	private class Test : INotifyDataErrorInfo
	{
		#region Fields

		private string _dataValidationError;

		#endregion

		#region Properties

		public object Bar { get; set; }

		public string DataValidationError
		{
			get => _dataValidationError;
			set
			{
				if (value == _dataValidationError)
				{
					return;
				}
				_dataValidationError = value;
				ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(DataValidationError)));
			}
		}

		public bool Foo { get; set; }
		public bool HasErrors => !string.IsNullOrWhiteSpace(DataValidationError);

		#endregion

		#region Methods

		public IEnumerable GetErrors(string propertyName)
		{
			return DataValidationError is not null ? new[] { DataValidationError } : [];
		}

		#endregion

		#region Events

		public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

		#endregion
	}

	#endregion
}