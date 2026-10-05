#region References

using System;
using System.Collections.Generic;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Storage.Sql.Migrations;

public class SqlMigrationScaffolderOptions
{
	#region Constructors

	public SqlMigrationScaffolderOptions()
	{
		AppliedMigrationIds = [];
		EntityTypes = [];
		Force = false;
	}

	#endregion

	#region Properties

	public IReadOnlyList<string> AppliedMigrationIds { get; set; }

	public Type DatabaseType { get; set; }

	public IDateTimeProvider DateTimeProvider { get; set; }

	public IReadOnlyList<Type> EntityTypes { get; set; }

	public bool Force { get; set; }

	public string MigrationsNamespace { get; set; }

	public string Name { get; set; }

	public string OutputDirectory { get; set; }

	public SchemaSnapshot PreviousSnapshot { get; set; }

	#endregion
}