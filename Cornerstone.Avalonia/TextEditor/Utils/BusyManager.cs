#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

#endregion

namespace Cornerstone.Avalonia.TextEditor.Utils;

/// <summary>
/// This class is used to prevent stack overflows by representing a 'busy' flag
/// that prevents reentrance when another call is running.
/// However, using a simple 'bool busy' is not thread-safe, so we use a
/// thread-static BusyManager.
/// </summary>
internal static class BusyManager
{
	#region Fields

	[ThreadStatic]
	private static List<object> _activeObjects;

	#endregion

	#region Methods

	public static BusyLock Enter(object obj)
	{
		var activeObjects = _activeObjects ?? (_activeObjects = new List<object>());
		if (activeObjects.Any(t => t == obj))
		{
			return BusyLock.Failed;
		}
		activeObjects.Add(obj);
		return new BusyLock(activeObjects);
	}

	#endregion

	#region Structures

	[SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
	[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible",
		Justification = "Should always be used with 'var'")]
	public struct BusyLock : IDisposable
	{
		#region Fields

		public static readonly BusyLock Failed = new(null);

		private readonly List<object> _objectList;

		#endregion

		#region Constructors

		internal BusyLock(List<object> objectList)
		{
			_objectList = objectList;
		}

		#endregion

		#region Properties

		public bool Success => _objectList != null;

		#endregion

		#region Methods

		public void Dispose()
		{
			_objectList?.RemoveAt(_objectList.Count - 1);
		}

		#endregion
	}

	#endregion
}