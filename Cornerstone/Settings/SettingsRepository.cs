#region References

using System;
using System.Collections.Generic;
using Cornerstone.Collections;
using Cornerstone.Storage;

#endregion

namespace Cornerstone.Settings;

/// <summary>
/// Represents a settings repository.
/// </summary>
/// <typeparam name="T"> The type of the setting. </typeparam>
/// <typeparam name="T2"> The primary key of the setting. </typeparam>
public class SettingsRepository<T, T2>
	: DataRepository<T>, ISettingsRepository<T, T2>
	where T : Setting<T2>, new()
{
	#region Fields

	private readonly string _category;

	#endregion

	#region Constructors

	/// <summary>
	/// Instantiate a settings repository.
	/// </summary>
	/// <param name="category"> </param>
	/// <param name="database"> The database the collection resides in. </param>
	/// <param name="settings"> The collection of all settings. </param>
	public SettingsRepository(string category, IDatabase database, ICollection<T> settings)
		: base(database, settings)
	{
		_category = category;
	}

	#endregion

	#region Properties

	protected override Func<T, bool> LoadPredicate => x => x.Category == _category;

	protected override Func<T, T, bool> LookupPredicate => (x, y) => string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);

	#endregion
}

/// <summary>
/// Represents a settings repository.
/// </summary>
/// <typeparam name="T"> The type of the setting. </typeparam>
/// <typeparam name="T2"> The primary key of the setting. </typeparam>
public interface ISettingsRepository<T, T2>
	: IDataRepository<T>
	where T : Setting<T2>
{
}