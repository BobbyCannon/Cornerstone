#region References

using System.Collections.Generic;
using System.Collections.Specialized;
using Cornerstone.Presentation.Collections;
using Cornerstone.Presentation.Controls.Utils;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CollectionChangedEventManager = Cornerstone.Presentation.Controls.Utils.CollectionChangedEventManager;

#endregion

namespace Cornerstone.Presentation.UnitTests.Controls.Utils;

[TestClass]
public class CollectionChangedEventManagerTests : ScopedTestBase
{
	#region Methods

	[PresentationTestMethod]
	public void AddListenerListensToEvents()
	{
		var source = new OldPresentationList<string>();
		var listener = new Listener();

		CollectionChangedEventManager.Instance.AddListener(source, listener);

		CornerstoneTest.Empty(listener.Received);

		source.Add("foo");

		CornerstoneTest.AreEqual(1, listener.Received.Count);
	}

	[PresentationTestMethod]
	public void ReceivesEventsFromWrappedCollection()
	{
		var source = new WrappingCollection();
		var listener = new Listener();

		CollectionChangedEventManager.Instance.AddListener(source, listener);

		CornerstoneTest.Empty(listener.Received);

		source.Add("foo");

		CornerstoneTest.AreEqual(1, listener.Received.Count);
	}

	[PresentationTestMethod]
	public void RemoveListenerStopsListeningToEvents()
	{
		var source = new OldPresentationList<string>();
		var listener = new Listener();

		CollectionChangedEventManager.Instance.AddListener(source, listener);
		CollectionChangedEventManager.Instance.RemoveListener(source, listener);

		source.Add("foo");

		CornerstoneTest.Empty(listener.Received);
	}

	#endregion

	#region Classes

	private class Listener : ICollectionChangedListener
	{
		#region Properties

		public List<NotifyCollectionChangedEventArgs> Received { get; } = new();

		#endregion

		#region Methods

		public void Changed(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
		{
			Received.Add(e);
		}

		public void PostChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
		{
		}

		public void PreChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
		{
		}

		#endregion
	}

	private class WrappingCollection : INotifyCollectionChanged
	{
		#region Fields

		private readonly OldPresentationList<string> _inner = new();

		#endregion

		#region Methods

		public void Add(string s)
		{
			_inner.Add(s);
		}

		#endregion

		#region Events

		public event NotifyCollectionChangedEventHandler CollectionChanged
		{
			add => _inner.CollectionChanged += value;
			remove => _inner.CollectionChanged -= value;
		}

		#endregion
	}

	#endregion
}