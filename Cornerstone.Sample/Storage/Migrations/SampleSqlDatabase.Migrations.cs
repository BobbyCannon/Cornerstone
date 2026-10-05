#region References

using System.Collections.Generic;
using Cornerstone.Sample.Storage.Migrations;
using Cornerstone.Storage.Sql.Migrations;

#endregion

#pragma warning disable IDE0130
namespace Cornerstone.Sample.Storage;

public partial class SampleSqlDatabase
{
	#region Methods

	protected override IReadOnlyList<SqlMigration> GetMigrations()
	{
		return
		[
			new Initial()
		];
	}

	#endregion
}