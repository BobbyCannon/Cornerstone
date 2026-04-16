#region References

using System.Diagnostics.CodeAnalysis;

#endregion

namespace Cornerstone.Collections;

/// <summary>
/// Defines the operational mode of the <see cref="SpeedyQueue{T}" />.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum QueueMode
{
	/// <summary>
	/// First-In-First-Out (standard queue behavior).
	/// Enqueue adds to the back; Dequeue removes from the front.
	/// </summary>
	FIFO,

	/// <summary>
	/// Last-In-First-Out (stack behavior).
	/// Enqueue adds to the "top"; Dequeue removes from the "top".
	/// </summary>
	LIFO
}