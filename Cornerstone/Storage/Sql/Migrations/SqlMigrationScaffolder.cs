#region References

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Storage.Sql.Migrations;

/// <summary>
/// Writes migration, designer snapshot, model snapshot, and GetMigrations register files.
/// Design-time only (PowerShell / tests). Not used at runtime under Native AOT.
/// </summary>
public static class SqlMigrationScaffolder
{
	#region Fields

	private static readonly Regex _idRegex;
	private static readonly Regex _migrationClassRegex;

	#endregion

	#region Constructors

	static SqlMigrationScaffolder()
	{
		_idRegex = new Regex(@"Id\s*=>\s*""([^""]+)""", RegexOptions.Compiled);
		_migrationClassRegex = new Regex(@"public class (\w+)\s*:\s*SqlMigration", RegexOptions.Compiled);
	}

	#endregion

	#region Methods

	public static SqlMigrationScaffoldResult Add(SqlMigrationScaffolderOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		ValidateAddOptions(options);

		var entityTypes = options.EntityTypes;
		var current = SchemaSnapshot.Capture(entityTypes);
		var previous = options.PreviousSnapshot ?? LoadModelSnapshot(options.DatabaseType) ?? new SchemaSnapshot();
		var diff = SchemaDiffer.Diff(previous, current);
		if (!diff.HasChanges)
		{
			throw new InvalidOperationException("No schema changes to migrate.");
		}

		var className = ToClassName(options.Name);
		var timestamp = (options.DateTimeProvider ?? DateTimeProvider.RealTime).UtcNow.ToString("yyyyMMddHHmmss");
		var migrationId = timestamp + "_" + className;
		var tableMap = BuildTableMap(entityTypes);
		var migrationsNamespace = options.MigrationsNamespace
			?? options.DatabaseType.Namespace + ".Migrations";
		var outputDirectory = options.OutputDirectory;
		Directory.CreateDirectory(outputDirectory);

		var existing = ListMigrationClasses(outputDirectory);
		if (existing.Any(x => string.Equals(x.ClassName, className, StringComparison.OrdinalIgnoreCase)))
		{
			throw new InvalidOperationException($"A migration named '{className}' already exists.");
		}

		var migrationPath = Path.Combine(outputDirectory, migrationId + ".cs");
		var designerPath = Path.Combine(outputDirectory, migrationId + ".Designer.cs");
		var snapshotClassName = "Snapshot_" + migrationId;
		var modelSnapshotClassName = options.DatabaseType.Name + "ModelSnapshot";
		var modelSnapshotPath = Path.Combine(outputDirectory, modelSnapshotClassName + ".cs");
		var registerPath = Path.Combine(outputDirectory, options.DatabaseType.Name + ".Migrations.cs");

		File.WriteAllText(migrationPath, SqlMigrationCodeGenerator.WriteMigration(
			migrationsNamespace, className, migrationId, diff, tableMap));
		File.WriteAllText(designerPath, current.ToCSharp(migrationsNamespace, snapshotClassName));
		File.WriteAllText(modelSnapshotPath, SqlMigrationCodeGenerator.WriteSnapshotSubclass(
			migrationsNamespace, modelSnapshotClassName, snapshotClassName));

		var classNames = existing.Select(x => x.ClassName).ToList();
		classNames.Add(className);
		File.WriteAllText(registerPath, SqlMigrationCodeGenerator.WriteRegister(
			options.DatabaseType.Namespace,
			options.DatabaseType.Name,
			migrationsNamespace,
			classNames));

		var result = new SqlMigrationScaffoldResult();
		result.MigrationId = migrationId;
		result.ClassName = className;
		result.MigrationPath = migrationPath;
		result.DesignerPath = designerPath;
		result.ModelSnapshotPath = modelSnapshotPath;
		result.RegisterPath = registerPath;
		return result;
	}

