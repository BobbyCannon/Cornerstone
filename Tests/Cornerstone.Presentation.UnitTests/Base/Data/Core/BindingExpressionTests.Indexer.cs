#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Data;
using Cornerstone.Presentation.Data.Core;
using Cornerstone.Presentation.Threading;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Data.Core;

public partial class BindingExpressionTests
{
	#region Methods

	[PresentationTestMethod]
	public void ArrayOutOfBoundsShouldReturnUnsetValue()
	{
		var data = new { Foo = new[] { "foo", "bar" } };
		var target = CreateTargetWithSource(data, o => o.Foo[2]);

		CornerstoneTest.IsFalse(target.IsSet(TargetClass.StringProperty));

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void IndexerOnlyBindingWorks()
	{
		var data = new[] { 1, 2, 3 };
		var target = CreateTargetWithSource(data, o => o[1]);

		CornerstoneTest.AreEqual(data[1], target.Int);
	}

	[PresentationTestMethod]
	public void ListOutOfBoundsShouldReturnUnsetValue()
	{
		var data = new { Foo = new List<string> { "foo", "bar" } };
		var target = CreateTargetWithSource(data, o => o.Foo[2]);

		CornerstoneTest.IsFalse(target.IsSet(TargetClass.StringProperty));

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

		var target = CreateTargetWithSource(
			data,
			o => o.Foo["bar"],
			mode: BindingMode.TwoWay);

		target.Int = 4;

		CornerstoneTest.AreEqual(4, data.Foo["bar"]);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldGetArrayValue()
	{
		var data = new { Foo = new[] { "foo", "bar" } };
		var target = CreateTargetWithSource(data, o => o.Foo[1]);

		CornerstoneTest.AreEqual("bar", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldGetListValue()
	{
		var data = new { Foo = new List<string> { "foo", "bar" } };
		var target = CreateTargetWithSource(data, o => o.Foo[1]);

		CornerstoneTest.AreEqual("bar", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldGetMultiDimensionalArrayValue()
	{
		var data = new { Foo = new[,] { { "foo", "bar" }, { "baz", "qux" } } };
		var target = CreateTargetWithSource(data, o => o.Foo[1, 1]);

		CornerstoneTest.AreEqual("qux", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldGetValueForNonStringIndexer()
	{
		var data = new { Foo = new Dictionary<double, string> { { 1.0, "bar" }, { 2.0, "qux" } } };
		var target = CreateTargetWithSource(data, o => o.Foo[1.0]);

		CornerstoneTest.AreEqual("bar", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldGetValueForStringIndexer()
	{
		var data = new { Foo = new Dictionary<string, string> { { "foo", "bar" }, { "baz", "qux" } } };
		var target = CreateTargetWithSource(data, o => o.Foo["foo"]);

		CornerstoneTest.AreEqual("bar", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldSetArrayIndex()
	{
		var data = new { Foo = new[] { "foo", "bar" } };
		var target = CreateTargetWithSource(
			data,
			o => o.Foo[1],
			mode: BindingMode.TwoWay);

		target.String = "baz";

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

		var target = CreateTargetWithSource(
			data,
			o => o.Foo["foo"],
			mode: BindingMode.TwoWay);

		target.Int = 4;

		CornerstoneTest.AreEqual(4, data.Foo["foo"]);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldSetNonIntegerIndexer()
	{
		var data = new { Foo = new NonIntegerIndexer() };
		data.Foo["foo"] = "bar";
		data.Foo["baz"] = "qux";

		var target = CreateTargetWithSource(
			data,
			o => o.Foo["foo"],
			mode: BindingMode.TwoWay);

		target.String = "bar2";

		CornerstoneTest.AreEqual("bar2", data.Foo["foo"]);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldTrackINCCAdd()
	{
		var data = new { Foo = new OldPresentationList<string> { "foo", "bar" } };
		var target = CreateTargetWithSource(data, o => o.Foo[2]);

		CornerstoneTest.IsFalse(target.IsSet(TargetClass.StringProperty));

		data.Foo.Add("baz");

		CornerstoneTest.AreEqual("baz", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldTrackINCCMove()
	{
		// Using ObservableCollection here because OldPresentationList does not yet have a Move
		// method, but even if it did we need to test with ObservableCollection as well
		// as OldPresentationList as it implements PropertyChanged as an explicit interface event.
		var data = new { Foo = new ObservableCollection<string> { "foo", "bar" } };
		var target = CreateTargetWithSource(data, o => o.Foo[1]);

		CornerstoneTest.AreEqual("bar", target.String);

		data.Foo.Move(0, 1);

		CornerstoneTest.AreEqual("foo", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldTrackINCCRemove()
	{
		var data = new { Foo = new OldPresentationList<string> { "foo", "bar" } };
		var target = CreateTargetWithSource(data, o => o.Foo[0]);

		CornerstoneTest.AreEqual("foo", target.String);

		data.Foo.RemoveAt(0);

		CornerstoneTest.AreEqual("bar", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldTrackINCCReplace()
	{
		var data = new { Foo = new OldPresentationList<string> { "foo", "bar" } };
		var target = CreateTargetWithSource(data, o => o.Foo[1]);

		CornerstoneTest.AreEqual("bar", target.String);

		data.Foo[1] = "baz";

		CornerstoneTest.AreEqual("baz", target.String);

		GC.KeepAlive(data);
	}

	[PresentationTestMethod]
	public void ShouldTrackINCCReset()
	{
		var data = new { Foo = new OldPresentationList<string> { "foo", "bar" } };
		var target = CreateTargetWithSource(data, o => o.Foo[1]);
		var result = new List<object>();

		CornerstoneTest.AreEqual("bar", target.String);

		data.Foo.Clear();

		CornerstoneTest.AreEqual(null, target.String);
	}

	[PresentationTestMethod]
	public void ShouldTrackNonIntegerIndexer()
	{
		var data = new { Foo = new NonIntegerIndexer() };
		data.Foo["foo"] = "bar";
		data.Foo["baz"] = "qux";

		var target = CreateTargetWithSource(data, o => o.Foo["foo"]);

		CornerstoneTest.AreEqual("bar", target.String);

		data.Foo["foo"] = "bar2";

		// Forces WeakEvent compact
		Dispatcher.UIThread.RunJobs(null, CancellationToken.None);

		CornerstoneTest.AreEqual("bar2", target.String);

		GC.KeepAlive(data);
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