#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Storage.Sql.Migrations;

/// <summary>
/// Design-time helpers for PowerShell cmdlets: load a host database type, entity types, and construct a database.
/// </summary>
public static class SqlMigrationHost
{
	#region Fields

	private static string _assemblyResolveDirectory;
	private static bool _assemblyResolveHooked;

	#endregion

	#region Constructors

	static SqlMigrationHost()
	{
		_assemblyResolveDirectory = null;
		_assemblyResolveHooked = false;
	}

	#endregion

	#region Methods

	[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Design-time cmdlets construct host database types by known constructor shapes.")]
	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Design-time cmdlets construct host database types by known constructor shapes.")]
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Design-time Activator is not used under Native AOT.")]
	public static SqlDatabase CreateDatabase(Type databaseType, string connectionString, SqlProvider provider)
	{
		ArgumentNullException.ThrowIfNull(databaseType);
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			throw new ArgumentException("ConnectionString is required.", nameof(connectionString));
		}

		if (!typeof(SqlDatabase).IsAssignableFrom(databaseType))
		{
			throw new InvalidOperationException($"Type '{databaseType.FullName}' is not a SqlDatabase.");
		}

		var settingsCtor = databaseType.GetConstructor(
		[
			typeof(string),
			typeof(SqlProvider),
			typeof(DatabaseSettings),
			typeof(DatabaseKeyCache),
			typeof(IDateTimeProvider)
		]);
		if (settingsCtor != null)
		{
			return (SqlDatabase) settingsCtor.Invoke(
			[
				connectionString,
				provider,
				null,
				null,
				DateTimeProvider.RealTime
			]);
		}

		var simpleCtor = databaseType.GetConstructor([typeof(string), typeof(SqlProvider)]);
		if (simpleCtor != null)
		{
			return (SqlDatabase) simpleCtor.Invoke([connectionString, provider]);
		}

		throw new InvalidOperationException(
			$"Type '{databaseType.FullName}' has no constructor (string, SqlProvider) or (string, SqlProvider, DatabaseSettings, DatabaseKeyCache, IDateTimeProvider).");
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Design-time assembly load for scaffolding.")]
	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Design-time type resolution for scaffolding.")]
	public static Type ResolveDatabaseType(string assemblyPath, string databaseTypeName)
	{
		if (string.IsNullOrWhiteSpace(databaseTypeName))
		{
			throw new ArgumentException("DatabaseType is required.", nameof(databaseTypeName));
		}

		if (!string.IsNullOrWhiteSpace(assemblyPath))
		{
			var fullPath = Path.GetFullPath(assemblyPath);
			if (!File.Exists(fullPath))
			{
				throw new FileNotFoundException($"Assembly not found: {fullPath}", fullPath);
			}

			HookAssemblyResolve(Path.GetDirectoryName(fullPath));
			var assembly = Assembly.LoadFrom(fullPath);
			return FindType(GetLoadableTypes(assembly), databaseTypeName)
				?? throw new InvalidOperationException($"Type '{databaseTypeName}' was not found in '{fullPath}'.");
		}

		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			var match = FindType(GetLoadableTypes(assembly), databaseTypeName);
			if (match != null)
			{
				return match;
			}
		}

		var loaded = Type.GetType(databaseTypeName);
		if (loaded != null)
		{
			return loaded;
		}

		throw new InvalidOperationException(
			$"Type '{databaseTypeName}' was not found. Pass -Assembly to load the host dll.");
	}

	[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Design-time looks up GetEntityTypes on the host database type.")]
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Design-time looks up GetEntityTypes on host interfaces.")]
	public static Type[] ResolveEntityTypes(Type databaseType)
	{
		ArgumentNullException.ThrowIfNull(databaseType);

		var method = FindEntityTypesMethod(databaseType);
		if (method == null)
		{
			foreach (var iface in databaseType.GetInterfaces())
			{
				method = FindEntityTypesMethod(iface);
				if (method != null)
				{
					break;
				}
			}
		}

		if (method == null)
		{
			throw new InvalidOperationException(
				$"Type '{databaseType.FullName}' has no static GetEntityTypes() method. Pass entity types or add GetEntityTypes to the database contract.");
		}

		var value = method.Invoke(null, null);
		if (value is Type[] types)
		{
			return types;
		}

		if (value is IEnumerable<Type> enumerable)
		{
			return enumerable.ToArray();
		}

		throw new InvalidOperationException("GetEntityTypes() did not return Type[].");
	}

	[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Design-time looks up GetEntityTypes on the host database type.")]
	private static MethodInfo FindEntityTypesMethod(Type type)
	{
		return type.GetMethod("GetEntityTypes", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy, null, Type.EmptyTypes, null);
	}

	private static Type FindType(IEnumerable<Type> types, string databaseTypeName)
	{
		var matches = types
			.Where(x =>
				string.Equals(x.FullName, databaseTypeName, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(x.Name, databaseTypeName, StringComparison.OrdinalIgnoreCase))
			.ToList();
		if (matches.Count == 1)
		{
			return matches[0];
		}

		if (matches.Count > 1)
		{
			throw new InvalidOperationException(
				$"Type name '{databaseTypeName}' is ambiguous. Use the full type name.");
		}

		return null;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Design-time assembly scan for scaffolding cmdlets.")]
	private static Type[] GetLoadableTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			return ex.Types.Where(x => x != null).ToArray();
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Design-time cmdlets load the host output directory.")]
	private static void HookAssemblyResolve(string directory)
	{
		if (string.IsNullOrWhiteSpace(directory))
		{
			return;
		}

		_assemblyResolveDirectory = directory;
		if (_assemblyResolveHooked)
		{
			return;
		}

		_assemblyResolveHooked = true;
		AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
		{
			var name = new AssemblyName(args.Name).Name;
			if (string.IsNullOrWhiteSpace(name) || name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			var candidate = Path.Combine(_assemblyResolveDirectory, name + ".dll");
			return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
		};
	}

	#endregion
}