	public static SqlMigrationScaffoldResult Remove(SqlMigrationScaffolderOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		if (string.IsNullOrWhiteSpace(options.OutputDirectory))
		{
			throw new ArgumentException("OutputDirectory is required.", nameof(options));
		}

		if (options.DatabaseType == null)
		{
			throw new ArgumentException("DatabaseType is required.", nameof(options));
		}

		var outputDirectory = options.OutputDirectory;
		var existing = ListMigrationClasses(outputDirectory);
		if (existing.Count == 0)
		{
			throw new InvalidOperationException("There are no migrations to remove.");
		}

		var last = existing[existing.Count - 1];
		var applied = options.AppliedMigrationIds ?? [];
		if (!options.Force && applied.Any(x => string.Equals(x, last.Id, StringComparison.OrdinalIgnoreCase)))
		{
			throw new InvalidOperationException(
				$"Cannot remove '{last.Id}' because it is recorded as applied. Use Force to override.");
		}

		var migrationPath = Path.Combine(outputDirectory, last.Id + ".cs");
		if (!File.Exists(migrationPath))
		{
			migrationPath = Path.Combine(outputDirectory, last.ClassName + ".cs");
		}

		var designerPath = Path.Combine(outputDirectory, last.Id + ".Designer.cs");
		if (File.Exists(migrationPath))
		{
			File.Delete(migrationPath);
		}

		if (File.Exists(designerPath))
		{
			File.Delete(designerPath);
		}

		var modelSnapshotClassName = options.DatabaseType.Name + "ModelSnapshot";
		var modelSnapshotPath = Path.Combine(outputDirectory, modelSnapshotClassName + ".cs");
		var remaining = existing.Take(existing.Count - 1).ToList();
		var migrationsNamespace = options.MigrationsNamespace
			?? options.DatabaseType.Namespace + ".Migrations";

		if (remaining.Count == 0)
		{
			if (File.Exists(modelSnapshotPath))
			{
				File.Delete(modelSnapshotPath);
			}
		}
		else
		{
			var previousDesigner = Path.Combine(outputDirectory, remaining[remaining.Count - 1].Id + ".Designer.cs");
			if (!File.Exists(previousDesigner))
			{
				throw new InvalidOperationException(
					$"Cannot restore the model snapshot; missing '{previousDesigner}'.");
			}

			File.WriteAllText(modelSnapshotPath, SqlMigrationCodeGenerator.WriteSnapshotSubclass(
				migrationsNamespace,
				modelSnapshotClassName,
				"Snapshot_" + remaining[remaining.Count - 1].Id));
		}

		var registerPath = Path.Combine(outputDirectory, options.DatabaseType.Name + ".Migrations.cs");
		File.WriteAllText(registerPath, SqlMigrationCodeGenerator.WriteRegister(
			options.DatabaseType.Namespace,
			options.DatabaseType.Name,
			migrationsNamespace,
			remaining.Select(x => x.ClassName).ToList()));

		var result = new SqlMigrationScaffoldResult();
		result.MigrationId = last.Id;
		result.ClassName = last.ClassName;
		result.MigrationPath = migrationPath;
		result.DesignerPath = designerPath;
		result.ModelSnapshotPath = modelSnapshotPath;
		result.RegisterPath = registerPath;
		return result;
	}

	internal static string ToClassName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("Migration name is required.", nameof(name));
		}

		var builder = new StringBuilder();
		var upperNext = true;
		foreach (var c in name)
		{
			if (char.IsLetterOrDigit(c))
			{
				builder.Append(upperNext ? char.ToUpperInvariant(c) : c);
				upperNext = false;
			}
			else
			{
				upperNext = true;
			}
		}

		if (builder.Length == 0)
		{
			throw new ArgumentException("Migration name must contain letters or digits.", nameof(name));
		}

		if (char.IsDigit(builder[0]))
		{
			builder.Insert(0, 'M');
		}

		return builder.ToString();
	}

	private static Dictionary<string, Type> BuildTableMap(IReadOnlyList<Type> entityTypes)
	{
		var map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
		foreach (var type in entityTypes)
		{
			var table = SqlGenerator.GetExpectedTableInfo(type);
			map[table.Name] = type;
		}

		return map;
	}

	private static List<(string Id, string ClassName)> ListMigrationClasses(string outputDirectory)
	{
		var response = new List<(string Id, string ClassName)>();
		if (!Directory.Exists(outputDirectory))
		{
			return response;
		}

		foreach (var path in Directory.GetFiles(outputDirectory, "*.cs"))
		{
			if (path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
				|| path.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase)
				|| path.EndsWith(".Migrations.cs", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var text = File.ReadAllText(path);
			var classMatch = _migrationClassRegex.Match(text);
			var idMatch = _idRegex.Match(text);
			if (classMatch.Success && idMatch.Success)
			{
				response.Add((idMatch.Groups[1].Value, classMatch.Groups[1].Value));
			}
		}

		return response
			.OrderBy(x => x.Id, StringComparer.Ordinal)
			.ToList();
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Scaffolder is design-time and instantiates the compiled model snapshot.")]
	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Snapshot types are compiled into the host assembly.")]
	private static SchemaSnapshot LoadModelSnapshot(Type databaseType)
	{
		if (databaseType == null)
		{
			return null;
		}

		var expectedName = databaseType.Name + "ModelSnapshot";
		var snapshotType = databaseType.Assembly
			.GetTypes()
			.FirstOrDefault(x =>
				(x.Name == expectedName)
				&& typeof(SchemaSnapshot).IsAssignableFrom(x)
				&& (x != typeof(SchemaSnapshot)));
		if (snapshotType == null)
		{
			return null;
		}

		return (SchemaSnapshot) Activator.CreateInstance(snapshotType);
	}

	private static void ValidateAddOptions(SqlMigrationScaffolderOptions options)
	{
		if (options.DatabaseType == null)
		{
			throw new ArgumentException("DatabaseType is required.", nameof(options));
		}

		if ((options.EntityTypes == null) || (options.EntityTypes.Count == 0))
		{
			throw new ArgumentException("EntityTypes is required.", nameof(options));
		}

		if (string.IsNullOrWhiteSpace(options.OutputDirectory))
		{
			throw new ArgumentException("OutputDirectory is required.", nameof(options));
		}

		if (string.IsNullOrWhiteSpace(options.Name))
		{
			throw new ArgumentException("Name is required.", nameof(options));
		}
	}

	#endregion
}