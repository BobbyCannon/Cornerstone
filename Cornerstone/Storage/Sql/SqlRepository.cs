#region References

using System;
using System.Data;
using System.Data.Common;
using Cornerstone.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

#endregion

namespace Cornerstone.Storage.Sql;

public class SqlRepository<T>
	: IRepository<T>
	where T : Entity, new()
{
	#region Fields

	private readonly SourceTypeInfo _sourceTypeInfo;

	#endregion

	#region Constructors

	public SqlRepository(string connectionString, SqlProvider provider)
	{
		_sourceTypeInfo = SourceReflector.GetRequiredSourceType<T>();

		ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
		Provider = provider;
	}

	#endregion

	#region Properties

	public string ConnectionString { get; }

	public SqlProvider Provider { get; }

	#endregion

	#region Methods

	public void Add(T entity)
	{
		ArgumentNullException.ThrowIfNull(entity);

		var (sql, values) = SqlGenerator.GetInsertQuery(entity, Provider);
		using var connection = CreateConnection();
		connection.Open();

		using var command = connection.CreateCommand();
		command.CommandText = sql;

		if (values != null)
		{
			foreach (var kv in values)
			{
				var param = command.CreateParameter();
				param.ParameterName = kv.Key;
				param.Value = kv.Value ?? DBNull.Value;
				param.DbType = GetParameterType(param.Value);

				command.Parameters.Add(param);
			}
		}

		var rows = command.ExecuteNonQuery();
		if (rows != 1)
		{
			throw new InvalidOperationException("Add failed");
		}
	}

	/// <summary>
	/// Creates the table if it does not already exist
	/// </summary>
	public void EnsureTableCreated()
	{
		var sql = SqlGenerator.GetCreateTableScript(_sourceTypeInfo, Provider);
		using var connection = CreateConnection();
		connection.Open();

		using var command = connection.CreateCommand();
		command.CommandText = sql;
		command.ExecuteNonQuery();
	}

	public SqlQuery<T> Where(Func<T, bool> predicate)
	{
		return new SqlQuery<T>(ConnectionString, Provider);
	}

	private DbConnection CreateConnection()
	{
		return Provider == SqlProvider.SqlServer
			? new SqlConnection(ConnectionString)
			: new SqliteConnection(ConnectionString);
	}

	private DbType GetParameterType(object value)
	{
		return value switch
		{
			string => DbType.String,
			int => DbType.Int32,
			uint => DbType.UInt32,
			long => DbType.Int64,
			ulong => DbType.UInt64,
			DateTime => DbType.DateTime2,
			Guid => DbType.Guid,
			bool => DbType.Boolean,
			_ => throw new NotSupportedException($"Parameter type {value.GetType().FullName} value not supported.")
		};
	}

	#endregion
}