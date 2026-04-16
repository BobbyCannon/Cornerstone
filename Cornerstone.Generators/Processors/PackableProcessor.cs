#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Generators.Models;
using Microsoft.CodeAnalysis;
using static Cornerstone.Generators.Generator;
using SourceTypeInfo = Cornerstone.Generators.Models.SourceTypeInfo;

#endregion

namespace Cornerstone.Generators.Processors;

internal sealed class PackableProcessor : ITypeProcessor
{
	#region Fields

	private INamedTypeSymbol[] _typeSymbolsForCollectionDefinitions;

	#endregion

	#region Properties

	public bool EmitsInsideTypeBlock => true;

	#endregion

	#region Methods

	/// <summary>
	/// Called once per generation pass with the current compilation so
	/// the processor can resolve the collection type symbols it needs.
	/// </summary>
	internal void Initialize(Compilation compilation)
	{
		_typeSymbolsForCollectionDefinitions = new[]
		{
			compilation.GetTypeByMetadataName("Cornerstone.Collections.SpeedyList`1"),
			compilation.GetTypeByMetadataName("Cornerstone.Collections.ISpeedyList`1"),
			compilation.GetTypeByMetadataName("System.Array"),
			compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1"),
			compilation.GetTypeByMetadataName("System.Collections.Generic.Collection`1"),
			compilation.GetTypeByMetadataName("System.Collections.Generic.ICollection`1"),
			compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyCollection`1"),
			compilation.GetTypeByMetadataName("System.Collections.Generic.List`1"),
			compilation.GetTypeByMetadataName("System.Collections.Generic.IList`1")
		}.Where(s => s != null).ToArray();
	}

