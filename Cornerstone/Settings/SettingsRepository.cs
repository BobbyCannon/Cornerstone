#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Settings;

/// <summary>
/// Represents a settings repository.
/// </summary>
/// <typeparam name="TSetting"> The type of the setting. </typeparam>
/// <typeparam name="TSettingKey"> The primary key of the setting. </typeparam>
public class SettingsRepository<TSetting, TSettingKey>
	: ISettingsRepository<TSetting, TSettingKey>
	where TSetting : SettingSyncEntity<TSettingKey>, new()
{
	#region Fields

	private readonly string _category;
	private readonly IDatabase _database;
	private readonly ICollection<TSetting> _settings;

	#endregion

	#region Constructors

	/// <summary>
	/// Instantiate a settings repository.
	/// </summary>
	/// <param name="category"> </param>
	/// <param name="database"> The database the collection resides in. </param>
	/// <param name="settings"> The collection of all settings. </param>
	public SettingsRepository(string category, IDatabase database, ICollection<TSetting> settings)
	{
		_category = category;
		_database = database;
		_settings = settings;
	}

	#endregion

	#region Properties

	protected virtual Func<TSetting, bool> LoadPredicate => x => x.Category == _category;

	protected virtual Func<TSetting, TSetting, bool> LookupPredicate => (x, y) => (x.Category == y.Category) && string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);

	#endregion

	#region Methods

	public void Add(TSetting data)
	{
		_settings.Add(data);
	}

	public void Dispose()
	{
		_database?.Dispose();
		GC.SuppressFinalize(this);
	}

	public TSetting Get(Func<TSetting, bool> predicate)
	{
		return _settings.FirstOrDefault(predicate);
	}

	public IEnumerable<TSetting> Load()
	{
		return _settings.Where(LoadPredicate);
	}

	public void SaveChanges()
	{
		_database?.SaveChanges();
	}

	#endregion
}

/// <summary>
/// Represents a settings repository.
/// </summary>
/// <typeparam name="TSetting"> The type of the setting. </typeparam>
/// <typeparam name="TSettingKey"> The primary key of the setting. </typeparam>
public interface ISettingsRepository<TSetting, TSettingKey> : IDisposable
	where TSetting : SettingSyncEntity<TSettingKey>
{
	#region Methods

	/// <summary>
	/// Add a setting
	/// </summary>
	/// <param name="data"> The data to process. </param>
	void Add(TSetting data);

	/// <summary>
	/// Get an item with the provided predicate.
	/// </summary>
	/// <param name="predicate"> The filter to locate the item. </param>
	/// <returns> The found item otherwise null. </returns>
	TSetting Get(Func<TSetting, bool> predicate);

	/// <summary>
	/// Load data from the repository using the load predicate.
	/// </summary>
	/// <returns> The loaded data. </returns>
	IEnumerable<TSetting> Load();

	/// <summary>
	/// Save the changes of the repository.
	/// </summary>
	void SaveChanges();

	#endregion
}