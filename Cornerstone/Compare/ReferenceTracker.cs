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

	/// <inheritdoc />
	public void AddReference(object value)
	{
		var type = value?.GetType();
		if (type is not { IsClass: true }
			|| (type == typeof(string)))
		{
			return;
		}


		_references.Add(value);
	}

	/// <inheritdoc />
	public void RemoveReference(object value)
	{
		_references.Remove(value);
	}

	/// <summary>
	/// Clear all the current references
	/// </summary>
	public void Clear()
	{
		_references.Clear();
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
	/// Check to see if a value is a current reference.
	/// </summary>
	/// <param name="value"> The value to be checked. </param>
	/// <returns> True if the value is a reference otherwise false. </returns>
	public bool AlreadyProcessed(object value)
	{
		return _references.Any(x =>
		{
			var response = ReferenceEquals(x, value);
			return response;
		});
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
	void AddReference(object value);

	/// <summary>
	/// Remove a reference to track.
	/// </summary>
	/// <param name="value"> The value to track. </param>
	void RemoveReference(object value);

	/// <summary>
	/// Check to see if a value is a current reference.
	/// </summary>
	/// <param name="value"> The value to be checked. </param>
	/// <returns> True if the value is a reference otherwise false. </returns>
	bool AlreadyProcessed(object value);

	/// <summary>
	/// Clear all the current references
	/// </summary>
	internal void Clear();

	#endregion
}