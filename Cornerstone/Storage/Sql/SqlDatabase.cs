#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Cornerstone.Reflection;
using Cornerstone.Storage.Sql.Data;
using Cornerstone.Storage.Sql.Migrations;
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
	private readonly IReadOnlyList<SqlMigration> _migrations;
	private readonly ConcurrentDictionary<Type, object> _repositories;
	private bool? _schemaMigrated;
	private DbConnection _workConnection;
	private bool _workOwnsConnection;
	private DbTransaction _workTransaction;

	#endregion

	#region Constructors

	public SqlDatabase(string connectionString, SqlProvider provider)
		: this(connectionString, provider, null)
	{
	}

	public SqlDatabase(string connectionString, SqlProvider provider, IReadOnlyList<SqlMigration> migrations)
	{
		_migrations = migrations ?? [];
		_repositories = new();
		_schemaMigrated = null;
		_workConnection = null;
		_workOwnsConnection = false;
		_workTransaction = null;

		ConnectionString = connectionString;
		Provider = provider;

		if (provider == SqlProvider.Sqlite)
		{
			// One connection for the lifetime of this database. Memory databases
			// disappear when the last connection closes; file databases pay
			// open/close per command if we do not keep this. Queries and
			// transactions reuse it via WithConnection / ExecuteInTransaction.
			_connection = new SqliteConnection(connectionString);
			_connection.Open();
		}
	}

	#endregion

	#region Properties

	public string ConnectionString { get; }

	public bool IsDisposed { get; protected set; }

	public SqlProvider Provider { get; }

	internal int CommandCount { get; private set; }

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
	/// Drops pending inserts and deletes without writing.
	/// </summary>
	public virtual int DiscardChanges()
	{
		var count = 0;
		foreach (var repository in _repositories.Values.OfType<ISqlPendingRepository>())
		{
			repository.DiscardPending();
			count++;
		}

		return count;
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

	public Task ExecutePendingMigrationsAsync()
	{
		Migrate();
		return Task.CompletedTask;
	}

	/// <summary>
	/// Migration ids already recorded in MigrationHistory.
	/// </summary>
	public IReadOnlyList<string> GetAppliedMigrationIds()
	{
		EnsureHistoryTable();

		var ids = new List<string>();
		var (open, close) = SqlGenerator.GetIdentifierBrackets(Provider);
		WithConnection((connection, transaction) =>
		{
			using var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = $"SELECT {open}MigrationId{close} FROM {open}{SqlMigration.HistoryTableName}{close} ORDER BY {open}MigrationId{close}";
			using var reader = command.ExecuteReader();
			while (reader.Read())
			{
				ids.Add(reader.GetString(0));
			}
		});

		return ids;
	}

	/// <summary>
	/// Registered migrations not yet in history, in apply order.
	/// </summary>
	public IReadOnlyList<SqlMigration> GetPendingMigrations()
	{
		var applied = new HashSet<string>(GetAppliedMigrationIds(), StringComparer.OrdinalIgnoreCase);
		return GetMigrations()
			.OrderBy(x => x.Id, StringComparer.Ordinal)
			.Where(x => !applied.Contains(x.Id))
			.ToList();
	}

	/// <summary>
	/// All migrations registered on this database, in apply order.
	/// </summary>
	public IReadOnlyList<SqlMigration> GetRegisteredMigrations()
	{
		return GetMigrations()
			.OrderBy(x => x.Id, StringComparer.Ordinal)
			.ToList();
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
	/// True when every registered migration is in MigrationHistory. False when there are no
	/// registered migrations (EnsureTableCreated hosts) or pending Ups remain.
	/// </summary>
	public virtual bool IsDatabaseMigrated()
	{
		if (_schemaMigrated == true)
		{
			return true;
		}

		var registered = GetRegisteredMigrations();
		if (registered.Count == 0)
		{
			return false;
		}

		_schemaMigrated = GetPendingMigrations().Count == 0;
		return _schemaMigrated.Value;
	}

	/// <summary>
	/// Applies pending <see cref="SqlMigration" /> Up scripts and records them in MigrationHistory.
	/// CreateTable creates a missing table or adds missing columns on an existing one (no drops).
	/// </summary>
	public virtual void Migrate()
	{
		EnsureDatabaseCreated();
		EnsureHistoryTable();

		var productVersion = typeof(SqlDatabase).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";
		foreach (var migration in GetPendingMigrations())
		{
			var builder = new MigrationBuilder(Provider);
			migration.Up(builder);
			ExecuteInTransaction(() =>
			{
				ApplyBuilder(builder);
				InsertHistory(migration.Id, productVersion);
			});
		}

		_schemaMigrated = true;
	}

	/// <summary>
	/// Query the tables for the database.
	/// </summary>
	public IEnumerable<SqlTable> QueryTables()
	{
		var tables = LoadTables();
		LoadIndexes(tables);
		LoadForeignKeys(tables);
		return tables;
	}

	/// <summary>
	/// Writes pending Add/Delete work in one transaction. Identity values are
	/// assigned from RETURNING / OUTPUT. Does not implement IDatabase.
	/// </summary>
	public virtual int SaveChanges()
	{
		if (IsDisposed)
		{
			throw new InvalidOperationException("The database has been disposed.");
		}

		return ExecuteInTransaction(FlushPending);
	}

	/// <summary>
	/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	/// </summary>
	/// <param name="disposing"> True if disposing and false if otherwise. </param>
	protected virtual void Dispose(bool disposing)
	{
		if (IsDisposed)
		{
			return;
		}

		if (disposing)
		{
			DiscardChanges();
			IsDisposed = true;
			_connection?.Close();
			_connection?.Dispose();
		}
	}

	/// <summary>
	/// Explicit migration list for this database. Override in hosts; tests may pass the list to the constructor.
	/// </summary>
	protected virtual IReadOnlyList<SqlMigration> GetMigrations()
	{
		return _migrations;
	}

	internal void ExecuteInTransaction(Action action)
	{
		ExecuteInTransaction(() =>
		{
			action();
			return 0;
		});
	}

	internal T ExecuteInTransaction<T>(Func<T> action)
	{
		if (_workTransaction != null)
		{
			return action();
		}

		if (_connection != null)
		{
			_workConnection = _connection;
			_workOwnsConnection = false;
		}
		else
		{
			_workConnection = CreateConnection();
			_workConnection.Open();
			_workOwnsConnection = true;
		}

		_workTransaction = _workConnection.BeginTransaction();
		try
		{
			var result = action();
			_workTransaction.Commit();
			return result;
		}
		catch
		{
			_workTransaction.Rollback();
			throw;
		}
		finally
		{
			_workTransaction.Dispose();
			_workTransaction = null;
			if (_workOwnsConnection)
			{
				_workConnection.Dispose();
			}

			_workConnection = null;
			_workOwnsConnection = false;
		}
	}

	internal int ExecuteNonQuery(string sql, Action<DbCommand> bind)
	{
		var written = 0;
		WithConnection((connection, transaction) =>
		{
			using var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = sql;
			bind?.Invoke(command);
			written = command.ExecuteNonQuery();
		});
		return written;
	}

	internal T ExecuteOnCommand<T>(string sql, Action<DbCommand> bind, Func<DbCommand, T> use)
	{
		T result = default;
		WithConnection((connection, transaction) =>
		{
			using var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = sql;
			bind?.Invoke(command);
			result = use(command);
		});
		return result;
	}

	internal object ExecuteScalar(string sql, Action<DbCommand> bind)
	{
		return ExecuteOnCommand(sql, bind, command => command.ExecuteScalar());
	}

	internal int FlushPending()
	{
		var count = 0;
		foreach (var repository in _repositories.Values.OfType<ISqlPendingRepository>())
		{
			count += repository.SavePending();
		}

		return count;
	}

	private void AddMissingColumns(SqlTable expected, SqlTable live)
	{
		var liveNames = live.Columns.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
		var add = new MigrationBuilder(Provider);
		foreach (var column in expected.Columns)
		{
			if (liveNames.Contains(column.Name) || column.IsPrimaryKey)
			{
				continue;
			}

			add.AddColumn(expected.Name, column);
		}

		foreach (var statement in add.Statements)
		{
			ExecuteScript(statement);
		}
	}

	private void AlignSqlServerTable(SqlTable expected, SqlTable live)
	{
		var liveMap = live.Columns.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
		var alter = new MigrationBuilder(Provider);
		foreach (var column in expected.Columns)
		{
			if (!liveMap.TryGetValue(column.Name, out var liveColumn))
			{
				if (!column.IsPrimaryKey)
				{
					alter.AddColumn(expected.Name, column);
				}

				continue;
			}

			if (!ColumnNeedsModify(liveColumn, column))
			{
				continue;
			}

			if ((liveColumn.IsPrimaryKey != column.IsPrimaryKey)
				|| (liveColumn.IsAutoIncrement != column.IsAutoIncrement))
			{
				throw new InvalidOperationException(
					$"SQL Server cannot change primary key or identity on '{expected.Name}.{column.Name}'. Write a custom Sql() rebuild.");
			}

			alter.AlterColumn(expected.Name, column);
		}

		foreach (var statement in alter.Statements)
		{
			ExecuteScript(statement);
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "CreateTable entity types come from host migrations.")]
	private void AlignTableSchema(Type entityType)
	{
		var expected = SqlGenerator.GetExpectedTableInfo(Provider, entityType);
		var live = QueryTables().FirstOrDefault(x => string.Equals(x.Name, expected.Name, StringComparison.OrdinalIgnoreCase));
		if (live == null)
		{
			var sourceType = SourceReflector.GetRequiredSourceType(entityType);
			ExecuteScript(SqlGenerator.GetCreateTableScript(sourceType, Provider, false));
			live = QueryTables().FirstOrDefault(x => string.Equals(x.Name, expected.Name, StringComparison.OrdinalIgnoreCase));
			if (live != null)
			{
				ApplyMissingIndexes(expected, live);
				if (Provider == SqlProvider.SqlServer)
				{
					ApplyMissingSqlServerForeignKeys(expected, live);
				}
			}

			return;
		}

		if (Provider == SqlProvider.Sqlite)
		{
			if (NeedsSqliteRebuild(live, expected))
			{
				RebuildSqliteTable(entityType, live, expected);
				live = QueryTables().FirstOrDefault(x => string.Equals(x.Name, expected.Name, StringComparison.OrdinalIgnoreCase));
			}
			else
			{
				AddMissingColumns(expected, live);
				live = QueryTables().FirstOrDefault(x => string.Equals(x.Name, expected.Name, StringComparison.OrdinalIgnoreCase)) ?? live;
			}

			ApplyMissingIndexes(expected, live);
			return;
		}

		AlignSqlServerTable(expected, live);
		live = QueryTables().FirstOrDefault(x => string.Equals(x.Name, expected.Name, StringComparison.OrdinalIgnoreCase)) ?? live;
		ApplyMissingIndexes(expected, live);
		ApplyMissingSqlServerForeignKeys(expected, live);
	}

	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "CreateTable entity types come from host migrations.")]
	private void ApplyBuilder(MigrationBuilder builder)
	{
		var skipStatements = new HashSet<string>(StringComparer.Ordinal);
		var aligned = new HashSet<Type>();
		foreach (var entityType in builder.RebuildTableTypes.Concat(builder.CreateTableTypes))
		{
			if (!aligned.Add(entityType))
			{
				continue;
			}

			var sourceType = SourceReflector.GetRequiredSourceType(entityType);
			skipStatements.Add(SqlGenerator.GetCreateTableScript(sourceType, Provider, false));
			skipStatements.Add("-- RebuildTable " + entityType.FullName);
			AlignTableSchema(entityType);
		}

		foreach (var statement in builder.Statements)
		{
			if (skipStatements.Contains(statement))
			{
				continue;
			}

			ExecuteScript(statement);
		}
	}

	private void ApplyMissingIndexes(SqlTable expected, SqlTable live)
	{
		if ((expected == null) || (live == null))
		{
			return;
		}

		var liveSignatures = live.Indexes.Select(IndexSignature).ToHashSet(StringComparer.Ordinal);
		var add = new MigrationBuilder(Provider);
		foreach (var index in expected.Indexes)
		{
			if (liveSignatures.Contains(IndexSignature(index)))
			{
				continue;
			}

			add.CreateIndex(expected.Name, index);
		}

		foreach (var statement in add.Statements)
		{
			ExecuteScript(statement);
		}
	}

	private void ApplyMissingSqlServerForeignKeys(SqlTable expected, SqlTable live)
	{
		if ((expected == null) || (live == null))
		{
			return;
		}

		var add = new MigrationBuilder(Provider);
		foreach (var fk in expected.ForeignKeys)
		{
			if (live.ForeignKeys.Any(l => ForeignKeysEqual(l, fk)))
			{
				continue;
			}

			add.AddForeignKey(expected.Name, fk);
		}

		foreach (var statement in add.Statements)
		{
			ExecuteScript(statement);
		}
	}

	private static bool ColumnNeedsModify(SqlTableColumn live, SqlTableColumn expected)
	{
		return (live.IsNullable != expected.IsNullable)
			|| (live.IsPrimaryKey != expected.IsPrimaryKey)
			|| (live.IsAutoIncrement != expected.IsAutoIncrement)
			|| !string.Equals(
				SqlGenerator.NormalizeDeclaredType(live.ColumnType),
				SqlGenerator.NormalizeDeclaredType(expected.ColumnType),
				StringComparison.OrdinalIgnoreCase);
	}

	private void EnsureHistoryTable()
	{
		ExecuteScript(MigrationBuilder.GetCreateHistoryTableScript(Provider));
	}

	private static void ExecuteScript(DbConnection connection, string script, DbTransaction transaction)
	{
		using var command = connection.CreateCommand();
		command.CommandText = script;
		command.Transaction = transaction;
		command.ExecuteNonQuery();
	}

	private void ExecuteScript(string script)
	{
		WithConnection((connection, transaction) => ExecuteScript(connection, script, transaction));
	}

	private static bool ForeignKeysEqual(SqlForeignKey left, SqlForeignKey right)
	{
		if (!string.Equals(left.PrincipalTable, right.PrincipalTable, StringComparison.OrdinalIgnoreCase)
			|| !string.Equals(left.PrincipalColumn, right.PrincipalColumn, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		if (left.Columns.Count != right.Columns.Count)
		{
			return false;
		}

		for (var i = 0; i < left.Columns.Count; i++)
		{
			if (!string.Equals(left.Columns[i].ColumnName, right.Columns[i].ColumnName, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}

		return true;
	}

	private static string IndexSignature(SqlIndex index)
	{
		var columns = string.Join(",", index.Columns.OrderBy(x => x.Ordinal).Select(x => x.ColumnName.ToUpperInvariant()));
		return (index.IsUnique ? "U:" : "I:") + columns;
	}

	private void InsertHistory(string migrationId, string productVersion)
	{
		var (open, close) = SqlGenerator.GetIdentifierBrackets(Provider);
		WithConnection((connection, transaction) =>
		{
			using var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText =
				$"INSERT INTO {open}{SqlMigration.HistoryTableName}{close} ({open}MigrationId{close}, {open}ProductVersion{close}) VALUES (@id, @version)";

			var id = command.CreateParameter();
			id.ParameterName = "@id";
			id.Value = migrationId;
			command.Parameters.Add(id);

			var version = command.CreateParameter();
			version.ParameterName = "@version";
			version.Value = productVersion;
			command.Parameters.Add(version);

			command.ExecuteNonQuery();
		});
	}

	private void LoadForeignKeys(List<SqlTable> tables)
	{
		var map = tables.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
		WithConnection((connection, transaction) =>
		{
			using var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = SqlGenerator.GetForeignKeyQueryScript(Provider);
			using var reader = command.ExecuteReader();
			while (reader.Read())
			{
				var tableName = reader["TableName"].ToString();
				if (!map.TryGetValue(tableName, out var table))
				{
					continue;
				}

				var principalTable = reader["PrincipalTable"].ToString();
				var columnName = reader["ColumnName"].ToString();
				var principalColumn = reader["PrincipalColumn"].ToString();
				var ordinal = System.Convert.ToInt32(reader["Ordinal"]);
				var name = Provider == SqlProvider.SqlServer
					? reader["ForeignKeyName"].ToString()
					: "FK_" + tableName + "_" + principalTable + "_" + columnName;
				var fk = table.ForeignKeys.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
				if (fk == null)
				{
					fk = new SqlForeignKey();
					fk.Name = name;
					fk.PrincipalTable = principalTable;
					fk.PrincipalColumn = principalColumn;
					table.ForeignKeys.Add(fk);
				}

				var fkColumn = new SqlForeignKeyColumn();
				fkColumn.ColumnName = columnName;
				fkColumn.Ordinal = ordinal;
				fk.Columns.Add(fkColumn);
			}
		});
	}

	private void LoadIndexes(List<SqlTable> tables)
	{
		var map = tables.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
		WithConnection((connection, transaction) =>
		{
			using var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = SqlGenerator.GetIndexQueryScript(Provider);
			using var reader = command.ExecuteReader();
			SqlIndex index = null;
			string indexTable = null;
			while (reader.Read())
			{
				var tableName = reader["TableName"].ToString();
				var indexName = reader["IndexName"].ToString();
				if ((index == null)
					|| (indexTable != tableName)
					|| !string.Equals(index.Name, indexName, StringComparison.OrdinalIgnoreCase))
				{
					if (!map.TryGetValue(tableName, out var table))
					{
						index = null;
						continue;
					}

					index = new SqlIndex();
					index.Name = indexName;
					index.IsUnique = System.Convert.ToInt32(reader["IsUnique"]) != 0;
					table.Indexes.Add(index);
					indexTable = tableName;
				}

				var indexColumn = new SqlIndexColumn();
				indexColumn.ColumnName = reader["ColumnName"].ToString();
				indexColumn.Ordinal = System.Convert.ToInt32(reader["Ordinal"]);
				index.Columns.Add(indexColumn);
			}
		});
	}

	private List<SqlTable> LoadTables()
	{
		var tables = new List<SqlTable>();
		WithConnection((connection, transaction) =>
		{
			using var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = SqlGenerator.GetTableQueryScript(Provider);
			using var reader = command.ExecuteReader();
			SqlTable table = null;
			while (reader.Read())
			{
				var tableName = reader["TableName"].ToString();
				if ((table == null) || (tableName != table.Name))
				{
					if (table != null)
					{
						tables.Add(table);
					}

					table = new SqlTable(reader);
				}

				var column = SqlTableColumn.FromReader(reader);
				if (!table.Columns.Any(c => string.Equals(c.Name, column.Name, StringComparison.OrdinalIgnoreCase)))
				{
					table.Columns.Add(column);
				}
			}

			if (table != null)
			{
				tables.Add(table);
			}
		});

		return tables;
	}

	private static bool NeedsSqliteRebuild(SqlTable live, SqlTable expected)
	{
		var liveMap = live.Columns.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
		foreach (var column in expected.Columns)
		{
			if (liveMap.TryGetValue(column.Name, out var liveColumn) && ColumnNeedsModify(liveColumn, column))
			{
				return true;
			}
		}

		return expected.ForeignKeys.Any(fk => live.ForeignKeys.All(l => !ForeignKeysEqual(l, fk)));
	}

	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Rebuild entity types come from host migrations.")]
	private void RebuildSqliteTable(Type entityType, SqlTable live, SqlTable expected)
	{
		var (open, close) = SqlGenerator.GetIdentifierBrackets(Provider);
		var tableName = expected.Name;
		var oldName = tableName + "__Old";
		var sourceType = SourceReflector.GetRequiredSourceType(entityType);
		var create = SqlGenerator.GetCreateTableScript(sourceType, Provider, false);

		if (_workConnection != null)
		{
			RunSqliteRebuild(_workConnection, _workTransaction, open, close, tableName, oldName, create, live, expected);
			return;
		}

		if (_connection != null)
		{
			using var keepAliveTransaction = _connection.BeginTransaction();
			RunSqliteRebuild(_connection, keepAliveTransaction, open, close, tableName, oldName, create, live, expected);
			keepAliveTransaction.Commit();
			return;
		}

		using var connection = CreateConnection();
		connection.Open();
		using var transaction = connection.BeginTransaction();
		RunSqliteRebuild(connection, transaction, open, close, tableName, oldName, create, live, expected);
		transaction.Commit();
	}

	private static void RunSqliteRebuild(
		DbConnection connection,
		DbTransaction transaction,
		string open,
		string close,
		string tableName,
		string oldName,
		string create,
		SqlTable live,
		SqlTable expected)
	{
		ExecuteScript(connection, $"DROP TABLE IF EXISTS {open}{oldName}{close}", transaction);
		ExecuteScript(connection, $"ALTER TABLE {open}{tableName}{close} RENAME TO {open}{oldName}{close}", transaction);
		ExecuteScript(connection, create, transaction);
		if (expected.Columns.Count > 0)
		{
			var insertNames = new List<string>();
			var selectParts = new List<string>();
			foreach (var column in expected.Columns)
			{
				insertNames.Add(open + column.Name + close);
				if (live.Columns.Any(c => string.Equals(c.Name, column.Name, StringComparison.OrdinalIgnoreCase)))
				{
					selectParts.Add(open + column.Name + close);
				}
				else if (column.IsNullable
						|| expected.ForeignKeys.Any(fk => fk.Columns.Any(c => string.Equals(c.ColumnName, column.Name, StringComparison.OrdinalIgnoreCase))))
				{
					selectParts.Add("NULL");
				}
				else if (string.Equals(column.ClrTypeName, "System.Guid", StringComparison.OrdinalIgnoreCase))
				{
					selectParts.Add("'00000000-0000-0000-0000-000000000000'");
				}
				else if (string.Equals(column.ClrTypeName, "System.DateTime", StringComparison.OrdinalIgnoreCase)
						|| (SqlGenerator.NormalizeDeclaredType(column.ColumnType) == "DATE"))
				{
					selectParts.Add("'0001-01-01'");
				}
				else
				{
					selectParts.Add(MigrationBuilder.InferDefaultLiteral(column.ColumnType));
				}
			}

			ExecuteScript(
				connection,
				$"INSERT INTO {open}{tableName}{close} ({string.Join(", ", insertNames)}) SELECT {string.Join(", ", selectParts)} FROM {open}{oldName}{close}",
				transaction);
		}

		ExecuteScript(connection, $"DROP TABLE {open}{oldName}{close}", transaction);
	}

	private void WithConnection(Action<DbConnection, DbTransaction> action)
	{
		CommandCount++;
		if (_workConnection != null)
		{
			action(_workConnection, _workTransaction);
			return;
		}

		if (_connection != null)
		{
			action(_connection, null);
			return;
		}

		using var connection = CreateConnection();
		connection.Open();
		action(connection, null);
	}

	#endregion
}