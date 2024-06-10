#region References

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

#endregion

namespace Cornerstone.Avalonia.AvaloniaEdit.Utils;

internal interface IFreezable
{
	#region Properties

	/// <summary>
	/// Gets if this instance is frozen. Frozen instances are immutable and thus thread-safe.
	/// </summary>
	bool IsFrozen { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Freezes this instance.
	/// </summary>
	void Freeze();

	#endregion
}

internal static class FreezableHelper
{
	#region Methods

	public static void Freeze(object item)
	{
		var f = item as IFreezable;
		f?.Freeze();
	}

	public static T FreezeAndReturn<T>(T item) where T : IFreezable
	{
		item.Freeze();
		return item;
	}

	public static IList<T> FreezeList<T>(IList<T> list)
	{
		if ((list == null) || (list.Count == 0))
		{
			return [];
		}
		if (list.IsReadOnly)
		{
			// If the list is already read-only, return it directly.
			// This is important, otherwise we might undo the effects of interning.
			return list;
		}
		return new ReadOnlyCollection<T>(list.ToArray());
	}

	public static IList<T> FreezeListAndElements<T>(IList<T> list)
	{
		if (list != null)
		{
			foreach (var item in list)
			{
				Freeze(item);
			}
		}
		return FreezeList(list);
	}

	/// <summary>
	/// If the item is not frozen, this method creates and returns a frozen clone.
	/// If the item is already frozen, it is returned without creating a clone.
	/// </summary>
	public static T GetFrozenClone<T>(T item) where T : IFreezable, ICloneable
	{
		if (!item.IsFrozen)
		{
			item = (T) item.Clone();
			item.Freeze();
		}
		return item;
	}

	public static void ThrowIfFrozen(IFreezable freezable)
	{
		if (freezable.IsFrozen)
		{
			throw new InvalidOperationException("Cannot mutate frozen " + freezable.GetType().Name);
		}
	}

	#endregion
}

internal abstract class AbstractFreezable : IFreezable
{
	#region Properties

	/// <summary>
	/// Gets if this instance is frozen. Frozen instances are immutable and thus thread-safe.
	/// </summary>
	public bool IsFrozen { get; private set; }

	#endregion

	#region Methods

	/// <summary>
	/// Freezes this instance.
	/// </summary>
	public void Freeze()
	{
		if (!IsFrozen)
		{
			FreezeInternal();
			IsFrozen = true;
		}
	}

	protected virtual void FreezeInternal()
	{
	}

	#endregion
}