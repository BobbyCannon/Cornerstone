#region References

using System;
using System.Data;
using Cornerstone.Storage.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.IntegrationTests.Storage;

internal static class SqlProviderHarness
{
	#region Methods

	public static string CreateSqliteMemoryConnectionString()
	{
		return $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared;";
	}

	public static void DropSqlServerDatabase(string connectionString)
	{
		var databaseName = ConnectionStringParser.GetDatabaseName(connectionString);
		var master = ConnectionStringParser.GetMasterString(connectionString);
		using var connection = new SqlConnection(master);
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

	public static bool IsDatabasePresent(SqlProvider provider, string connectionString)
	{
		if (provider == SqlProvider.Sqlite)
		{
			using var connection = new SqliteConnection(connectionString);
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandText = "SELECT 1";
			return System.Convert.ToInt32(command.ExecuteScalar()) == 1;
		}

		var databaseName = ConnectionStringParser.GetDatabaseName(connectionString);
		var master = ConnectionStringParser.GetMasterString(connectionString);
		using var sqlConnection = new SqlConnection(master);
		sqlConnection.Open();
		using var sqlCommand = sqlConnection.CreateCommand();
		sqlCommand.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name = @name";
		var name = sqlCommand.CreateParameter();
		name.ParameterName = "@name";
		name.Value = databaseName;
		sqlCommand.Parameters.Add(name);
		return System.Convert.ToInt32(sqlCommand.ExecuteScalar()) == 1;
	}

	public static void RequireSqlServer(string connectionString)
	{
		if (!TryConnectSqlServer(connectionString, out var error))
		{
			Assert.Inconclusive("SQL Server is not available on localhost. " + error);
		}
	}

	public static string SqlServerConnectionString()
	{
		return "server=localhost;database=Cornerstone.Sample.Tests;integrated security=true;encrypt=false;";
	}

	public static bool TryConnectSqlServer(string connectionString, out string error)
	{
		try
		{
			var master = ConnectionStringParser.GetMasterString(connectionString);
			using var connection = new SqlConnection(master);
			connection.Open();
			error = null;
			return connection.State == ConnectionState.Open;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	#endregion
}