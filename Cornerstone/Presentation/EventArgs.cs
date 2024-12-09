#region References

using System;

#endregion

namespace Cornerstone.Presentation;

/// <inheritdoc />
public class EventArgs<T> : EventArgs
{
	#region Constructors

	/// <inheritdoc />
	public EventArgs(T data)
	{
		Data = data;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The data provided with the event.
	/// </summary>
	public T Data { get; }

	#endregion
}