#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Storage.Sql.Migrations;

/// <summary>
/// Result of comparing two schema snapshots (previous vs current).
/// </summary>
public class SchemaDiff
{
	#region Constructors

	public SchemaDiff()
	{
		Tables = [];
	}

	#endregion

	#region Properties

	public bool HasChanges => Tables.Count > 0;

	public List<TableDiff> Tables { get; }

	#endregion
}