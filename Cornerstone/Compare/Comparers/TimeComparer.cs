#region References

using System;
using Cornerstone.Convert;

#endregion

namespace Cornerstone.Compare.Comparers;

/// <summary>
/// Comparer for time types.
/// </summary>
public class TimeComparer : BaseComparer
{
	#region Constructors

	/// <inheritdoc />
	public TimeComparer() : base(Activator.TimeTypes)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override CompareResult CompareValues(CompareSession session, object expected, object actual, Func<string> message)
	{
		switch (expected)
		{
			#if !NETSTANDARD
			case TimeOnly eValue:
			{
				var aValue = actual.ConvertTo<TimeOnly>();
				if (eValue.CompareTo(aValue) == 0)
				{
					return CompareResult.AreEqual;
				}
				break;
			}
			#endif
			case TimeSpan eValue:
			{
				var aValue = actual.ConvertTo<TimeSpan>();
				if (eValue == aValue)
				{
					return CompareResult.AreEqual;
				}
				break;
			}
			default:
			{
				throw new NotImplementedException();
			}
		}

		session.AddDifference(expected, actual, true, message);
		return CompareResult.NotEqual;
	}

	#endregion
}