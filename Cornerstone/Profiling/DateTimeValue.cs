#region References

using System;

#endregion

namespace Cornerstone.Profiling;

public struct DateTimeValue<T>
{
	#region Constructors

	public DateTimeValue(DateTime dateTime, T value)
	{
		DateTime = dateTime;
		Value = value;
	}

	#endregion

	#region Properties

	public DateTime DateTime { get; }

	public T Value { get; }

	#endregion
}