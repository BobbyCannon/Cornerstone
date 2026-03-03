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

	public void ProcessComparable(CSharpCodeBuilder builder, SourceTypeInfo typeInfo)
	{
		if (!ShouldImplementComparable(typeInfo.TypeSymbol))
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

	private static void AppendCompareGenericMethod(CSharpCodeBuilder builder, SourceTypeInfo typeInfo, List<SourcePropertyInfo> comparableProperties)
	{
		builder.WriteLine();
		builder.IndentWriteLine($"public int CompareTo({typeInfo.FullyQualifiedCodeName} value)");
		builder.IndentWriteLine("{");
		builder.IncreaseIndent();
		AppendCompareProperties(builder, comparableProperties);
		builder.DecreaseIndent();
		builder.IndentWriteLine("}");
	}

	private static void AppendCompareMethod(CSharpCodeBuilder builder, SourceTypeInfo typeInfo, bool implementsGenericComparable, List<SourcePropertyInfo> comparableProperties)
	{
		builder.IndentWriteLine("public int CompareTo(object other)");
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

			builder.Write(needsNullableCompare
				? $"cmp = global::System.Nullable.Compare(this.{propName}, value.{propName});"
				: $"cmp = this.{propName}.CompareTo(value.{propName});"
			);

			builder.WriteLine();
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
			// Classic non-generic IComparable
			if ((m.Parameters.Length == 1)
				&& (m.Parameters[0].Type.SpecialType == SpecialType.System_Object))
			{
				return true;
			}

			// IComparable<T>.CompareTo(T)
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
		const string ComparableT = "System.IComparable<T>";

		return type.AllInterfaces
			.Any(x => x is { IsGenericType: true }
				&& (x.OriginalDefinition.ToDisplayString() == ComparableT)
				&& SymbolEqualityComparer.Default.Equals(x.TypeArguments[0], type));
	}

	private static bool ShouldImplementComparable(INamedTypeSymbol symbol)
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

	#endregion
}