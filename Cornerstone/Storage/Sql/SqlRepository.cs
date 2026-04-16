#region References

using System;
using System.Data;
using System.Linq.Expressions;
using Cornerstone.Extensions;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Storage.Sql;

public class SqlRepository<T>
	where T : Entity, new()
{
	#region Fields

	private readonly SourceTypeInfo _sourceTypeInfo;

	#endregion

	#region Constructors

	public SqlRepository(SqlDatabase database)
	{
		_sourceTypeInfo = SourceReflector.GetRequiredSourceType<T>();

		Database = database;
	}

	#endregion

	#region Properties

	public SqlDatabase Database { get; }

	#endregion

	#region Methods

	public void Add(T entity)
	{
		ArgumentNullException.ThrowIfNull(entity);

		var (sql, values) = SqlGenerator.GetInsertQuery(entity, Database.Provider);
		using var connection = Database.CreateConnection();
		connection.Open();

		using var command = connection.CreateCommand();
		command.CommandText = sql;

		if (values != null)
		{
			foreach (var kv in values)
			{
				var param = command.CreateParameter();
				param.ParameterName = kv.Key;
				param.Value = kv.Value.Item1 ?? DBNull.Value;
				param.DbType = GetParameterType(kv.Value.Item2);

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
	/// Deletes a single entity by its primary key.
	/// </summary>
	public int Delete(T entity)
	{
		ArgumentNullException.ThrowIfNull(entity);

		var (sql, pkValue) = SqlGenerator.GetDeleteQuery(entity, Database.Provider);
		using var connection = Database.CreateConnection();
		connection.Open();

		using var command = connection.CreateCommand();
		command.CommandText = sql;

		var param = command.CreateParameter();
		param.ParameterName = "@p0";
		param.Value = pkValue.Item1 ?? DBNull.Value;
		param.DbType = GetParameterType(pkValue.Item2);
		command.Parameters.Add(param);

		return command.ExecuteNonQuery();
	}

	/// <summary>
	/// Deletes all rows matching the predicate.
	/// </summary>
	/// <returns> The number of rows deleted. </returns>
	public int DeleteWhere(Expression<Func<T, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);

		var (sql, parameters) = SqlGenerator.GetDeleteWhereQuery(predicate, Database.Provider);
		using var connection = Database.CreateConnection();
		connection.Open();

		using var command = connection.CreateCommand();
		command.CommandText = sql;

		for (var i = 0; i < parameters.Length; i++)
		{
			var param = command.CreateParameter();
			param.ParameterName = $"@p{i}";
			param.Value = parameters[i] ?? DBNull.Value;
			command.Parameters.Add(param);
		}

		return command.ExecuteNonQuery();
	}

	/// <summary>
	/// Creates the table if it does not already exist
	/// </summary>
	public void EnsureTableCreated()
	{
		var sql = SqlGenerator.GetCreateTableScript(_sourceTypeInfo, Database.Provider);
		using var connection = Database.CreateConnection();
		connection.Open();

		using var command = connection.CreateCommand();
		command.CommandText = sql;
		command.ExecuteNonQuery();
	}

	public SqlQuery<T> Where(Expression<Func<T, bool>> predicate)
	{
		return new SqlQuery<T>(Database.ConnectionString, Database.Provider).Where(predicate);
	}

	/// <summary>
	/// Counts all rows matching the predicate.
	/// </summary>
	/// <returns> The number of rows matching the predicate. </returns>
	public int Count()
	{
		return Count(x => true);
	}

	/// <summary>
	/// Counts all rows matching the predicate.
	/// </summary>
	/// <returns> The number of rows matching the predicate. </returns>
	public int Count(Expression<Func<T, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);

		var (sql, parameters) = SqlGenerator.GetCountWhereQuery(predicate, Database.Provider);
		using var connection = Database.CreateConnection();
		connection.Open();

		using var command = connection.CreateCommand();
		command.CommandText = sql;

		for (var i = 0; i < parameters.Length; i++)
		{
			var param = command.CreateParameter();
			param.ParameterName = $"@p{i}";
			param.Value = parameters[i] ?? DBNull.Value;
			command.Parameters.Add(param);
		}

		return System.Convert.ToInt32(command.ExecuteScalar());
	}

	private DbType GetParameterType(Type type)
	{
		if (type.IsNullableType())
		{
			type = type.FromNullableType();
		}

		if (type.IsEnum)
		{
			// Handle enums by mapping to their underlying integral type
			type = Enum.GetUnderlyingType(type);
		}

		return type switch
		{
			not null when ReferenceEquals(type, typeof(string)) => DbType.String,
			not null when ReferenceEquals(type, typeof(byte)) => DbType.Byte,
			not null when ReferenceEquals(type, typeof(sbyte)) => DbType.SByte,
			not null when ReferenceEquals(type, typeof(short)) => DbType.Int16,
			not null when ReferenceEquals(type, typeof(ushort)) => DbType.UInt16,
			not null when ReferenceEquals(type, typeof(int)) => DbType.Int32,
			not null when ReferenceEquals(type, typeof(uint)) => DbType.UInt32,
			not null when ReferenceEquals(type, typeof(long)) => DbType.Int64,
			not null when ReferenceEquals(type, typeof(ulong)) => DbType.UInt64,
			not null when ReferenceEquals(type, typeof(DateTime)) => DbType.DateTime2,
			not null when ReferenceEquals(type, typeof(Guid)) => DbType.Guid,
			not null when ReferenceEquals(type, typeof(bool)) => DbType.Boolean,
			not null when ReferenceEquals(type, typeof(decimal)) => DbType.Decimal,
			not null when ReferenceEquals(type, typeof(double)) => DbType.Double,
			not null when ReferenceEquals(type, typeof(float)) => DbType.Double,
			_ => throw new NotSupportedException(
				$"Parameter type {type.FullName} is not supported.")
		};
	}

	#endregion
}