	public void Process(CSharpCodeBuilder builder, SourceTypeInfo typeInfo)
	{
		var properties = GetAllMembers(typeInfo.TypeSymbol)
			.OfType<IPropertySymbol>()
			.GroupBy(x => x.Name)
			.Select(x => x.First())
			.OrderBy(x => x.Name)
			.ToArray();

		var classPackableByVersions = new Dictionary<byte, string[]>();

		if ((properties.Length <= 0)
			&& (classPackableByVersions.Count == 0))
		{
			return;
		}

		var packableAttributes = typeInfo.Attributes
			.Where(x => x.Name == NamePackableAttribute)
			.ToArray();

		foreach (var attr in packableAttributes)
		{
			var packVersion = (byte) attr.ConstructorArguments[0];
			var packProperties = ((object[]) attr.ConstructorArguments[1])
				.Select(x => (string) x)
				.ToArray();

			if ((packProperties.Length == 1) && (packProperties[0] == "*"))
			{
				classPackableByVersions[packVersion] = properties
					.Where(x => !x.IsWriteOnly && !x.IsReadOnly)
					.Select(x => x.Name)
					.ToArray();
			}
			else
			{
				classPackableByVersions[packVersion] = packProperties;
			}
		}

		var propertyAttributes = typeInfo
			.Properties
			.SelectMany(x => x.Attributes
				.Where(a => a.Name == NamePackAttribute)
				.Select(a => (
					Version: (byte) a.ConstructorArguments[0]!,
					Offset: (byte) a.ConstructorArguments[1]!,
					Property: x.PropertySymbol
				)))
			.ToArray();

		var distinctVersions = propertyAttributes
			.Select(x => x.Version)
			.Concat(classPackableByVersions.Keys)
			.Distinct()
			.OrderBy(v => v)
			.ToArray();

		var versionGroups = new Dictionary<int, (byte Version, byte Offset, IPropertySymbol Property)[]>();

		foreach (var targetVersion in distinctVersions)
		{
			var group = new List<(byte Version, byte Offset, IPropertySymbol Property)>();

			if (classPackableByVersions.TryGetValue(targetVersion, out var orderedPropertyNames))
			{
				for (byte i = 0; i < orderedPropertyNames.Length; i++)
				{
					var propName = orderedPropertyNames[i];
					var propertyInfo = properties.FirstOrDefault(p => p.Name == propName);

					if (propertyInfo != null)
					{
						group.Add((
							Version: targetVersion,
							Offset: (byte) (i + 1),
							Property: propertyInfo
						));
					}
				}
			}
			else
			{
				var active = propertyAttributes
					.Where(x => x.Version == targetVersion)
					.OrderBy(x => x.Offset)
					.Select(x => (x.Version, x.Offset, x.Property))
					.ToArray();

				group.AddRange(active);
			}

			versionGroups[targetVersion] = group.ToArray();
		}

		if (versionGroups.Count == 0)
		{
			return;
		}

		var currentVersion = distinctVersions.Max();
		var method = ImplementsMethodRecursively(typeInfo.TypeSymbol, "ToSpeedyPacket");
		var needsOverride = RequiresOverride(method);

		// ToSpeedyPacket
		builder.IndentWrite("public ");
		builder.Write(needsOverride ? "override " : "virtual ");
		builder.WriteLine("global::Cornerstone.Serialization.SpeedyPacket ToSpeedyPacket()");
		builder.IndentWriteLine("{");
		builder.Indent++;

		var currentProperties = versionGroups[currentVersion];
		var propertiesList = string.Join(", ", currentProperties.Select(x => x.Property.Name));

		builder.IndentWriteLine($"return new global::Cornerstone.Serialization.SpeedyPacket([(byte) {currentVersion}, {propertiesList}]);");

		builder.Indent--;
		builder.IndentWriteLine("}");

		// FromSpeedyPacket
		builder.IndentWrite("public ");
		builder.Write(needsOverride ? "override " : "virtual ");
		builder.WriteLine("void FromSpeedyPacket(global::Cornerstone.Serialization.SpeedyPacket values)");
		builder.IndentWriteLine("{");
		builder.Indent++;

		builder.IndentWriteLine("var version = (byte) values[0];");

		foreach (var group in versionGroups.OrderBy(kv => kv.Key))
		{
			builder.IndentWriteLine($"if (version == {group.Key})");
			builder.IndentWriteLine("{");
			builder.Indent++;

			foreach (var v in group.Value)
			{
				var propertyType = v.Property.Type.ToDisplayString(SymbolDisplayFormats.FullyQualifiedName);
				var propertyName = v.Property.Name;

				if (TryGetCollectionElementType(v.Property.Type, out var elementType)
					&& elementType.AllInterfaces.Any(i => i.Name == "IPackable"))
				{
					builder.IndentWriteLine($"if ({propertyName} != null)");
					builder.IndentWriteLine("{");
					builder.Indent++;
					builder.IndentWriteLine($"{propertyName}.Load([..((global::Cornerstone.Serialization.SpeedyPacket)values[{v.Offset}]).ToArray<{elementType}>()]);");
					builder.Indent--;
					builder.IndentWriteLine("}");

					if (v.Property.SetMethod != null)
					{
						builder.IndentWriteLine("else");
						builder.IndentWriteLine("{");
						builder.Indent++;
						builder.IndentWriteLine($"{propertyName} = [..((global::Cornerstone.Serialization.SpeedyPacket)values[{v.Offset}]).ToArray<{elementType}>()];");
						builder.Indent--;
						builder.IndentWriteLine("}");
					}
				}
				else if (v.Property.Type.AllInterfaces.Any(x => x.Name == "IPackable"))
				{
					builder.IndentWriteLine($"{propertyName} = ((global::Cornerstone.Serialization.SpeedyPacket)values[{v.Offset}]).Unpack<{propertyType}>();");
				}
				else
				{
					builder.IndentWriteLine($"{propertyName} = ({propertyType}) values[{v.Offset}];");
				}
			}

			builder.Indent--;
			builder.IndentWriteLine("}");
		}

		builder.Indent--;
		builder.IndentWriteLine("}");
	}

	/// <summary>
	/// Determines if the given type is a supported collection type and, if so,
	/// returns the element type (T) via the out parameter.
	/// </summary>
	private bool TryGetCollectionElementType(ITypeSymbol type, out ITypeSymbol elementType)
	{
		elementType = null;

		switch (type)
		{
			case IArrayTypeSymbol arrayType:
			{
				elementType = arrayType.ElementType;
				return true;
			}
			case INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } namedType:
			{
				var allCandidates = namedType.AllInterfaces.Prepend(namedType).Where(t => t.IsGenericType && (t.TypeArguments.Length == 1));

				foreach (var candidate in allCandidates)
				{
					var originalDef = candidate.OriginalDefinition;

					if (_typeSymbolsForCollectionDefinitions.Any(def =>
							(def != null) && SymbolEqualityComparer.Default.Equals(originalDef, def)))
					{
						elementType = candidate.TypeArguments[0];
						return true;
					}
				}
				break;
			}
		}

		return false;
	}

	#endregion
}