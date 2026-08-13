namespace Cornerstone.Presentation;

/// <summary>
/// Pure state machine for AppDispatcher idle/active rate selection.
/// Unit-tested without wall-clock waits.
/// </summary>
internal static class AdaptiveDispatchMode
{
	#region Methods

	/// <summary>
	/// Advances mode after one worker tick.
	/// </summary>
	/// <param name="isActive"> Current high-rate mode. </param>
	/// <param name="idleStreak"> Consecutive empty ticks while active (or idle). </param>
	/// <param name="applied"> True when <c> Update </c> applied at least one ViewModel. </param>
	/// <param name="requested"> True when the wait ended because of <c> RequestDispatch </c>. </param>
	/// <param name="idleTicksBeforeThrottle"> Empty ticks required before leaving active mode. </param>
	public static void Advance(
		ref bool isActive,
		ref int idleStreak,
		bool applied,
		bool requested,
		int idleTicksBeforeThrottle)
	{
		if (applied || requested)
		{
			isActive = true;
			idleStreak = 0;
			return;
		}

		idleStreak++;
		if (isActive && (idleStreak >= idleTicksBeforeThrottle))
		{
			isActive = false;
			idleStreak = 0;
		}
	}

	#endregion
}
