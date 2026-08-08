#region References

using System.Collections.Generic;

#endregion

namespace Cornerstone.Storage.Sql.Migrations;

public record TableDiff(string TableName, List<ColumnChange> Columns, (string Open, string Close) IdentifierBrackets);