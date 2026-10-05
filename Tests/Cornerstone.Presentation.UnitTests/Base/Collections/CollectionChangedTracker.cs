#region References

using System;
using System.Collections.Specialized;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Collections;

internal class CollectionChangedTracker
{
	#region Constructors

	public CollectionChangedTracker(INotifyCollectionChanged collection)
	{
		collection.CollectionChanged += CollectionChanged;
	}

	#endregion

	#region Properties

	public NotifyCollectionChangedEventArgs Args { get; private set; }

	#endregion

	#region Methods

	public void Reset()
	{
		Args = null;
	}

	private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		if (Args != null)
		{
			throw new Exception("CollectionChanged called more than once.");
		}

		Args = e;
	}

	#endregion
}