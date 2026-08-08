#region References

using System;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Settings;

/// <summary>
/// Represents a settings repository.
/// </summary>
/// <typeparam name="TSetting"> The type of the setting. </typeparam>
/// <typeparam name="TSettingKey"> The primary key of the setting. </typeparam>
/// <typeparam name="TDatabase"> The type of the database. </typeparam>
public interface ISettingsRepositoryProvider<TSetting, TSettingKey, out TDatabase>
	: IDatabaseProvider<TDatabase>
	where TSetting : SettingSyncEntity<TSettingKey>
	where TDatabase : IDatabase
{
	#region Methods

	public ISettingsRepository<TSetting, TSettingKey> GetSettingsRepository(string category);

	#endregion
}