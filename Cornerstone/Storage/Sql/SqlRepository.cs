#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using Cornerstone.Reflection;

#endregion

namespace Cornerstone.Storage.Sql;

public class SqlRepository<T> : ISqlPendingRepository
	where T : Entity, new()
{
	#region Fields

	private readonly HashSet<T> _pendingDeletes;
	private readonly HashSet<T> _pendingInserts;
	private readonly SourceTypeInfo _sourceTypeInfo;

	#endregion

	#region Constructors

	public SqlRepository(SqlDatabase database)
	{
		_sourceTypeInfo = SourceReflector.GetRequiredSourceType<T>();
		_pendingDeletes = [];
		_pendingInserts = [];

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
		_pendingDeletes.Remove(entity);
		_pendingInserts.Add(entity);
		AttachLoaded(entity);
	}

	public bool Any()
	{
		return Any(x => true);
	}

	public bool Any(Expression<Func<T, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);

		var (sql, parameters) = SqlGenerator.GetExistsWhereQuery(predicate, Database.Provider);
		return System.Convert.ToInt32(Database.ExecuteScalar(sql, command => BindParameters(command, parameters))) != 0;
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
		return System.Convert.ToInt32(Database.ExecuteScalar(sql, command => BindParameters(command, parameters)));
	}

	/// <summary>
	/// Queues a delete by primary key. Flushed on SaveChanges. A row that was only
	/// added in this unit of work is dropped from pending inserts instead.
	/// </summary>
	public int Delete(T entity)
	{
		ArgumentNullException.ThrowIfNull(entity);
		if (_pendingInserts.Remove(entity))
		{
			return 1;
		}

		_pendingDeletes.Add(entity);
		return 1;
	}

	/// <summary>
	/// Queues deletes by primary key. Flushed on SaveChanges.
	/// </summary>
	public int Delete(IReadOnlyList<T> entities)
	{
		ArgumentNullException.ThrowIfNull(entities);
		var count = 0;
		for (var i = 0; i < entities.Count; i++)
		{
			count += Delete(entities[i]);
		}

		return count;
	}

	/// <summary>
	/// Deletes all rows matching the predicate.
	/// </summary>
	/// <returns> The number of rows deleted. </returns>
	public int DeleteWhere(Expression<Func<T, bool>> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);

		var (sql, parameters) = SqlGenerator.GetDeleteWhereQuery(predicate, Database.Provider);
		return Database.ExecuteNonQuery(sql, command => BindParameters(command, parameters));
	}

	/// <summary>
	/// Creates the table if it does not already exist
	/// </summary>
	public void EnsureTableCreated()
	{
		var sql = SqlGenerator.GetCreateTableScript(_sourceTypeInfo, Database.Provider);
		Database.ExecuteNonQuery(sql, null);
	}

	public T GetById(object id)
	{
		ArgumentNullException.ThrowIfNull(id);

		var pk = SqlGenerator.GetPrimaryKeyProperty(_sourceTypeInfo);
		if (pk?.PropertyInfo == null)
		{
			throw new InvalidOperationException(
				$"No primary key mapped for '{typeof(T).Name}'.");
		}

		var pkType = pk.PropertyInfo.PropertyType;
		if (pkType.IsNullableType())
		{
			pkType = pkType.FromNullableType();
		}

		if (!pkType.IsInstanceOfType(id))
		{
			id = System.Convert.ChangeType(id, pkType);
		}

		var parameter = Expression.Parameter(typeof(T), "x");
		var body = Expression.Equal(
			Expression.Property(parameter, pk.PropertyInfo),
			Expression.Constant(id, pk.PropertyInfo.PropertyType)
		);
		var predicate = Expression.Lambda<Func<T, bool>>(body, parameter);
		var item = Where(predicate).Query().FirstOrDefault();
		if (item != null)
		{
			AttachLoaded(item);
		}

		return item;
	}

	/// <summary>
	/// Inserts or updates by primary key. Unlike UpsertSync, ModifiedOn is not compared.
	/// Returns the number of rows written.
	/// </summary>
	public int Upsert(T entity)
	{
		ArgumentNullException.ThrowIfNull(entity);

		var (sql, values) = SqlGenerator.GetInsertQuery(entity, Database.Provider);
		var id = ExecuteScalar(sql, values);
		if ((id == null) || (id == DBNull.Value))
		{
			return 0;
		}

		AssignPrimaryKey(entity, id);
		return 1;
	}

	/// <summary>
	/// Inserts or updates by SyncId. An existing row is updated only when the incoming
	/// ModifiedOn is newer. Returns the number of rows written (0 if the local row is newer).
	/// </summary>
	public int UpsertSync(T entity)
	{
		return UpsertSync(entity, true);
	}

	/// <summary>
	/// Inserts or updates by SyncId. When requireNewerModifiedOn is false, an existing row
	/// is updated even if incoming ModifiedOn is not newer (applied tombstone).
	/// </summary>
	public int UpsertSync(T entity, bool requireNewerModifiedOn)
	{
		ArgumentNullException.ThrowIfNull(entity);

		var (sql, values) = SqlGenerator.GetSyncUpsertQuery(entity, Database.Provider, requireNewerModifiedOn);
		var id = ExecuteScalar(sql, values);
		if ((id == null) || (id == DBNull.Value))
		{
			return 0;
		}

		AssignPrimaryKey(entity, id);
		return 1;
	}

	/// <summary>
	/// Inserts or updates many rows by SyncId in one command (chunked). Returns rows written.
	/// </summary>
	public int UpsertSync(IReadOnlyList<T> entities)
	{
		return UpsertSync(entities, true);
	}

	/// <summary>
	/// Inserts or updates many rows by SyncId. When requireNewerModifiedOn is false,
	/// existing rows are updated even if incoming ModifiedOn is not newer.
	/// </summary>
	public int UpsertSync(IReadOnlyList<T> entities, bool requireNewerModifiedOn)
	{
		ArgumentNullException.ThrowIfNull(entities);
		if (entities.Count == 0)
		{
			return 0;
		}

		if (entities.Count == 1)
		{
			return UpsertSync(entities[0], requireNewerModifiedOn);
		}

		var width = InsertColumnCount();
		return WriteChunks(entities, width, chunk => UpsertSyncChunk(chunk, requireNewerModifiedOn));
	}

	public SqlQuery<T> Where(Expression<Func<T, bool>> predicate)
	{
		return new SqlQuery<T>(Database).Where(predicate);
	}

	internal void AttachLoaded(T entity)
	{
		if (entity is INotifyPropertyChanged notifier)
		{
			notifier.PropertyChanged -= OnEntityPropertyChanged;
			notifier.PropertyChanged += OnEntityPropertyChanged;
		}
	}

	internal int DeleteNow(IReadOnlyList<T> entities)
	{
		if ((entities == null) || (entities.Count == 0))
		{
			return 0;
		}

		return DeleteImmediate(entities);
	}

	private void AssignPrimaryKey(T entity, object id)
	{
		var property = SqlGenerator.GetPrimaryKeyProperty(_sourceTypeInfo);
		if (property?.SetValue == null)
		{
			return;
		}

		var targetType = property.PropertyInfo.PropertyType;
		if (targetType.IsNullableType())
		{
			targetType = targetType.FromNullableType();
		}

		if (!targetType.IsInstanceOfType(id))
		{
			id = System.Convert.ChangeType(id, targetType);
		}

		property.SetValue(entity, id);
	}

	private int AssignReturnedIds(IReadOnlyList<T> entities, DbDataReader reader)
	{
		var syncProperty = _sourceTypeInfo.GetProperty("SyncId");
		Dictionary<Guid, T> bySyncId = null;
		if ((syncProperty != null) && (reader.FieldCount > 1))
		{
			bySyncId = new Dictionary<Guid, T>(entities.Count);
			for (var i = 0; i < entities.Count; i++)
			{
				if (syncProperty.GetValue(entities[i]) is Guid syncId)
				{
					bySyncId[syncId] = entities[i];
				}
			}
		}

		var written = 0;
		var ordinal = 0;
		while (reader.Read())
		{
			var id = reader.GetValue(0);
			if ((id == null) || (id == DBNull.Value))
			{
				ordinal++;
				continue;
			}

			if (bySyncId != null)
			{
				var syncValue = reader.GetValue(1);
				if ((syncValue != null) && (syncValue != DBNull.Value))
				{
					var syncId = syncValue is Guid guid ? guid : Guid.Parse(syncValue.ToString());
					if (bySyncId.TryGetValue(syncId, out var entity))
					{
						AssignPrimaryKey(entity, id);
						written++;
					}

					ordinal++;
					continue;
				}
			}

			if (ordinal < entities.Count)
			{
				AssignPrimaryKey(entities[ordinal], id);
				written++;
			}

			ordinal++;
		}

		return written;
	}

	private static void BindNamedParameters(DbCommand command, IDictionary<string, (object, Type)> values)
	{
		if (values == null)
		{
			return;
		}

		foreach (var kv in values)
		{
			var param = command.CreateParameter();
			param.ParameterName = kv.Key;
			param.Value = kv.Value.Item1 ?? DBNull.Value;
			param.DbType = SqlGenerator.GetParameterDbType(kv.Value.Item2);
			command.Parameters.Add(param);
		}
	}

	private static void BindParameters(DbCommand command, object[] parameters)
	{
		if (parameters == null)
		{
			return;
		}

		for (var i = 0; i < parameters.Length; i++)
		{
			var param = command.CreateParameter();
			param.ParameterName = $"@p{i}";
			param.Value = parameters[i] ?? DBNull.Value;
			if (parameters[i] != null)
			{
				param.DbType = SqlGenerator.GetParameterDbType(parameters[i].GetType());
			}
			command.Parameters.Add(param);
		}
	}

	private int DeleteImmediate(IReadOnlyList<T> entities)
	{
		if (entities.Count == 1)
		{
			var (sql, pkValue) = SqlGenerator.GetDeleteQuery(entities[0], Database.Provider);
			return Database.ExecuteNonQuery(sql, command =>
			{
				var param = command.CreateParameter();
				param.ParameterName = "@p0";
				param.Value = pkValue.Item1 ?? DBNull.Value;
				param.DbType = SqlGenerator.GetParameterDbType(pkValue.Item2);
				command.Parameters.Add(param);
			});
		}

		return WriteChunks(entities, 1, chunk =>
		{
			var (sql, values) = SqlGenerator.GetDeleteBatchQuery(chunk, Database.Provider);
			return ExecuteNonQuery(sql, values);
		});
	}

	void ISqlPendingRepository.DiscardPending()
	{
		_pendingInserts.Clear();
		_pendingDeletes.Clear();
	}

	private int ExecuteNonQuery(string sql, IDictionary<string, (object, Type)> values)
	{
		return Database.ExecuteNonQuery(sql, command => BindNamedParameters(command, values));
	}

	private object ExecuteScalar(string sql, IDictionary<string, (object, Type)> values)
	{
		return Database.ExecuteScalar(sql, command => BindNamedParameters(command, values));
	}

	private bool HasDefaultKey(T entity)
	{
		var property = SqlGenerator.GetPrimaryKeyProperty(_sourceTypeInfo);
		var value = property?.GetValue(entity);
		return value switch
		{
			null => true,
			int i => i == 0,
			long l => l == 0,
			short s => s == 0,
			Guid g => g == Guid.Empty,
			_ => false
		};
	}

	private int InsertChunk(IReadOnlyList<T> entities)
	{
		var (sql, values) = SqlGenerator.GetInsertBatchQuery(entities, Database.Provider);
		return Database.ExecuteOnCommand(sql, command => BindNamedParameters(command, values), command =>
		{
			using var reader = command.ExecuteReader();
			return AssignReturnedIds(entities, reader);
		});
	}

	private int InsertColumnCount()
	{
		return SqlGenerator.GetExpectedTableInfo(Database.Provider, typeof(T)).Columns
			.Count(c => !c.IsPrimaryKey);
	}

	private int InsertImmediate(IReadOnlyList<T> entities)
	{
		if (entities.Count == 1)
		{
			return Upsert(entities[0]);
		}

		var width = InsertColumnCount();
		return WriteChunks(entities, width, InsertChunk);
	}

	private void OnEntityPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (sender is T item)
		{
			_pendingDeletes.Remove(item);
			_pendingInserts.Add(item);
		}
	}

	int ISqlPendingRepository.SavePending()
	{
		var written = 0;
		if (_pendingDeletes.Count > 0)
		{
			written += DeleteImmediate(_pendingDeletes.ToList());
		}

		if (_pendingInserts.Count > 0)
		{
			var inserts = new List<T>();
			var updates = new List<T>();
			foreach (var entity in _pendingInserts)
			{
				if (HasDefaultKey(entity))
				{
					inserts.Add(entity);
				}
				else
				{
					updates.Add(entity);
				}
			}

			if (inserts.Count > 0)
			{
				written += InsertImmediate(inserts);
			}

			if (updates.Count > 0)
			{
				written += UpdateImmediate(updates);
			}
		}

		_pendingInserts.Clear();
		_pendingDeletes.Clear();
		return written;
	}

	private int UpdateImmediate(IReadOnlyList<T> entities)
	{
		var written = 0;
		for (var i = 0; i < entities.Count; i++)
		{
			var (sql, values) = SqlGenerator.GetUpdateByIdQuery(entities[i], Database.Provider);
			written += ExecuteNonQuery(sql, values);
		}

		return written;
	}

	private int UpsertSyncChunk(IReadOnlyList<T> entities, bool requireNewerModifiedOn)
	{
		var (sql, values) = SqlGenerator.GetSyncUpsertBatchQuery(entities, Database.Provider, requireNewerModifiedOn);
		return Database.ExecuteOnCommand(sql, command => BindNamedParameters(command, values), command =>
		{
			using var reader = command.ExecuteReader();
			return AssignReturnedIds(entities, reader);
		});
	}

	private int WriteChunks(IReadOnlyList<T> entities, int parameterWidth, Func<IReadOnlyList<T>, int> write)
	{
		var maxRows = SqlGenerator.GetMaxBatchRows(Database.Provider, parameterWidth);
		var written = 0;
		for (var offset = 0; offset < entities.Count; offset += maxRows)
		{
			var take = Math.Min(maxRows, entities.Count - offset);
			var chunk = take == entities.Count
				? entities
				: entities.Skip(offset).Take(take).ToList();
			written += write(chunk);
		}

		return written;
	}

	#endregion
}

internal interface ISqlPendingRepository
{
	#region Methods

	void DiscardPending();

	int SavePending();

	#endregion
}