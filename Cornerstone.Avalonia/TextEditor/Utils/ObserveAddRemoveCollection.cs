#region References

using System;
using System.Collections.ObjectModel;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Utils;

/// <summary>
/// A collection where adding and removing items causes a callback.
/// It is valid for the onAdd callback to throw an exception - this will prevent the new item from
/// being added to the collection.
/// </summary>
internal sealed class ObserveAddRemoveCollection<T> : Collection<T>
{
	#region Fields

	private readonly Action<T> _onAdd;
	private readonly Action<T> _onRemove;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates a new ObserveAddRemoveCollection using the specified callbacks.
	/// </summary>
	public ObserveAddRemoveCollection(Action<T> onAdd, Action<T> onRemove)
	{
		_onAdd = onAdd ?? throw new ArgumentNullException(nameof(onAdd));
		_onRemove = onRemove ?? throw new ArgumentNullException(nameof(onRemove));
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override void ClearItems()
	{
		if (_onRemove != null)
		{
			foreach (var val in this)
			{
				_onRemove(val);
			}
		}
		base.ClearItems();
	}

	/// <inheritdoc />
	protected override void InsertItem(int index, T item)
	{
		_onAdd?.Invoke(item);
		base.InsertItem(index, item);
	}

	/// <inheritdoc />
	protected override void RemoveItem(int index)
	{
		_onRemove?.Invoke(this[index]);
		base.RemoveItem(index);
	}

	/// <inheritdoc />
	protected override void SetItem(int index, T item)
	{
		_onRemove?.Invoke(this[index]);
		try
		{
			_onAdd?.Invoke(item);
		}
		catch
		{
			// When adding the new item fails, just remove the old one
			// (we cannot keep the old item since we already successfully called onRemove for it)
			RemoveAt(index);
			throw;
		}
		base.SetItem(index, item);
	}

	#endregion
}