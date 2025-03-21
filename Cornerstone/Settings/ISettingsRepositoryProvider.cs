namespace Cornerstone.Settings;

/// <summary>
/// Represents a settings repository.
/// </summary>
/// <typeparam name="T"> The type of the setting. </typeparam>
/// <typeparam name="T2"> The primary key of the setting. </typeparam>
public interface ISettingsRepositoryProvider<T, T2>
	where T : Setting<T2>
{
	#region Methods

	public ISettingsRepository<T, T2> GetSettingsRepository(string category);

	#endregion
}