#region References

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Compare;

[SourceReflection]
public class ReferenceTracker : IReferenceTracker
{
	#region Fields

	private static readonly ReferenceTrackerObjectPool<HashSet<object>> _hashSetPool;
	private HashSet<object> _visitedObjects;

	#endregion

	#region Constructors

	public ReferenceTracker()
	{
		_visitedObjects = _hashSetPool.Get();
	}

	static ReferenceTracker()
	{
		_hashSetPool = new(
			() => new HashSet<object>(ReferenceEqualityComparer.Instance),
			x => x.Clear()
		);
	}

	#endregion

	#region Methods

	/// <summary>
	/// Check to see if a value is a current reference.
	/// </summary>
	/// <param name="value"> The value to be checked. </param>
	/// <returns> True if the value is a reference otherwise false. </returns>
	public bool AlreadyProcessed(object value)
	{
		return _visitedObjects?.Contains(value) == true;
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Removed a tracked reference
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool RemoveReference(object obj)
	{
		if ((obj == null) || (_visitedObjects == null))
		{
			return false;
		}

		return _visitedObjects.Remove(obj);
	}

	/// <summary>
	/// Tracks an object and returns false if it is already being tracked.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TrackReference(object obj)
	{
		if (obj == null)
		{
			return true;
		}

		if (obj is string || obj.GetType().IsValueType)
		{
			return true;
		}

		if (_visitedObjects == null)
		{
			return true;
		}

		return _visitedObjects.Add(obj);
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected virtual void Dispose(bool disposing)
	{
		if (_visitedObjects != null)
		{
			_hashSetPool.Return(_visitedObjects);
			_visitedObjects = null;
		}
	}

	internal bool CheckReference(object expected, object expectedValue, object actual, object actualValue)
	{
		// Values cannot be previous process or references to themselves.
		var actualIsParentOrProcessed = ReferenceEquals(actualValue, actual)
			|| AlreadyProcessed(actualValue);

		var expectedIsParentOrProcessed = ReferenceEquals(expectedValue, expected)
			|| AlreadyProcessed(expectedValue);

		return actualIsParentOrProcessed
			&& expectedIsParentOrProcessed;
	}

	#endregion

	#region Classes

	// Custom comparer for reference equality
	private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
	{
		#region Fields

		public static readonly ReferenceEqualityComparer Instance = new();

		#endregion

		#region Methods

		public int GetHashCode(object obj)
		{
			return RuntimeHelpers.GetHashCode(obj);
		}

		bool IEqualityComparer<object>.Equals(object x, object y)
		{
			return ReferenceEquals(x, y);
		}

		#endregion
	}

	#endregion
}

public interface IReferenceTracker : IDisposable
{
	#region Methods

	bool AlreadyProcessed(object value);

	bool RemoveReference(object obj);

	bool TrackReference(object obj);

	#endregion
}