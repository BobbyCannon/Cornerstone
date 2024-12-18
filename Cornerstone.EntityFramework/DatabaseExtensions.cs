﻿#region References

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Cornerstone.Exceptions;
using Cornerstone.Extensions;
using Cornerstone.Storage;
using Cornerstone.Storage.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Index = Microsoft.EntityFrameworkCore.Metadata.Internal.Index;
#if !NET6_0_OR_GREATER
using Microsoft.EntityFrameworkCore.Metadata.Internal;
#endif

#endregion

namespace Cornerstone.EntityFramework;

/// <summary>
/// Extensions for the Database class.
/// </summary>
public static class DatabaseExtensions
{
	#region Fields

	private static readonly ConcurrentDictionary<Type, IList<IEntityMappingConfiguration>> _configurations;

	#endregion

	#region Constructors

	static DatabaseExtensions()
	{
		_configurations = new ConcurrentDictionary<Type, IList<IEntityMappingConfiguration>>();
	}

	#endregion

	#region Methods

	/// <summary>
	/// Try to configure the entity models using the EntityMappingConfiguration configurations
	/// </summary>
	/// <param name="database"> The database to process. </param>
	/// <remarks>
	/// todo: create a cached version that can be loaded?
	/// </remarks>
	public static void ConfigureModelViaMapping(this Database database)
	{
		if (database.HasBeenConfiguredViaMapping)
		{
			return;
		}

		var methods = database.GetCachedMethods(BindingFlags.Instance | BindingFlags.Public);
		var databasePropertyMethod = methods.FirstOrDefault(x => x.IsGenericMethod && (x.Name == nameof(Database.Property)) && (x.ReturnType.Name == "PropertyConfiguration`2"))
			?? throw new CornerstoneException($"Failed to find the '{nameof(Database.Property)}' method on the '{nameof(Database)}' class.");

		var databaseHasRequiredMethod = methods.FirstOrDefault(x => x.IsGenericMethod && (x.Name == nameof(Database.HasRequired)))
			?? throw new CornerstoneException($"Failed to find the '{nameof(Database.HasRequired)}' method on the '{nameof(Database)}' class.");

		var builder = new ModelBuilder(new ConventionSet());

		foreach (var config in database.GetMappingConfigurations())
		{
			var entityBuilder = (EntityTypeBuilder) config.Map(builder);
			var primaryKey = entityBuilder.Metadata.ClrType.GetPrimaryKeyType()
				?? throw new CornerstoneException($"Failed to find the 'Primary Key' type for the '{entityBuilder.Metadata.ClrType.FullName}' entity.");

			var entityProperties = entityBuilder.Metadata.GetProperties();
			var annotations = entityBuilder.Metadata.GetAnnotations();
			var indexes = entityBuilder.Metadata.GetIndexes().ToList();
			var foreignKeys = entityBuilder.Metadata.GetForeignKeys().ToList();

			foreach (var property in entityProperties)
			{
				#if NET6_0_OR_GREATER
				if (property.IsShadowProperty())
				{
					continue;
				}
				#else
				// Ignore shadow properties
				if (property.GetShadowIndex() >= 0)
				{
					continue;
				}
				#endif

				// Builds the 'Property' method
				// ex. database.Property<AddressEntity, long>(x => x.City).IsRequired().HasMaximumLength(256);
				var propertyAnnotations = property.GetAnnotations();
				var lambdaExpression = GetPropertyObjectExpression(entityBuilder.Metadata.ClrType, property.Name);
				var propertyMethod = databasePropertyMethod.GetCachedMakeGenericMethod(entityBuilder.Metadata.ClrType, primaryKey);
				var propertyConfiguration = (IPropertyConfiguration) propertyMethod.Invoke(database, [lambdaExpression]);

				if (propertyConfiguration == null)
				{
					continue;
				}

				propertyConfiguration.IsNullable = property.IsNullable;
				propertyConfiguration.MemberName = property.Name;

				foreach (var annotation in propertyAnnotations)
				{
					if ((annotation.Name == "MaxLength") && (annotation.Value != null))
					{
						propertyConfiguration.HasMaximumLength((int) annotation.Value);
					}
				}

				foreach (var mutableIndex in indexes.Where(x => x.IsUnique && x.Properties.Any(p => p.Name == property.Name)))
				{
					if (mutableIndex is not Index index)
					{
						continue;
					}

					var name = index.Builder.Metadata.FindAnnotation("Relational:Name")?.Value?.ToString() ?? index.ToString();
					var filter = index.Builder.Metadata.FindAnnotation("Relational:Filter")?.Value?.ToString()?.ToLower() ?? string.Empty;
					var indexConfiguration = database.HasIndex(entityBuilder.Metadata.ClrType, name);
					indexConfiguration.IsUnique();
					indexConfiguration.AllowNull = filter.Contains("is not null") || (propertyConfiguration.IsNullable ?? false);
					indexConfiguration.AddProperty(propertyConfiguration);
				}
			}

			// Builds relationships
			// ex. database.HasOptional<AddressEntity, long, AddressEntity, long>(x => x.LinkedAddress, x => x.LinkedAddressId, x => x.LinkedAddresses);
			//     database.HasRequired<PetEntity, PetEntity.PetKey, PersonEntity, int>(x => x.Owner, x => x.OwnerId, x => x.Owners);
			foreach (var foreignKey in foreignKeys)
			{
				if (foreignKey.DependentToPrincipal == null)
				{
					continue;
				}

				var firstParameter = GetPropertyExpression(foreignKey.DeclaringEntityType.ClrType, foreignKey.DependentToPrincipal.Name);
				var secondParameter = GetPropertyObjectExpression(foreignKey.DeclaringEntityType.ClrType, foreignKey.Properties[0].Name);
				var thirdParameter = foreignKey.PrincipalToDependent != null ? GetPropertyExpression(foreignKey.PrincipalEntityType.ClrType, foreignKey.PrincipalToDependent.Name) : null;

				// Get the types for the property configurations
				var firstType = foreignKey.DeclaringEntityType.ClrType;
				var secondType = foreignKey.DeclaringEntityType.ClrType.GetPrimaryKeyType();
				var thirdType = foreignKey.PrincipalEntityType.ClrType;
				var fourthType = foreignKey.PrincipalEntityType.ClrType.GetPrimaryKeyType();

				// Get the configuration for the property
				var method = databaseHasRequiredMethod.GetCachedMakeGenericMethod(firstType, secondType, thirdType, fourthType);
				var configuration = (IPropertyConfiguration) method.Invoke(database, [foreignKey.IsRequired, firstParameter, secondParameter, thirdParameter]);

				switch (foreignKey.DeleteBehavior)
				{
					case DeleteBehavior.ClientSetNull:
					case DeleteBehavior.SetNull:
						configuration?.OnDelete(RelationshipDeleteBehavior.SetNull);
						break;

					case DeleteBehavior.Restrict:
						configuration?.OnDelete(RelationshipDeleteBehavior.Restrict);
						break;

					case DeleteBehavior.Cascade:
						configuration?.OnDelete(RelationshipDeleteBehavior.Cascade);
						break;

					case DeleteBehavior.ClientCascade:
					case DeleteBehavior.ClientNoAction:
					case DeleteBehavior.NoAction:
					default:
						// Do nothing right now.
						break;
				}
			}
		}

		database.HasBeenConfiguredViaMapping = true;
	}

