#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Generators.Models;
using Microsoft.CodeAnalysis;

#endregion

namespace Cornerstone.Generators;

public partial class Generator
{
	#region Methods

	private void ProcessPackable(CSharpCodeBuilder builder, SourceTypeInfo typeInfo)
	{
		var packableProperties = typeInfo
			.Properties.Where(x => x.Attributes.Any(a => a.Name == NamePackAttribute)).ToArray();

		if (packableProperties.Length <= 0)
		{
			return;
		}

		var allPackEntries = packableProperties.SelectMany(x => x
			.Attributes.Where(a => a.Name == NamePackAttribute).Select(a =>
				(
					Version: (byte) a.ConstructorArguments[0]!,
					Offset: (byte) a.ConstructorArguments[1]!,
					Property: x.PropertySymbol
				)
			)).OrderBy(x => x.Version).ThenBy(x => x.Offset).ToArray();

		// Find all distinct versions that appear (sorted)
		var distinctVersions = allPackEntries.Select(x => x.Version).Distinct().OrderBy(v => v).ToArray();

		// Now create cumulative groups: for each version, all properties <= that version
		var versionGroups = new Dictionary<int, (byte Version, byte Offset, IPropertySymbol Property)[]>();

		foreach (var targetVersion in distinctVersions)
		{
			var active = allPackEntries.Where(x => x.Version <= targetVersion).OrderBy(x => x.Offset).ToArray();

			versionGroups[targetVersion] = active;
		}

		var currentVersion = distinctVersions.Max();
		var method = ImplementsMethodRecursively(typeInfo.TypeSymbol, "ToSpeedyPacket");
		var needsOverride = RequiresOverride(method);

		builder.IndentWrite("public ");
		builder.Write(needsOverride ? "override " : "virtual ");
		builder.WriteLine("global::Cornerstone.Serialization.SpeedyPacket ToSpeedyPacket()");
		builder.IndentWriteLine("{");
		builder.Indent++;
		var currentProperties = versionGroups[currentVersion];
		var properties = string.Join(",", currentProperties.Select(x => x.Property.Name));
		builder.IndentWriteLine($"return new global::Cornerstone.Serialization.SpeedyPacket([(byte){currentVersion},{properties}]);");
		builder.Indent--;
		builder.IndentWriteLine("}");

		builder.IndentWrite("public ");
		builder.Write(needsOverride ? "override " : "virtual ");
		builder.WriteLine("void FromSpeedyPacket(global::Cornerstone.Serialization.SpeedyPacket values)");
		builder.IndentWriteLine("{");
		builder.Indent++;
		builder.IndentWriteLine("var version = (byte) values[0];");
		foreach (var group in versionGroups)
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
					builder.IndentWriteLine($"{propertyName} = ({propertyType})values[{v.Offset}];");
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
	/// <param name="type"> The property type to check. </param>
	/// <param name="elementType"> Set to the element type (T) if this is a supported collection; otherwise null. </param>
	/// <returns> true if the type is a supported collection; otherwise false. </returns>
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
				// Check if the type itself or any of its interfaces match one of our supported collection definitions
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