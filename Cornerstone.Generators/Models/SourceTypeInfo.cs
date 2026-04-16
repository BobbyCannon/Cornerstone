#region References

using System;
using System.Collections.Generic;
using System.Text;
using Cornerstone.Reflection;
using Microsoft.CodeAnalysis;

#endregion

namespace Cornerstone.Generators.Models;

public class SourceTypeInfo : Reflection.SourceTypeInfo
{
	#region Fields

	private string _fullyQualifiedSourceReflectorName;

	#endregion

	#region Constructors

	public SourceTypeInfo()
	{
		Attributes = [];
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

	public new Accessibility Accessibility { get; set; }
	public new List<SourceAttributeInfo> Attributes { get; } = [];
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
	public bool IsRefLikeType { get; set; }
	public bool IsSourceReflectionType { get; set; }
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
			if (!string.IsNullOrEmpty(ns) && (ns != "<global namespace>"))
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

				ToSafeCodeName(typeArg, builder, includeNamespace);
				isFirst = false;
			}
		}
		else if (typeSymbol.TypeKind == TypeKind.Array)
		{
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