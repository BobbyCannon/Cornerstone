#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Cornerstone.Reflection;
using Cornerstone.Storage.Sql.Data;
using Cornerstone.Storage.Sql.Migrations;
using Cornerstone.Text;
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

	public static List<TableDiff> Compare(
		IEnumerable<SqlTable> existing,
		IEnumerable<Type> targetTypes,
		SqlProvider provider)
	{
		var response = new List<TableDiff>();
		var identifierBrackets = SqlGenerator.GetIdentifierBrackets(provider);
		var existingDict = existing.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);

		foreach (var targetType in targetTypes)
		{
			var targetSourceType = SourceReflector.GetRequiredSourceType(targetType);
			var tableName = SqlGenerator.GetTableName(targetSourceType);
			var existingTable = existingDict.GetValueOrDefault(tableName);
			var targetColumnNames = targetSourceType
				.GetProperties()
				.Select(p => p.Attributes
					.Where(a => a.Name == nameof(SqlTableColumnAttribute))
					.Select(a => a.NamedArguments[nameof(SqlTableColumnAttribute.Name)])
					.FirstOrDefault()
					?? p.Name
				)
				.Where(x => x != null)
				.ToHashSet();

			var columns = new List<ColumnChange>();
			if (existingTable == null)
			{
				response.Add(new TableDiff(tableName, columns, identifierBrackets));
				continue;
			}

			foreach (var col in existingTable.Columns)
			{
				if (!targetColumnNames.Contains(col.Name))
				{
					columns.Add(new ColumnChange(col.Name, ColumnAction.Drop));
				}
			}

			if (columns.Count > 0)
			{
				response.Add(new TableDiff(tableName, columns, identifierBrackets));
			}
		}

		return response;
	}

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

	public async Task ExecutePendingMigrationsAsync()
	{
		EnsureDatabaseCreated();

		var existingTables = QueryTables().ToList();
		var pendingDiff = Compare(existingTables, _repositories.Keys, Provider);

		foreach (var diff in pendingDiff)
		{
			var script = GenerateAlterScript(diff, Provider);
			await ExecuteMigrationScriptAsync(script);
		}
	}

	public static string GenerateAlterScript(TableDiff diff, SqlProvider provider)
	{
		using var rented = StringBuilderPool.Rent();
		var builder = rented.Value;
		var (open, close) = diff.IdentifierBrackets;

		builder.Append($"ALTER TABLE {open}{diff.TableName}{close} ");

		var operations = new List<string>();

		foreach (var col in diff.Columns)
		{
			operations.Add(col.Action switch
			{
				ColumnAction.Drop => $"DROP COLUMN {open}{col.Name}{close}",
				ColumnAction.Add => $"ADD COLUMN {open}{col.Name}{close} TEXT",
				_ => string.Empty
			});
		}

		builder.AppendLine(string.Join(";", operations));
		return builder.ToString();
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

	private async Task ExecuteMigrationScriptAsync(string script)
	{
		await using var connection = CreateConnection();
		await connection.OpenAsync();

		await using var command = connection.CreateCommand();
		command.CommandText = script;
		await command.ExecuteNonQueryAsync();
	}

	#endregion
}