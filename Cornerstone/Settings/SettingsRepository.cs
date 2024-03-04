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
/// <typeparam name="T"> The type of the setting. </typeparam>
/// <typeparam name="T2"> The primary key of the setting. </typeparam>
public class SettingsRepository<T, T2> : ISettingsRepository<T, T2>
	where T : Setting<T2>, new()
{
	#region Fields

	private readonly IDatabase _database;
	private readonly ICollection<T> _settings;

	#endregion

	#region Constructors

	/// <summary>
	/// Instantiate a settings repository.
	/// </summary>
	/// <param name="settings"> The collection of all settings. </param>
	public SettingsRepository(ICollection<T> settings) : this(null, settings)
	{
	}

	/// <summary>
	/// Instantiate a settings repository.
	/// </summary>
	/// <param name="database"> The database the collection resides in. </param>
	/// <param name="settings"> The collection of all settings. </param>
	public SettingsRepository(IDatabase database, ICollection<T> settings)
	{
		_database = database;
		_settings = settings;
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	public void AddOrUpdate(Setting<T2> setting)
	{
		var foundSetting = _settings.FirstOrDefault(x => x.Name == setting.Name);
		if (foundSetting != null)
		{
			foundSetting.UpdateWith(setting);
			return;
		}

		foundSetting = new T();
		foundSetting.UpdateWith(setting);

		_settings.Add(foundSetting);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_database?.Dispose();
	}

	/// <inheritdoc />
	public IEnumerable<T> Load(Func<T, bool> loadPredicate)
	{
		return _settings.Where(loadPredicate);
	}

	/// <inheritdoc />
	public void SaveChanges()
	{
		_database?.SaveChanges();
	}

	#endregion
}

/// <summary>
/// Represents a settings repository.
/// </summary>
/// <typeparam name="T"> The type of the setting. </typeparam>
/// <typeparam name="T2"> The primary key of the setting. </typeparam>
public interface ISettingsRepository<out T, T2> : IDisposable
	where T : Setting<T2>
{
	#region Methods

	/// <summary>
	/// Add or update a setting.
	/// </summary>
	/// <param name="setting"> The setting to process. </param>
	void AddOrUpdate(Setting<T2> setting);

	/// <summary>
	/// Load settings from the repository using the predicate.
	/// </summary>
	/// <param name="loadPredicate"> The predicate to filter the settings. </param>
	/// <returns> The loaded settings. </returns>
	IEnumerable<T> Load(Func<T, bool> loadPredicate);

	/// <summary>
	/// Save the changes of the repository.
	/// </summary>
	void SaveChanges();

	#endregion
}