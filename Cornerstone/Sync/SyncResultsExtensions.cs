#region References

using System.Collections.Generic;
using System.Linq;

#endregion

namespace Cornerstone.Sync;

public static class SyncResultsExtensions
{
	#region Methods

	public static string ToDetailedString(this IList<SyncIssue> issues)
	{
		return issues.Count <= 0 ? string.Empty : string.Join("\r\n", issues.Select(x => $"{x.TypeName}: {x.IssueType} {x.Message}"));
	}

	#endregion
}