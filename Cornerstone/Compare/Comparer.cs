#region References

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
			new DictionaryComparer(),
			new ListComparer(),
			new StringComparer(),
			new EnumerableComparer(),
			new TypeComparer(),
			new ObjectComparer()
		];

		Settings = new ComparerSettings();
	}

	#endregion

	#region Properties

	/// <summary>
	/// The comparers used by the compare service
	/// </summary>
	public static BaseComparer[] Comparers { get; }

	/// <summary>
	/// The default settings when starting a session.
	/// </summary>
	public static ComparerSettings Settings { get; }

	#endregion

	#region Methods

	public static CompareSession<T, T2> StartSession<T, T2>(T expected, T2 actual, ComparerSettings? settings)
	{
		return new CompareSession<T, T2>(expected, actual, settings ?? Settings);
	}

	#endregion
}