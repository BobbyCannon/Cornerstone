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

	protected readonly IDateTimeProvider DateTimeProvider;

	#endregion

	#region Constructors

	protected DatabaseProvider(IDateTimeProvider dateTimeProvider)
	{
		DateTimeProvider = dateTimeProvider;
	}

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
		var response = GetDatabaseFromProvider();
		response.UpdateDateTimeProvider(DateTimeProvider);
		return response;
	}

	/// <summary>
	/// Gets an instance of the database from the provider.
	/// </summary>
	/// <returns> The database instance. </returns>
	protected abstract T GetDatabaseFromProvider();

	/// <inheritdoc />
	IDatabase IDatabaseProvider.GetDatabase()
	{
		return GetDatabase();
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

	#endregion
}

/// <summary>
/// Represents a database provider for syncable databases.
/// </summary>
public interface IDatabaseProvider
{
	#region Methods

	/// <summary>
	/// Gets an instance of the database.
	/// </summary>
	/// <returns> The database instance. </returns>
	IDatabase GetDatabase();

	#endregion
}