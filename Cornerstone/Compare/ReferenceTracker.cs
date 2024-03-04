#region References

using System.Collections.Generic;
using System.Linq;

#endregion

namespace Cornerstone.Compare;

/// <summary>
/// Track a set of references.
/// </summary>
public class ReferenceTracker : IReferenceTracker
{
	#region Fields

	private readonly IList<object> _references;

	#endregion

	#region Constructors

	/// <summary>
	/// Initialize the reference tracker.
	/// </summary>
	public ReferenceTracker()
	{
		_references = new List<object>();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Clear all the current references
	/// </summary>
	public void Clear()
	{
		_references.Clear();
	}

	/// <summary>
	/// Add reference to the session.
	/// </summary>
	/// <param name="value"> The value to track as a reference. </param>
	internal void Add(object value)
	{
		if (value != null)
		{
			_references.Add(value);
		}
	}

	internal bool CheckReference(object expected, object expectedValue, object actual, object actualValue)
	{
		// Values cannot be previous process or references to themselves.
		var actualIsParentOrProcessed = ReferenceEquals(actualValue, actual)
			|| ((IReferenceTracker) this).AlreadyProcessed(actualValue);

		var expectedIsParentOrProcessed = ReferenceEquals(expectedValue, expected)
			|| ((IReferenceTracker) this).AlreadyProcessed(expectedValue);

		return actualIsParentOrProcessed
			&& expectedIsParentOrProcessed;
	}

	/// <summary>
	/// Remove the reference from the session.
	/// </summary>
	/// <param name="value"> The reference to remove. </param>
	internal void Remove(object value)
	{
		_references.Remove(value);
	}

	/// <inheritdoc />
	void IReferenceTracker.AddReference(object value)
	{
		_references.Add(value);
	}

	/// <summary>
	/// Check to see if a value is a current reference.
	/// </summary>
	/// <param name="value"> The value to be checked. </param>
	/// <returns> True if the value is a reference otherwise false. </returns>
	bool IReferenceTracker.AlreadyProcessed(object value)
	{
		return _references.Any(x => ReferenceEquals(x, value));
	}

	#endregion
}

internal interface IReferenceTracker
{
	#region Methods

	/// <summary>
	/// Add a reference to track.
	/// </summary>
	/// <param name="value"> The value to track. </param>
	internal void AddReference(object value);

	/// <summary>
	/// Check to see if a value is a current reference.
	/// </summary>
	/// <param name="value"> The value to be checked. </param>
	/// <returns> True if the value is a reference otherwise false. </returns>
	internal bool AlreadyProcessed(object value);

	/// <summary>
	/// Clear all the current references
	/// </summary>
	internal void Clear();

	#endregion
}