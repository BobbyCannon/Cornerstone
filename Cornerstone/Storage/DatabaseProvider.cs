#region References

using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Storage;

/// <summary>
/// Represents a database provider for syncable databases.
/// </summary>
public abstract class DatabaseProvider<T> : IDatabaseProvider<T> where T : IDatabase
{
	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;

	#endregion

	#region Constructors

	protected DatabaseProvider(IDateTimeProvider dateTimeProvider, DatabaseSettings settings)
	{
		_dateTimeProvider = dateTimeProvider;

		Settings = settings;
	}

	#endregion

	#region Properties

	public DatabaseSettings Settings { get; set; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public T GetDatabase()
	{
		var response = GetDatabaseFromProvider(Settings, null);
		response.UpdateDateTimeProvider(_dateTimeProvider);
		return response;
	}

	/// <inheritdoc />
	public T GetDatabase(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		var response = GetDatabaseFromProvider(settings, keyCache);
		response.UpdateDateTimeProvider(_dateTimeProvider);
		return response;
	}

	/// <summary>
	/// Gets an instance of the database from the provider.
	/// </summary>
	/// <returns> The database instance. </returns>
	protected abstract T GetDatabaseFromProvider();

	/// <summary>
	/// Gets an instance of the database from the provider.
	/// </summary>
	/// <returns> The database instance. </returns>
	protected abstract T GetDatabaseFromProvider(DatabaseSettings settings, DatabaseKeyCache keyCache);

	/// <inheritdoc />
	IDatabase IDatabaseProvider.GetDatabase()
	{
		return GetDatabase();
	}

	/// <inheritdoc />
	IDatabase IDatabaseProvider.GetDatabase(DatabaseSettings settings, DatabaseKeyCache keyCache)
	{
		return GetDatabase(settings, keyCache);
	}

	#endregion
}

/// <summary>
/// Represents a database provider for syncable databases.
/// </summary>
public interface IDatabaseProvider<out T> : IDatabaseProvider
	where T : IDatabase
{
	#region Methods

	/// <summary>
	/// Gets an instance of the database.
	/// </summary>
	/// <returns> The database instance. </returns>
	new T GetDatabase();

	/// <summary>
	/// Gets an instance of the database.
	/// </summary>
	/// <returns> The database instance. </returns>
	new T GetDatabase(DatabaseSettings settings, DatabaseKeyCache keyCache);

	#endregion
}

/// <summary>
/// Represents a database provider for syncable databases.
/// </summary>
public interface IDatabaseProvider
{
	#region Properties

	/// <summary>
	/// Gets or sets the options for the database provider.
	/// </summary>
	DatabaseSettings Settings { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Gets an instance of the database.
	/// </summary>
	/// <returns> The database instance. </returns>
	IDatabase GetDatabase();

	/// <summary>
	/// Gets an instance of the database.
	/// </summary>
	/// <returns> The database instance. </returns>
	IDatabase GetDatabase(DatabaseSettings settings, DatabaseKeyCache keyCache);

	#endregion
}