#region References

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using static Cornerstone.Generators.Generator;
using SourcePropertyInfo = Cornerstone.Generators.Models.SourcePropertyInfo;
using SourceTypeInfo = Cornerstone.Generators.Models.SourceTypeInfo;

#endregion

namespace Cornerstone.Generators.Processors;

internal sealed class ComparableProcessor : ITypeProcessor
{
	#region Constants

	private const string ComparableT = "System.IComparable<T>";

	#endregion

	#region Properties

	public bool EmitsInsideTypeBlock => true;

	#endregion

	#region Methods

	public void Process(CSharpCodeBuilder builder, SourceTypeInfo typeInfo)
	{
		if (!ShouldProcess(typeInfo.TypeSymbol))
		{
			return;
		}

		var implementsSelfComparable = ImplementsSelfComparable(typeInfo.TypeSymbol);
		var comparableProperties = GetComparableProperties(typeInfo);

		AppendCompareMethod(builder, typeInfo, implementsSelfComparable, comparableProperties);

		if (implementsSelfComparable)
		{
			AppendCompareGenericMethod(builder, typeInfo, comparableProperties);
		}
	}

	internal static bool HasBaseCompareToImplementation(INamedTypeSymbol symbol)
	{
		// Walk the complete base hierarchy (excluding Object)
		for (var baseType = symbol.BaseType;
			(baseType != null) && (baseType.SpecialType != SpecialType.System_Object);
			baseType = baseType.BaseType)
		{
			if (HasCompareToMethod(baseType)
				|| ShouldProcess(baseType))
			{
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Also used by the incremental pipeline to filter candidate types.
	/// </summary>
	internal static bool ShouldProcess(INamedTypeSymbol symbol)
	{
		if (symbol.BaseType?.SpecialType == SpecialType.System_Enum)
		{
			return false;
		}

		var isComparable = symbol
			.AllInterfaces
			.Any(i =>
				i.Name is NameIComparable
					or NameIComparableOfT
			);

		var isComparableImplemented = isComparable
			&& HasCompareToMethod(symbol);

		if (isComparable
			&& !isComparableImplemented)
		{
			return true;
		}
		return false;
	}

	private static void AppendCompareGenericMethod(CSharpCodeBuilder builder, SourceTypeInfo typeInfo, List<SourcePropertyInfo> comparableProperties)
	{
		builder.WriteLine();
		builder.IndentWriteLine($"public int CompareTo({typeInfo.FullyGlobalQualifiedName} value)");
		builder.IndentWriteLine("{");
		builder.IncreaseIndent();
		AppendCompareProperties(builder, comparableProperties);
		builder.DecreaseIndent();
		builder.IndentWriteLine("}");
	}

	private static void AppendCompareMethod(CSharpCodeBuilder builder, SourceTypeInfo typeInfo, bool implementsGenericComparable, List<SourcePropertyInfo> comparableProperties)
	{
		var isOverride = HasBaseCompareToImplementation(typeInfo.TypeSymbol);
		var modifier = isOverride ? "override" : "virtual";

		builder.IndentWriteLine($"public {modifier} int CompareTo(object other)");
		builder.IndentWriteLine("{");
		builder.IncreaseIndent();

		builder.WriteBlock(
			$"if (other is not {typeInfo.FullyGlobalQualifiedName} value)",
			() => builder.IndentWriteLine($"throw new ArgumentException(\"Object is not a {typeInfo.TypeSymbol.Name}\", nameof(other));")
		);
		builder.WriteLine();

		if (implementsGenericComparable)
		{
			builder.IndentWriteLine("return CompareTo(value);");
		}
		else
		{
			AppendCompareProperties(builder, comparableProperties);
		}

		builder.DecreaseIndent();
		builder.IndentWriteLine("}");
	}

	private static void AppendCompareProperties(CSharpCodeBuilder builder, List<SourcePropertyInfo> comparableProperties)
	{
		builder.WriteBlock("if (value == null)", () => builder.IndentWriteLine("return 1;"));
		builder.WriteLine();

		var isFirst = true;

		foreach (var prop in comparableProperties)
		{
			var propName = prop.PropertySymbol.Name;
			var propType = prop.PropertySymbol.Type;
			var needsNullableCompare = propType.IsValueType
				&& (propType.NullableAnnotation == NullableAnnotation.Annotated);

			if (!isFirst)
			{
				builder.WriteLine();
				builder.IndentWrite(string.Empty);
			}
			else
			{
				builder.IndentWrite("var ");
			}

			if (needsNullableCompare)
			{
				builder.WriteLine($"cmp = global::System.Nullable.Compare(this.{propName}, value.{propName});");
			}
			else
			{
				var typeDisplay = prop.PropertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
				builder.WriteLine($"cmp = global::System.Collections.Generic.Comparer<{typeDisplay}>.Default.Compare(this.{propName}, value.{propName});");
			}

			builder.IndentWriteLine("if (cmp != 0) return cmp;");
			isFirst = false;
		}

		builder.WriteLine();
		builder.IndentWriteLine("return 0;");
	}

	private static List<SourcePropertyInfo> GetComparableProperties(SourceTypeInfo typeInfo)
	{
		return typeInfo.Properties
			.Where(p =>
				(p.CanRead
					&& !p.IsStatic
					&& !p.IsIndexer
					&& (p.Accessibility == Accessibility.Public)
					&& (p.PropertySymbol?.IsReadOnly == false))
				|| !string.IsNullOrEmpty(p.PropertySymbol?.Name)
			)
			.OrderBy(p => p.Name)
			.ToList();
	}

	private static bool HasCompareToMethod(INamedTypeSymbol symbol)
	{
		var compareToMembers = symbol
			.GetMembers("CompareTo")
			.OfType<IMethodSymbol>();

		foreach (var m in compareToMembers)
		{
			if ((m.Parameters.Length == 1)
				&& (m.Parameters[0].Type.SpecialType == SpecialType.System_Object))
			{
				return true;
			}

			if ((m.Parameters.Length == 1) &&
				m.Parameters[0].Type is INamedTypeSymbol { IsGenericType: false } paramType
				&& SymbolEqualityComparer.Default.Equals(paramType.OriginalDefinition, symbol))
			{
				return true;
			}
		}

		return false;
	}

	private static bool ImplementsSelfComparable(INamedTypeSymbol type)
	{
		return type.AllInterfaces
			.Any(x => x is { IsGenericType: true }
				&& (x.OriginalDefinition.ToDisplayString() == ComparableT)
				&& SymbolEqualityComparer.Default.Equals(x.TypeArguments[0], type));
	}

	#endregion
}