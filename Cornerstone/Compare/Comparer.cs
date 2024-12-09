#region References

using System.Collections.Generic;
using Cornerstone.Compare.Comparers;

#endregion

namespace Cornerstone.Compare;

/// <summary>
/// Service for comparing objects.
/// </summary>
public static class Comparer
{
	#region Constructors

	/// <summary>
	/// Initialize the compare service.
	/// </summary>
	static Comparer()
	{
		// Do not reorder these comparers, very important.
		Comparers =
		[
			new DateComparer(),
			new TimeComparer(),
			new NumberComparer(),
			new DictionaryComparer(),
			new ListComparer(),
			new TupleComparer(),
			new StringComparer(),
			new EnumComparer(),
			new EnumerableComparer(),
			new TypeComparer(),
			new ComparableComparer(),
			new DataTableComparer(),
			new ObjectComparer()
		];

		Settings = new ComparerSettings();
	}

	#endregion

	#region Properties

	/// <summary>
	/// The comparers used by the compare service
	/// </summary>
	public static List<BaseComparer> Comparers { get; }

	/// <summary>
	/// The default settings when starting a session.
	/// </summary>
	public static ComparerSettings Settings { get; }

	#endregion

	#region Methods

	/// <summary>
	/// Compare the expected to the actual value.
	/// </summary>
	/// <param name="expected"> The expected value. </param>
	/// <param name="actual"> The actual value to compare to expected. </param>
	/// <param name="settings"> The settings for the compare session. </param>
	/// <returns> True if the values are equal otherwise false. </returns>
	public static bool Compare(object expected, object actual, ComparerSettings? settings = null)
	{
		var session = StartSession(expected, actual, settings);
		session.Compare();
		return session.Result == CompareResult.AreEqual;
	}

	/// <summary>
	/// Starts a new sync session.
	/// </summary>
	public static CompareSession<T, T2> StartSession<T, T2>(T expected, T2 actual, ComparerSettings? settings = null)
	{
		return new CompareSession<T, T2>(expected, actual, settings ?? Settings);
	}

	#endregion
}