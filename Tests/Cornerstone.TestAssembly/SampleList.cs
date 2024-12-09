#region References

using System.Collections.Generic;
using System.Collections.Specialized;

#endregion

namespace Cornerstone.TestAssembly;

public class SampleList<T> : List<T>, INotifyCollectionChanged
{
	#region Methods

	protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
	{
		CollectionChanged?.Invoke(this, e);
	}

	#endregion

	#region Events

	/// <inheritdoc />
	public event NotifyCollectionChangedEventHandler CollectionChanged;

	#endregion
}