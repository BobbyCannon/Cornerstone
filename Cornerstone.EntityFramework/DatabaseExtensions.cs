#region References

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cornerstone.Reflection;
using Cornerstone.Storage;
using Cornerstone.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

#endregion

namespace Cornerstone.EntityFramework;

/// <summary>
/// Extensions for the Database class.
/// </summary>
public static class DatabaseExtensions
{
	#region Methods

	public static void AddToDatabase<T, TEntity, TKey>(this DatabaseManager<T> databaseManager, TEntity entity)
		where T : ISyncableDatabase
		where TEntity : Entity<TKey>
	{
		using var database = databaseManager.GetDatabase();
		var repository = database.GetRepository<TEntity, TKey>();
		repository.Add(entity);
		database.SaveChanges();
	}

	/// <summary>
	/// Applies any pending migrations for the context to the database.
	/// Will create the database if it does not already exist.
	/// </summary>
	/// <param name="database"> The database to process. </param>
	public static void EnsureMigrated(this IDatabase database)
	{
		if (database is EntityFrameworkDatabase efDatabase
			&& !efDatabase.IsDatabaseMigrated())
		{
			efDatabase.Database.Migrate();
		}
	}

	/// <summary>
	/// Determines the database provider type for the database.
	/// </summary>
	/// <param name="database"> The database to be tested. </param>
	/// <returns> The provider type for the database. </returns>
	public static DatabaseProviderType GetProviderType(this EntityFrameworkDatabase database)
	{
		if (database.Database.ProviderName?.EndsWith(nameof(DatabaseProviderType.Sqlite)) == true)
		{
			return DatabaseProviderType.Sqlite;
		}

		if (database.Database.ProviderName?.EndsWith(nameof(DatabaseProviderType.SqlServer)) == true)
		{
			return DatabaseProviderType.SqlServer;
		}

		return DatabaseProviderType.Unknown;
	}

	/// <summary>
	/// Validate mappings for the provided database.
	/// </summary>
	/// <param name="database"> The database to validate mappings for. </param>
	/// <returns> The list of entities with their missing properties. </returns>
	public static IDictionary<string, ICollection<string>> ValidateMappings(this IDatabase database)
	{
		var assembly = database.GetMappingAssembly();
		return ValidateMappings(assembly);
	}

	private static Dictionary<string, ICollection<string>> ValidateMappings(Assembly assembly)
	{
		var types = assembly.GetTypes();
		var mappingTypes = types.Where(x => !x.IsAbstract && x.GetInterfaces().Any(y => y == typeof(IEntityMappingConfiguration)));
		var builder = new ModelBuilder(new ConventionSet());
		var response = new Dictionary<string, ICollection<string>>();

		foreach (var config in mappingTypes
					.Select(x => SourceReflector.CreateInstance(x))
					.Cast<IEntityMappingConfiguration>())
		{
			var entityBuilder = (EntityTypeBuilder) config.Map(builder);
			var mapProperties = entityBuilder.Metadata.GetProperties();
			var ignoreProperties = entityBuilder.Metadata.GetIgnoredMembers().ToList();
			var clrTypeInfo = SourceReflector.GetRequiredSourceType(entityBuilder.Metadata.ClrType);
			var virtualProperties = clrTypeInfo.GetProperties().Where(x => x.IsVirtual).ToList();
			var entityProperties = clrTypeInfo.GetProperties().Where(x => !x.IsVirtual).ToList();
			var missingProperties = entityProperties
				.Where(x => ignoreProperties.All(v => v != x.Name))
				.Where(x => virtualProperties.All(v => v.Name != x.Name))
				.Where(x => x.CanWrite)
				.Where(x => mapProperties.All(m => m.Name != x.Name))
				.OrderBy(x => x.Name)
				.ToList();

			if (missingProperties.Count > 0)
			{
				response.Add(entityBuilder.Metadata.Name, missingProperties.Select(x => x.Name).ToList());
			}
		}

		return response;
	}

	#endregion
}