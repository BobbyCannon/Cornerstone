#region References

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Cornerstone.Attributes;
using Cornerstone.Data;
using Cornerstone.Storage;
using UpdateableAction = Cornerstone.Data.UpdateableAction;

#endregion

namespace Cornerstone.Collections;

/// <summary>
/// Represents a data repository.
/// </summary>
/// <typeparam name="T"> The type of the data. </typeparam>
public abstract class DataRepository<T> : IDataRepository<T>
	where T : IUpdateable, new()
{
	#region Fields

	private readonly ICollection<T> _data;

	private readonly IDatabase _database;

	#endregion

	#region Constructors

	/// <summary>
	/// Instantiate a data repository.
	/// </summary>
	/// <param name="database"> The database the collection resides in. </param>
	/// <param name="data"> The collection of all data. </param>
	protected DataRepository(IDatabase database, ICollection<T> data)
	{
		_database = database;
		_data = data;
	}

	#endregion

	#region Properties

	/// <summary>
	/// The predicate to load data from repository.
	/// </summary>
	[Browsable(false)]
	[SerializationIgnore]
	protected abstract Func<T, bool> LoadPredicate { get; }

	/// <summary>
	/// The predicate to look up a single data value from repository.
	/// </summary>
	[Browsable(false)]
	[SerializationIgnore]
	protected abstract Func<T, T, bool> LookupPredicate { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public void AddOrUpdate(T data)
	{
		var foundSetting = _data.FirstOrDefault(x => LookupPredicate(x, data));
		if (foundSetting != null)
		{
			foundSetting.UpdateWith(data, UpdateableAction.SyncIncomingUpdate);
			return;
		}

		foundSetting = new T();
		foundSetting.UpdateWith(data, UpdateableAction.SyncIncomingAdd);

		_data.Add(foundSetting);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_database?.Dispose();
	}

	/// <inheritdoc />
	public IEnumerable<T> Load()
	{
		return _data.Where(LoadPredicate);
	}

	/// <inheritdoc />
	public void SaveChanges()
	{
		_database?.SaveChanges();
	}

	#endregion
}

/// <summary>
/// Represents a data repository.
/// </summary>
/// <typeparam name="T"> The type of the data. </typeparam>
public interface IDataRepository<T> : IDisposable
	where T : IUpdateable
{
	#region Methods

	/// <summary>
	/// Add or update a data.
	/// </summary>
	/// <param name="data"> The data to process. </param>
	void AddOrUpdate(T data);

	/// <summary>
	/// Load data from the repository using the load predicate.
	/// </summary>
	/// <returns> The loaded data. </returns>
	IEnumerable<T> Load();

	/// <summary>
	/// Save the changes of the repository.
	/// </summary>
	void SaveChanges();

	#endregion
}