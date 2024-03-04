#region References

using System;
using Cornerstone.Convert;
using Cornerstone.Extensions;

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
	protected override CompareResult CompareValues(CompareSession session, object expected, object actual, string message)
	{
		switch (expected)
		{
			#if !NETSTANDARD
			case TimeOnly eValue:
			{
				var aValue = Converter.ConvertTo<TimeOnly>(actual);
				if (eValue.CompareTo(aValue) == 0)
				{
					return CompareResult.AreEqual;
				}
				break;
			}
			#endif
			case TimeSpan eValue:
			{
				var aValue = Converter.ConvertTo<TimeSpan>(actual);
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

		AddDifference(session, expected.ToString(), actual.ToString(), true);
		return CompareResult.NotEqual;
	}

	#endregion
}