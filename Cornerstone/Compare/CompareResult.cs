namespace Cornerstone.Compare;

/// <summary>
/// Represents the results of a <see cref="CompareSession{T,T2}" />.
/// </summary>
public enum CompareResult
{
	/// <summary>
	/// The comparison was inconclusive and could not determine if the actual value was equal to the expected value.
	/// </summary>
	Inconclusive = 0,

	/// <summary>
	/// The actual is equal to the expected.
	/// </summary>
	AreEqual = 1,

	/// <summary>
	/// The actual is not equal to the expected.
	/// </summary>
	NotEqual = 2
}