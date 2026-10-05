#region References

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Converters;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Leak;

[TestClass]
public class BindingExpressionTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void ShouldNotKeepSourceAliveMethodBinding()
	{
		static WeakReference CreateExpression()
		{
			var methodBound = new MethodBound();
			var source = new { Foo = methodBound };
			var target = CreateBindingExpression(source, o => (Action) o.Foo.A);
			target.ToObservable().Subscribe(_ => { });
			return new WeakReference(methodBound);
		}

		var weakSource = CreateExpression();
		CornerstoneTest.IsTrue(weakSource.IsAlive);

		GC.Collect();

		CornerstoneTest.IsFalse(weakSource.IsAlive);
	}

	[PresentationTestMethod]
	public void ShouldNotKeepSourceAliveNonIntegerIndexer()
	{
		static WeakReference CreateExpression()
		{
			var indexer = new NonIntegerIndexer();
			var source = new { Foo = indexer };
			var target = CreateBindingExpression(source, o => o.Foo);

			target.ToObservable().Subscribe(_ => { });
			return new WeakReference(indexer);
		}

		var weakSource = CreateExpression();
		CornerstoneTest.IsTrue(weakSource.IsAlive);

		GC.Collect();

		CornerstoneTest.IsFalse(weakSource.IsAlive);
	}

	[PresentationTestMethod]
	public void ShouldNotKeepSourceAliveObservableCollection()
	{
		static WeakReference CreateExpression()
		{
			var list = new OldPresentationList<string> { "foo", "bar" };
			var source = new { Foo = list };
			var target = CreateBindingExpression(source, o => o.Foo);

			target.ToObservable().Subscribe(_ => { });
			return new WeakReference(list);
		}

		var weakSource = CreateExpression();
		CornerstoneTest.IsTrue(weakSource.IsAlive);

		GC.Collect();

		CornerstoneTest.IsFalse(weakSource.IsAlive);
	}

	[PresentationTestMethod]
	public void ShouldNotKeepSourceAliveObservableCollectionWithDataValidation()
	{
		static WeakReference CreateExpression()
		{
			var list = new OldPresentationList<string> { "foo", "bar" };
			var source = new { Foo = list };
			var target = CreateBindingExpression(source, o => o.Foo, enableDataValidation: true);

			target.ToObservable().Subscribe(_ => { });
			return new WeakReference(list);
		}

		var weakSource = CreateExpression();
		CornerstoneTest.IsTrue(weakSource.IsAlive);

		GC.Collect();

		CornerstoneTest.IsFalse(weakSource.IsAlive);
	}

	private static BindingExpression CreateBindingExpression<TIn, TOut>(
		TIn source,
		Expression<Func<TIn, TOut>> expression,
		IValueConverter converter = null,
		CultureInfo converterCulture = null,
		object converterParameter = null,
		bool enableDataValidation = false,
		Optional<object> fallbackValue = default,
		BindingMode mode = BindingMode.OneWay,
		BindingPriority priority = BindingPriority.LocalValue,
		object targetNullValue = null,
		bool allowReflection = true)
		where TIn : class
	{
		return BindingExpressionExtensions.CreateBindingExpression(
			source,
			expression,
			converter,
			converterCulture,
			converterParameter,
			enableDataValidation,
			fallbackValue,
			mode,
			priority,
			targetNullValue,
			allowReflection);
	}

	#endregion

	#region Classes

	private class MethodBound
	{
		#region Methods

		public void A()
		{
		}

		#endregion
	}

	private class NonIntegerIndexer : NotifyingBase
	{
		#region Fields

		private readonly Dictionary<string, string> _storage = new();

		#endregion

		#region Properties

		public string this[string key]
		{
			get => _storage[key];
			set
			{
				_storage[key] = value;
				RaisePropertyChanged(CommonPropertyNames.IndexerName);
			}
		}

		#endregion
	}

	#endregion
}