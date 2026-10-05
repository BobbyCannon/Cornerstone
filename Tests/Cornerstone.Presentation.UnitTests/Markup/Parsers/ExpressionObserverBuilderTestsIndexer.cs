#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Data.Core.Parsers;
using Cornerstone.Presentation.Diagnostics;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Presentation.Utilities;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Markup.Parsers;

[TestClass]
public class ExpressionObserverBuilderTestsIndexer : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public async Task ArrayOutOfBoundsShouldReturnUnsetValue()
	{
		var data = new { Foo = new[] { "foo", "bar" } };
		var target = BuildAsObservable(data, "Foo[2]");
		var result = await target.Take(1);

		CornerstoneTest.AreEqual(PresentationProperty.UnsetValue, BindingNotification.ExtractValue(result));

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public async Task ArrayWithWrongDimensionsShouldReturnUnsetValue()
	{
		var data = new { Foo = new[] { "foo", "bar" } };
		var target = BuildAsObservable(data, "Foo[1,2]");
		var result = await target.Take(1);

		CornerstoneTest.AreEqual(PresentationProperty.UnsetValue, BindingNotification.ExtractValue(result));

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public async Task IndexerOnlyBindingWorks()
	{
		var data = new[] { 1, 2, 3 };

		var target = BuildAsObservable(data, "[1]");

		var value = await target.Take(1);

		CornerstoneTest.AreEqual(data[1], value);
	}

	[PresentationTestMethod]
	public async Task ListOutOfBoundsShouldReturnUnsetValue()
	{
		var data = new { Foo = new List<string> { "foo", "bar" } };
		var target = BuildAsObservable(data, "Foo[2]");
		var result = await target.Take(1);

		CornerstoneTest.AreEqual(PresentationProperty.UnsetValue, BindingNotification.ExtractValue(result));

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldAddNewDictionaryEntry()
	{
		var data = new
		{
			Foo = new Dictionary<string, int>
			{
				{ "foo", 1 }
			}
		};

		var target = Build(data, "Foo[bar]");
		using (target.ToObservable().Subscribe(_ => { }))
		{
			CornerstoneTest.IsTrue(target.WriteValueToSource(4));
		}

		CornerstoneTest.AreEqual(4, data.Foo["bar"]);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public async Task ShouldGetArrayValue()
	{
		var data = new { Foo = new[] { "foo", "bar" } };
		var target = BuildAsObservable(data, "Foo[1]");
		var result = await target.Take(1);

		CornerstoneTest.AreEqual("bar", result);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public async Task ShouldGetErrorForObjectWithoutIndexer()
	{
		var data = new { Foo = 5 };
		var target = BuildAsObservable(data, "Foo[noindexer]");
		var result = await target.Take(1);

		CornerstoneTest.AreEqual(new BindingNotification(
			new BindingChainException("Type 'System.Int32' does not have an indexer.", "Foo[noindexer]", "[noindexer]"),
			BindingErrorType.Error), result);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public async Task ShouldGetListValue()
	{
		var data = new { Foo = new List<string> { "foo", "bar" } };
		var target = BuildAsObservable(data, "Foo[1]");
		var result = await target.Take(1);

		CornerstoneTest.AreEqual("bar", result);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public async Task ShouldGetMultiDimensionalArrayValue()
	{
		var data = new { Foo = new[,] { { "foo", "bar" }, { "baz", "qux" } } };
		var target = BuildAsObservable(data, "Foo[1, 1]");
		var result = await target.Take(1);

		CornerstoneTest.AreEqual("qux", result);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public async Task ShouldGetUnsetValueForInvalidArrayIndex()
	{
		var data = new { Foo = new[] { "foo", "bar" } };
		var target = BuildAsObservable(data, "Foo[invalid]");
		var result = await target.Take(1);

		CornerstoneTest.AreEqual(PresentationProperty.UnsetValue, BindingNotification.ExtractValue(result));

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public async Task ShouldGetUnsetValueForInvalidDictionaryIndex()
	{
		var data = new { Foo = new Dictionary<int, string> { { 1, "foo" } } };
		var target = BuildAsObservable(data, "Foo[invalid]");
		var result = await target.Take(1);

		CornerstoneTest.AreEqual(PresentationProperty.UnsetValue, BindingNotification.ExtractValue(result));

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public async Task ShouldGetValueForNonStringIndexer()
	{
		var data = new { Foo = new Dictionary<double, string> { { 1.0, "bar" }, { 2.0, "qux" } } };
		var target = BuildAsObservable(data, "Foo[1.0]");
		var result = await target.Take(1);

		CornerstoneTest.AreEqual("bar", result);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public async Task ShouldGetValueForStringIndexer()
	{
		var data = new { Foo = new Dictionary<string, string> { { "foo", "bar" }, { "baz", "qux" } } };
		var target = BuildAsObservable(data, "Foo[foo]");
		var result = await target.Take(1);

		CornerstoneTest.AreEqual("bar", result);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldSetArrayIndex()
	{
		var data = new { Foo = new[] { "foo", "bar" } };
		var target = Build(data, "Foo[1]");

		using (target.ToObservable().Subscribe(_ => { }))
		{
			CornerstoneTest.IsTrue(target.WriteValueToSource("baz"));
		}

		CornerstoneTest.AreEqual("baz", data.Foo[1]);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldSetExistingDictionaryEntry()
	{
		var data = new
		{
			Foo = new Dictionary<string, int>
			{
				{ "foo", 1 }
			}
		};

		var target = Build(data, "Foo[foo]");
		using (target.ToObservable().Subscribe(_ => { }))
		{
			CornerstoneTest.IsTrue(target.WriteValueToSource(4));
		}

		CornerstoneTest.AreEqual(4, data.Foo["foo"]);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldSetNonIntegerIndexer()
	{
		var data = new { Foo = new NonIntegerIndexer() };
		data.Foo["foo"] = "bar";
		data.Foo["baz"] = "qux";

		var target = Build(data, "Foo[foo]");

		using (target.ToObservable().Subscribe(_ => { }))
		{
			CornerstoneTest.IsTrue(target.WriteValueToSource("bar2"));
		}

		CornerstoneTest.AreEqual("bar2", data.Foo["foo"]);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldTrackINCCAdd()
	{
		var data = new { Foo = new OldPresentationList<string> { "foo", "bar" } };
		var target = BuildAsObservable(data, "Foo[2]");
		var result = new List<object>();

		using (var sub = target.Subscribe(x => result.Add(BindingNotification.ExtractValue(x))))
		{
			data.Foo.Add("baz");
		}

		// Forces WeakEvent compact
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

		CornerstoneTest.AreEqual(new[] { PresentationProperty.UnsetValue, "baz" }, result);
		CornerstoneTest.IsNull(((INotifyCollectionChangedDebug) data.Foo).GetCollectionChangedSubscribers());

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldTrackINCCMove()
	{
		// Using ObservableCollection here because OldPresentationList does not yet have a Move
		// method, but even if it did we need to test with ObservableCollection as well
		// as OldPresentationList as it implements PropertyChanged as an explicit interface event.
		var data = new { Foo = new ObservableCollection<string> { "foo", "bar" } };
		var target = BuildAsObservable(data, "Foo[1]");
		var result = new List<object>();

		var sub = target.Subscribe(x => result.Add(x));
		data.Foo.Move(0, 1);

		CornerstoneTest.AreEqual(new[] { "bar", "foo" }, result);

		GC.KeepAlive(sub);
		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldTrackINCCRemove()
	{
		var data = new { Foo = new OldPresentationList<string> { "foo", "bar" } };
		var target = BuildAsObservable(data, "Foo[0]");
		var result = new List<object>();

		using (var sub = target.Subscribe(x => result.Add(x)))
		{
			data.Foo.RemoveAt(0);
		}

		// Forces WeakEvent compact
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

		CornerstoneTest.AreEqual(new[] { "foo", "bar" }, result);
		CornerstoneTest.IsNull(((INotifyCollectionChangedDebug) data.Foo).GetCollectionChangedSubscribers());

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldTrackINCCReplace()
	{
		var data = new { Foo = new OldPresentationList<string> { "foo", "bar" } };
		var target = BuildAsObservable(data, "Foo[1]");
		var result = new List<object>();

		using (var sub = target.Subscribe(x => result.Add(x)))
		{
			data.Foo[1] = "baz";
		}

		// Forces WeakEvent compact
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

		CornerstoneTest.AreEqual(new[] { "bar", "baz" }, result);
		CornerstoneTest.IsNull(((INotifyCollectionChangedDebug) data.Foo).GetCollectionChangedSubscribers());

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldTrackINCCReset()
	{
		var data = new { Foo = new OldPresentationList<string> { "foo", "bar" } };
		var target = BuildAsObservable(data, "Foo[1]");
		var result = new List<object>();

		var sub = target.Subscribe(x => result.Add(BindingNotification.ExtractValue(x)));
		data.Foo.Clear();

		CornerstoneTest.AreEqual(new[] { "bar", PresentationProperty.UnsetValue }, result);

		GC.KeepAlive(sub);
		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldTrackNonIntegerIndexer()
	{
		var data = new { Foo = new NonIntegerIndexer() };
		data.Foo["foo"] = "bar";
		data.Foo["baz"] = "qux";

		var target = BuildAsObservable(data, "Foo[foo]");
		var result = new List<object>();

		using (var sub = target.Subscribe(x => result.Add(x)))
		{
			data.Foo["foo"] = "bar2";
		}

		// Forces WeakEvent compact
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

		var expected = new[] { "bar", "bar2" };
		CornerstoneTest.AreEqual(expected, result);
		CornerstoneTest.AreEqual(0, data.Foo.PropertyChangedSubscriptionCount);

		GC.KeepAlive(data);
	}

	private static BindingExpression Build(object source, string path, Func<string, string, Type> typeResolver = null)
	{
		var r = new CharacterReader(path);
		var grammar = BindingExpressionGrammar.Parse(ref r).Nodes;
		var nodes = ExpressionNodeFactory.CreateFromAst(grammar, typeResolver, null, out _);
		return new BindingExpression(source, nodes, PresentationProperty.UnsetValue);
	}

	private static IObservable<object> BuildAsObservable(object source, string path, Func<string, string, Type> typeResolver = null)
	{
		return Build(source, path, typeResolver).ToObservable();
	}

	#endregion

	#region Classes

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