#region References

using System;
using Cornerstone.Convert;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Compare.Comparers;

/// <summary>
/// Comparer for date types.
/// </summary>
public class DateComparer : BaseComparer
{
	#region Constructors

	public DateComparer() : base(SourceTypes.DateTypes)
	{
	}

	#endregion

	#region Methods

	protected override CompareResult CompareValues(CompareSession session, object expected, object actual, Func<string> message)
	{
		switch (expected)
		{
			case DateOnly eValue:
			{
				var aValue = actual.ConvertTo<DateOnly>();
				if (eValue.CompareTo(aValue) == 0)
				{
					return CompareResult.AreEqual;
				}
				break;
			}
			case DateTime eValue:
			{
				var aValue = actual.ConvertTo<DateTime>();
				if ((eValue.CompareTo(aValue) == 0)
					&& (eValue.Kind == aValue.Kind))
				{
					return CompareResult.AreEqual;
				}
				break;
			}
			case DateTimeOffset eValue:
			{
				var aValue = actual.ConvertTo<DateTimeOffset>();
				if (eValue.CompareTo(aValue) == 0)
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

		session.AddDifference(expected, actual, true);
		return CompareResult.NotEqual;
	}

	#endregion
}