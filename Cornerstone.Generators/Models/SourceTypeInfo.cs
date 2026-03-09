#region References

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

#endregion

namespace Cornerstone.Generators.Models;

public class SourceTypeInfo : SourceMemberInfo
{
	#region Fields

	private string _fullyQualifiedSourceReflectorName;

	#endregion

	#region Constructors

	public SourceTypeInfo()
	{
		Constructors = [];
		Fields = [];
		Generics = [];
		Interfaces = [];
		Methods = [];
		OuterTypes = [];
		Properties = [];
	}

	#endregion

	#region Properties

	public Accessibility Accessibility { get; set; }
	public string BaseFullyGlobalQualifiedTypeName { get; set; }
	public List<SourceConstructorInfo> Constructors { get; }
	public string EnumUnderlyingType { get; set; }
	public List<SourceFieldInfo> Fields { get; }
	public string FullyGlobalQualifiedName { get; set; }
	public string FullyQualifiedName { get; set; }
	public string FullyQualifiedSourceReflectorName => _fullyQualifiedSourceReflectorName ??= ToSourceReflectorName(TypeSymbol);
	public List<SourceGenericInfo> Generics { get; }
	public bool HasBaseRequiredMembers { get; set; }
	public List<SourceInterfaceInfo> Interfaces { get; }
	public bool IsAbstract { get; set; }
	public bool IsClass { get; set; }
	public bool IsEnum { get; set; }
	public bool IsGenericType { get; set; }
	public bool IsGenericTypeDefinition { get; set; }
	public bool IsPartial { get; set; }
	public bool IsReadOnly { get; set; }
	public bool IsRefLikeType { get; set; }
	public bool IsSourceReflectionType { get; set; }
	public bool IsStatic { get; set; }
	public bool IsStruct { get; set; }
	public List<SourceMethodInfo> Methods { get; }
	public List<string> OuterTypes { get; }
	public List<SourcePropertyInfo> Properties { get; }
	public INamedTypeSymbol TypeSymbol { get; set; }

	#endregion

	#region Methods

	private static void ToSafeCodeName(ITypeSymbol typeSymbol, StringBuilder builder, bool includeNamespace = false, string delimiter = "_")
	{
		if (typeSymbol is null)
		{
			throw new ArgumentNullException(nameof(typeSymbol));
		}

		if (includeNamespace && (typeSymbol.ContainingNamespace != null))
		{
			var ns = typeSymbol.ContainingNamespace.ToDisplayString();
			if (!string.IsNullOrEmpty(ns))
			{
				builder.Append(ns.Replace(".", delimiter)).Append(delimiter);
			}
		}

		var name = typeSymbol.Name;
		if (typeSymbol is INamedTypeSymbol { IsGenericType: true, Arity: > 0 })
		{
			var tickIndex = name.IndexOf('`');
			if (tickIndex >= 0)
			{
				name = name.Substring(0, tickIndex);
			}
		}

		builder.Append(name);

		if (typeSymbol is INamedTypeSymbol { TypeArguments.Length: > 0 } named)
		{
			builder.Append("Of");

			var isFirst = true;
			foreach (var typeArg in named.TypeArguments)
			{
				if (!isFirst)
				{
					builder.Append("Of");
				}

				// Recurse for nested generics / arrays / pointers / type parameters
				ToSafeCodeName(typeArg, builder, includeNamespace);
				isFirst = false;
			}
		}
		else if (typeSymbol.TypeKind == TypeKind.Array)
		{
			// Simple array handling — you can make fancier (e.g. Array2D, JaggedArray…)
			builder.Append("Array");
			if (typeSymbol is IArrayTypeSymbol { Rank: > 1 } array)
			{
				builder.Append(array.Rank);
			}
		}
		else if (typeSymbol.TypeKind == TypeKind.Pointer)
		{
			builder.Append("Ptr");
		}
	}

	private static string ToSourceReflectorName(ITypeSymbol typeSymbol)
	{
		var builder = new StringBuilder();
		ToSafeCodeName(typeSymbol, builder, true, string.Empty);
		return builder.ToString();
	}

	#endregion
}