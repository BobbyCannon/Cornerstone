namespace Cornerstone.Storage;

/// <summary>
/// Represents a database provider for syncable databases.
/// </summary>
public abstract class DatabaseProvider<T> : IDatabaseProvider<T> where T : IDatabase
{
	#region Constructors

	/// <summary>
	/// Initialize the database provider.
	/// </summary>
	/// <param name="options"> The database options. </param>
	protected DatabaseProvider(DatabaseOptions options)
	{
		Options = options ?? new DatabaseOptions();
	}

	#endregion

	#region Properties

	/// <inheritdoc />
	public DatabaseOptions Options { get; set; }

	#endregion

	#region Methods

	/// <summary>
	/// Add the entity to the database.
	/// </summary>
	/// <typeparam name="TEntity"> The entity type. </typeparam>
	/// <typeparam name="TKey"> The entity primary key type. </typeparam>
	/// <param name="entity"> The entity to be added. </param>
	public TEntity AddToDatabase<TEntity, TKey>(TEntity entity) where TEntity : Entity<TKey>
	{
		using var database = GetDatabase();
		var r = database.GetRepository<TEntity, TKey>();
		r.Add(entity);
		database.SaveChanges();
		return entity;
	}

	/// <inheritdoc />
	public T GetDatabase()
	{
		return GetDatabaseFromProvider(Options.DeepClone());
	}

	/// <inheritdoc />
	public T GetDatabase(DatabaseOptions options)
	{
		return GetDatabaseFromProvider(options);
	}

	/// <summary>
	/// Gets an instance of the database from the provider.
	/// </summary>
	/// <param name="options"> The database options to use for the new database instance. </param>
	/// <returns> The database instance. </returns>
	protected abstract T GetDatabaseFromProvider(DatabaseOptions options);

	/// <inheritdoc />
	IDatabase IDatabaseProvider.GetDatabase()
	{
		return GetDatabase(Options.DeepClone());
	}

	/// <inheritdoc />
	IDatabase IDatabaseProvider.GetDatabase(DatabaseOptions options)
	{
		return GetDatabase(options);
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
	/// <param name="options"> The database options to use for the new database instance. </param>
	/// <returns> The database instance. </returns>
	new T GetDatabase(DatabaseOptions options);

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
	DatabaseOptions Options { get; set; }

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
	/// <param name="options"> The database options to use for the new database instance. </param>
	/// <returns> The database instance. </returns>
	IDatabase GetDatabase(DatabaseOptions options);

	#endregion
}