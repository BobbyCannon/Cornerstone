#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using Cornerstone.Storage.Sql.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

#endregion

namespace Cornerstone.Storage.Sql;

public class SqlDatabase : IDisposable
{
	#region Fields

	private readonly DbConnection _connection;
	private string _databaseName;
	private string _masterConnectionString;
	private readonly ConcurrentDictionary<Type, object> _repositories;

	#endregion

	#region Constructors

	public SqlDatabase(string connectionString, SqlProvider provider)
	{
		_repositories = new();

		ConnectionString = connectionString;
		Provider = provider;

		if ((provider == SqlProvider.Sqlite)
			&& ConnectionString.Contains("Mode=Memory", StringComparison.OrdinalIgnoreCase))
		{
			// This connection keeps the :memory: DB alive until this database is disposed.
			_connection = new SqliteConnection(connectionString);
			_connection.Open();
		}
	}

	#endregion

	#region Properties

	public string ConnectionString { get; }

	public SqlProvider Provider { get; }

	#endregion

	#region Methods

	public DbConnection CreateConnection()
	{
		return CreateConnection(ConnectionString);
	}

	public DbConnection CreateConnection(string connectionString)
	{
		return Provider == SqlProvider.SqlServer
			? new SqlConnection(connectionString)
			: new SqliteConnection(connectionString);
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Make sure the database is created.
	/// </summary>
	public void EnsureDatabaseCreated()
	{
		if (Provider is not SqlProvider.SqlServer)
		{
			return;
		}

		_masterConnectionString ??= ConnectionStringParser.GetMasterString(ConnectionString);
		_databaseName ??= ConnectionStringParser.GetDatabaseName(ConnectionString);

		var sql = SqlGenerator.GetCreateDatabaseScript(_databaseName, Provider);
		using var connection = CreateConnection(_masterConnectionString);
		connection.Open();

		using var command = connection.CreateCommand();
		command.CommandText = sql;
		command.ExecuteNonQuery();
	}

	/// <summary>
	/// Get a database repository.
	/// </summary>
	/// <typeparam name="T"> The type of the repository. </typeparam>
	/// <returns> The repository. </returns>
	public SqlRepository<T> GetRepository<T>() where T : Entity, new()
	{
		return (SqlRepository<T>) _repositories.GetOrAdd(typeof(T), _ => new SqlRepository<T>(this));
	}

	/// <summary>
	/// Query the tables for the database.
	/// </summary>
	public IEnumerable<SqlTable> QueryTables()
	{
		var sql = SqlGenerator.GetTableQueryScript(Provider);
		using var connection = CreateConnection();
		connection.Open();

		using var command = connection.CreateCommand();
		command.CommandText = sql;
		using var reader = command.ExecuteReader();

		SqlTable table = null;

		while (reader.Read())
		{
			var tableName = reader["TableName"].ToString();

			if ((table == null) || (tableName != table.Name))
			{
				if (table != null)
				{
					yield return table;
				}
				table = new SqlTable(reader);
			}

			table.Columns.Add(SqlTableColumn.FromReader(reader));
		}

		if (table != null)
		{
			yield return table;
		}
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected virtual void Dispose(bool disposing)
	{
		_connection?.Close();
		_connection?.Dispose();
	}

	#endregion
}