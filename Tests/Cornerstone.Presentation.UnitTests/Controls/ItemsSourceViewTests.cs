#region References

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls.Items;
using Cornerstone.Presentation.Diagnostics;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls;

[TestClass]
public class ItemsSourceViewTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void CannotCreateItemsSourceViewWithCollectionThatImplementsINCCButNotList()
	{
		var source = new InvalidCollection();
		Assert.Throws<ArgumentException>(() => ItemsSourceView.GetOrCreate(source));
	}

	[PresentationTestMethod]
	public void OnlySubscribesToSourceCollectionChangedWhenCollectionChangedSubscribed()
	{
		var source = new OldPresentationList<string>();
		var target = ItemsSourceView.GetOrCreate(source);
		var debug = (INotifyCollectionChangedDebug) source;

		CornerstoneTest.IsNull(debug.GetCollectionChangedSubscribers());

		void Handler(object sender, NotifyCollectionChangedEventArgs e)
		{
		}

		target.CollectionChanged += Handler;

		var subscribers = debug.GetCollectionChangedSubscribers();
		CornerstoneTest.IsNotNull(subscribers);
		CornerstoneTest.AreEqual(1, subscribers.Length);

		target.CollectionChanged -= Handler;

		CornerstoneTest.IsNull(debug.GetCollectionChangedSubscribers());
	}

	[PresentationTestMethod]
	public void ReassigningSourceSubscribesToNewSource()
	{
		var source = new OldPresentationList<string>();
		var target = new ReassignableItemsSourceView(new string[0]);
		var debug = (INotifyCollectionChangedDebug) source;

		target.CollectionChanged += (s, e) => { };
		target.SetSource(source);

		var subscribers = debug.GetCollectionChangedSubscribers();
		CornerstoneTest.IsNotNull(subscribers);
		CornerstoneTest.AreEqual(1, subscribers.Length);
	}

	[PresentationTestMethod]
	public void ReassigningSourceUnsubscribesFromPreviousSource()
	{
		var source = new OldPresentationList<string>();
		var target = new ReassignableItemsSourceView(source);
		var debug = (INotifyCollectionChangedDebug) source;

		target.CollectionChanged += (s, e) => { };

		var subscribers = debug.GetCollectionChangedSubscribers();
		CornerstoneTest.IsNotNull(subscribers);
		CornerstoneTest.AreEqual(1, subscribers.Length);

		target.SetSource(new string[0]);

		CornerstoneTest.IsNull(debug.GetCollectionChangedSubscribers());
	}

	#endregion

	#region Classes

	private class InvalidCollection : INotifyCollectionChanged, IEnumerable<string>
	{
		#region Methods

		public IEnumerator<string> GetEnumerator()
		{
			yield break;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			yield break;
		}

		#endregion

		#region Events

		public event NotifyCollectionChangedEventHandler CollectionChanged
		{
			add { }
			remove { }
		}

		#endregion
	}

	private class ReassignableItemsSourceView : ItemsSourceView
	{
		#region Constructors

		public ReassignableItemsSourceView(IEnumerable source)
			: base(source)
		{
		}

		#endregion

		#region Methods

		public new void SetSource(IEnumerable source)
		{
			base.SetSource(source);
		}

		#endregion
	}

	#endregion
}