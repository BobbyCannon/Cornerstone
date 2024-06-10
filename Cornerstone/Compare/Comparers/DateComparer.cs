#region References

using System;
using Cornerstone.Convert;
using Cornerstone.Extensions;
using Cornerstone.Protocols.Osc;

#endregion

namespace Cornerstone.Compare.Comparers;

/// <summary>
/// Comparer for date types.
/// </summary>
public class DateComparer : BaseComparer
{
	#region Constructors

	/// <inheritdoc />
	public DateComparer() : base(Activator.DateTypes)
	{
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	protected override CompareResult CompareValues(CompareSession session, object expected, object actual, string message)
	{
		switch (expected)
		{
			#if !NETSTANDARD
			case DateOnly eValue:
			{
				var aValue = actual.ConvertTo<DateOnly>();
				if (eValue.CompareTo(aValue) == 0)
				{
					return CompareResult.AreEqual;
				}
				break;
			}
			#endif
			case DateTime eValue:
			{
				var aValue = actual.ConvertTo<DateTime>();
				var eUtc = eValue.ToUtcDateTime();
				var aUtc = aValue.ToUtcDateTime();
				if (eUtc == aUtc)
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
			case IsoDateTime eValue:
			{
				var aValue = actual.ConvertTo<IsoDateTime>();
				if (eValue.CompareTo(aValue) == 0)
				{
					return CompareResult.AreEqual;
				}
				break;
			}
			case OscTimeTag eValue:
			{
				var aValue = actual.ConvertTo<OscTimeTag>();
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

		AddDifference(session, expected.ToString(), actual.ToString(), true);
		return CompareResult.NotEqual;
	}

	#endregion
}