	/// <summary>
	/// Applies any pending migrations for the context to the database.
	/// Will create the database if it does not already exist.
	/// </summary>
	/// <param name="database"> The database to process. </param>
	public static void EnsureMigrated(this IDatabase database)
	{
		if (database is EntityFrameworkDatabase efDatabase)
		{
			efDatabase.Database.Migrate();
		}
	}

	/// <summary>
	/// Gets the assembly that contains the entity mappings. Base implementation defaults to the implemented types assembly.
	/// </summary>
	public static IList<IEntityMappingConfiguration> GetMappingConfigurations(this Database database)
	{
		return _configurations.GetOrAdd(database.GetType(), type =>
		{
			var assembly = database.GetMappingAssembly();
			var types = assembly.GetTypes();
			var mappingTypes = types.Where(x => !x.IsAbstract && x.GetInterfaces().Any(y => y == typeof(IEntityMappingConfiguration)));

			var response = mappingTypes
				.Select(x => x.CreateInstance())
				.Cast<IEntityMappingConfiguration>()
				.ToList();

			return response;
		});
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
	public static IDictionary<string, ICollection<string>> ValidateMappings(this Database database)
	{
		var assembly = database.GetMappingAssembly();
		return ValidateMappings(assembly);
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

	/// <summary>
	/// Get the type of the primary key.
	/// </summary>
	/// <param name="type"> The type to be tested. </param>
	/// <returns> The type for the primary key. </returns>
	private static Type GetPrimaryKeyType(this Type type)
	{
		while (true)
		{
			var baseType = type.BaseType;
			if (baseType == null)
			{
				return null;
			}

			var arg = baseType.GenericTypeArguments.FirstOrDefault();
			if (arg == null)
			{
				type = baseType;
				continue;
			}

			return arg;
		}
	}

	private static LambdaExpression GetPropertyExpression(this Type type, string name)
	{
		var parameter = Expression.Parameter(type, "x");
		var memberExpression = Expression.Property(parameter, name);
		var lambdaExpression = Expression.Lambda(memberExpression, parameter);
		return lambdaExpression;
	}

	private static LambdaExpression GetPropertyObjectExpression(this Type type, string name)
	{
		var parameter = Expression.Parameter(type, "x");
		var memberExpression = Expression.Property(parameter, name);
		var convert = Expression.Convert(memberExpression, typeof(object));
		var lambdaExpression = Expression.Lambda(convert, parameter);
		return lambdaExpression;
	}

	private static Dictionary<string, ICollection<string>> ValidateMappings(Assembly assembly)
	{
		var types = assembly.GetTypes();
		var mappingTypes = types.Where(x => !x.IsAbstract && x.GetInterfaces().Any(y => y == typeof(IEntityMappingConfiguration)));
		var builder = new ModelBuilder(new ConventionSet());
		var response = new Dictionary<string, ICollection<string>>();

		foreach (var config in mappingTypes
					.Select(x => x.CreateInstance())
					.Cast<IEntityMappingConfiguration>())
		{
			var entityBuilder = (EntityTypeBuilder) config.Map(builder);
			var mapProperties = entityBuilder.Metadata.GetProperties();
			var ignoreProperties = entityBuilder.Metadata.GetIgnoredMembers().ToList();
			var virtualProperties = entityBuilder.Metadata.ClrType.GetVirtualPropertyNames().ToList();
			var entityProperties = entityBuilder.Metadata.ClrType.GetCachedProperties().ToList();
			var missingProperties = entityProperties
				.Where(x => ignoreProperties.All(v => v != x.Name))
				.Where(x => virtualProperties.All(v => v != x.Name))
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