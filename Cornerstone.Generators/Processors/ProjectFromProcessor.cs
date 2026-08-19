#region References

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using static Cornerstone.Generators.Generator;
using SourcePropertyInfo = Cornerstone.Generators.Models.SourcePropertyInfo;
using SourceTypeInfo = Cornerstone.Generators.Models.SourceTypeInfo;
using SymbolDisplayFormats = Cornerstone.Generators.Models.SymbolDisplayFormats;

#endregion

namespace Cornerstone.Generators.Processors;

/// <summary>
/// Emits notify properties for [ProjectFrom&lt;TContract&gt;] members not already
/// declared on the type, and adds them to SourceTypeInfo.Properties so
/// SourceReflection / change bits include them.
/// </summary>
internal sealed class ProjectFromProcessor : ITypeProcessor
{
	#region Properties

	public bool EmitsInsideTypeBlock => true;

	#endregion

	#region Methods

	public void Process(CSharpCodeBuilder builder, SourceTypeInfo typeInfo)
	{
		var contracts = GetProjectFromContracts(typeInfo);
		if (contracts.Count == 0)
		{
			return;
		}

		if (!typeInfo.IsPartial)
		{
			DiagnosticReporter.ReportTypeIsNotPartial(typeInfo.TypeSymbol);
			return;
		}

		var existing = new HashSet<string>(typeInfo.Properties.Select(x => x.Name));
		var supportsOnPropertyChangedWithValues = ImplementsMethodRecursively(
			typeInfo.TypeSymbol, "OnPropertyChanged", "string", "TValue", "TValue") != null;
		var supportsOnPropertyChanged = supportsOnPropertyChangedWithValues
			|| (ImplementsMethodRecursively(typeInfo.TypeSymbol, "OnPropertyChanged", "string") != null);
		var supportsOnPropertyChangingWithValues = ImplementsMethodRecursively(
			typeInfo.TypeSymbol, "OnPropertyChanging", "string", "TValue", "TValue") != null;
		var supportsOnPropertyChanging = supportsOnPropertyChangingWithValues
			|| (ImplementsMethodRecursively(typeInfo.TypeSymbol, "OnPropertyChanging", "string") != null);

		foreach (var contract in contracts)
		{
			foreach (var property in contract.GetMembers().OfType<IPropertySymbol>())
			{
				if (property.IsStatic
					|| (property.Parameters.Length > 0)
					|| (property.GetMethod == null)
					|| existing.Contains(property.Name)
					|| !IsMappable(property.Type))
				{
					continue;
				}

				var typeName = property.Type.ToDisplayString(SymbolDisplayFormats.GlobalFullyQualifiedName);
				var fieldName = "_" + ToCamelCase(property.Name);
				var defaultLiteral = property.Type.SpecialType == SpecialType.System_String
					? " = \"\""
					: string.Empty;

				builder.IndentWriteLine($"private {typeName} {fieldName}{defaultLiteral};");
				builder.IndentWriteLine($"public {typeName} {property.Name}");
				builder.IndentWriteLine("{");
				builder.Indent++;
				builder.IndentWriteLine($"get => {fieldName};");
				builder.IndentWriteLine("set");
				builder.IndentWriteLine("{");
				builder.Indent++;
				builder.IndentWrite("if (!global::System.Collections.Generic.EqualityComparer<");
				builder.Write(typeName);
				builder.Write(">.Default.Equals(value, ");
				builder.Write(fieldName);
				builder.WriteLine("))");
				builder.IndentWriteLine("{");
				builder.Indent++;

				if (supportsOnPropertyChangingWithValues || supportsOnPropertyChangedWithValues)
				{
					builder.IndentWriteLine($"var oldValue = {fieldName};");
				}

				if (supportsOnPropertyChanging)
				{
					builder.IndentWrite("OnPropertyChanging(\"");
					builder.Write(property.Name);
					builder.WriteLine(supportsOnPropertyChangingWithValues ? "\", oldValue, value);" : "\");");
				}

				builder.IndentWriteLine($"{fieldName} = value;");

				if (supportsOnPropertyChanged)
				{
					builder.IndentWrite("OnPropertyChanged(\"");
					builder.Write(property.Name);
					builder.WriteLine(supportsOnPropertyChangedWithValues ? "\", oldValue, value);" : "\");");
				}

				builder.Indent--;
				builder.IndentWriteLine("}");
				builder.Indent--;
				builder.IndentWriteLine("}");
				builder.Indent--;
				builder.IndentWriteLine("}");
				builder.WriteLine();

				existing.Add(property.Name);
				typeInfo.Properties.Add(new SourcePropertyInfo
				{
					Name = property.Name,
					Accessibility = Accessibility.Public,
					CanRead = true,
					CanWrite = true,
					GetMethodAccessibility = Accessibility.Public,
					SetMethodAccessibility = Accessibility.Public,
					GlobalFullyQualifiedName = typeName,
					FullyQualifiedName = typeName,
					IsPartial = false,
					IsReadOnly = false
				});
			}
		}
	}

	void ITypeProcessor.Initialize(Compilation compilation)
	{
	}

	private static List<INamedTypeSymbol> GetProjectFromContracts(SourceTypeInfo typeInfo)
	{
		var result = new List<INamedTypeSymbol>();
		foreach (var attribute in typeInfo.Attributes)
		{
			if ((attribute.Name != NameProjectFromAttribute)
				&& (attribute.TypeSymbol?.Name != NameProjectFromAttribute))
			{
				continue;
			}

			INamedTypeSymbol contract = null;
			if (attribute.GenericTypes is { Length: 1 } && attribute.GenericTypes[0] is INamedTypeSymbol generic)
			{
				contract = generic;
			}
			else if (attribute.TypeSymbol is { IsGenericType: true, TypeArguments.Length: 1 } named
				&& named.TypeArguments[0] is INamedTypeSymbol fromAttribute)
			{
				contract = fromAttribute;
			}

			if (contract != null)
			{
				result.Add(contract);
			}
		}

		return result;
	}

	private static bool IsMappable(ITypeSymbol type)
	{
		if (type.SpecialType == SpecialType.System_String)
		{
			return true;
		}

		if ((type.TypeKind == TypeKind.Enum) || type.IsValueType)
		{
			return type.SpecialType is not SpecialType.None
				|| type.TypeKind == TypeKind.Enum
				|| type.Name is "DateTime" or "DateTimeOffset" or "Guid" or "TimeSpan" or "Decimal";
		}

		return false;
	}

	private static string ToCamelCase(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return name;
		}

		if (name.Length == 1)
		{
			return name.ToLowerInvariant();
		}

		return char.ToLowerInvariant(name[0]) + name.Substring(1);
	}

	#endregion
}
