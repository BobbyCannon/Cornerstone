#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.UnitTests.Storage.Sql;

/// <summary>
/// Reused localhost SQL Server databases for scenario matrices. Reset between pairs; drop at assembly cleanup.
/// </summary>
public sealed class SqlServerTestPool
{
	#region Constants

	private const string ClearDatabaseQuery =
		"""
		EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'
		EXEC sp_MSForEachTable 'ALTER TABLE ? DISABLE TRIGGER ALL'
		EXEC sp_MSForEachTable 'SET QUOTED_IDENTIFIER ON; IF ''?'' NOT LIKE ''%MigrationHistory%'' AND ''?'' NOT LIKE ''%MigrationsHistory%'' DELETE FROM ?'
		EXEC sp_MSforeachtable 'ALTER TABLE ? ENABLE TRIGGER ALL'
		EXEC sp_MSForEachTable 'ALTER TABLE ? CHECK CONSTRAINT ALL'
		EXEC sp_MSForEachTable 'IF OBJECTPROPERTY(object_id(''?''), ''TableHasIdentity'') = 1 DBCC CHECKIDENT (''?'', RESEED, 0)'
		""";

	private const string MasterConnectionString = "server=localhost;database=master;integrated security=true;encrypt=false;";

	#endregion

	#region Fields

	private static readonly List<string> EfPoolNames;
	private static readonly List<string> MapperPoolNames;
	private static bool? _available;

	private readonly List<string> _checkedOutNames;
	private int _efCheckout;
	private int _mapperCheckout;

	#endregion

	#region Constructors

	static SqlServerTestPool()
	{
		EfPoolNames = [];
		MapperPoolNames = [];
	}

	public SqlServerTestPool()
	{
		_checkedOutNames = [];
	}

	#endregion

	#region Methods

	public string Checkout(bool entityFramework)
	{
		var pool = entityFramework ? EfPoolNames : MapperPoolNames;
		var index = entityFramework ? _efCheckout++ : _mapperCheckout++;
		while (pool.Count <= index)
		{
			var prefix = entityFramework ? "EfSql" : "Sql";
			pool.Add($"Cornerstone.UnitTests.{prefix}.{pool.Count}");
		}

		var name = pool[index];
		_checkedOutNames.Add(name);
		return UserConnectionString(name);
	}

	public static void DropAll()
	{
		var names = MapperPoolNames.Concat(EfPoolNames).ToList();
		if (names.Count == 0)
		{
			return;
		}

		SqlConnection.ClearAllPools();
		foreach (var name in names)
		{
			DropDatabase(name);
		}

		MapperPoolNames.Clear();
		EfPoolNames.Clear();
	}

	public void RequireAvailable()
	{
		if (_available == true)
		{
			return;
		}

		if (_available == false)
		{
			Assert.Inconclusive("SQL Server is not available on localhost.");
		}

		try
		{
			using var connection = new SqlConnection(MasterConnectionString);
			connection.Open();
			_available = true;
		}
		catch (Exception ex)
		{
			_available = false;
			Assert.Inconclusive("SQL Server is not available on localhost. " + ex.Message);
		}
	}

	public void ResetCheckedOut()
	{
		if (_checkedOutNames.Count == 0)
		{
			return;
		}

		foreach (var name in _checkedOutNames.Distinct())
		{
			try
			{
				ClearDatabase(name);
			}
			catch (SqlException)
			{
				// Database may not exist yet if Migrate never ran.
			}
		}

		_checkedOutNames.Clear();
		_efCheckout = 0;
		_mapperCheckout = 0;
	}

	private static void ClearDatabase(string databaseName)
	{
		using var connection = new SqlConnection(UserConnectionString(databaseName));
		connection.Open();
		using var command = connection.CreateCommand();
		command.CommandText = ClearDatabaseQuery;
		command.CommandTimeout = 30;
		command.ExecuteNonQuery();
	}

	private static void DropDatabase(string databaseName)
	{
		using var connection = new SqlConnection(MasterConnectionString);
		connection.Open();
		using var command = connection.CreateCommand();
		command.CommandText =
			$"""
			IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'{databaseName}')
			BEGIN
				ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
				DROP DATABASE [{databaseName}];
			END
			""";
		command.ExecuteNonQuery();
	}

	private static string UserConnectionString(string databaseName)
	{
		return $"server=localhost;database={databaseName};integrated security=true;encrypt=false;";
	}

	#endregion
}

[TestClass]
public class SqlServerTestPoolCleanup
{
	#region Methods

	[AssemblyCleanup]
	public static void DropPooledDatabases()
	{
		SqlServerTestPool.DropAll();
	}

	#endregion
}
