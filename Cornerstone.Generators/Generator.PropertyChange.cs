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

	public static IEnumerable<ISymbol> GetAllMembers(INamedTypeSymbol type)
	{
		var processedMembers = new HashSet<string>();
		var processedTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

		foreach (var member in GetAllMembersRecursive(type, processedMembers, processedTypes))
		{
			yield return member;
		}
	}

	public static IMethodSymbol ImplementsMethodRecursively(INamedTypeSymbol typeSymbol, string methodName, params string[] parameterTypes)
	{
		var allMembers = GetAllMembers(typeSymbol).OfType<IMethodSymbol>().ToArray();
		return ImplementsMethod(allMembers, methodName, parameterTypes);
	}

	private static void GenerateOnPropertyChangedEvent(CSharpCodeBuilder builder, string memberName)
	{
		builder.WriteIndent();
		builder.Write("OnPropertyChanged(\"");
		builder.Write(memberName);
		builder.WriteLine("\");");
	}

	private static void GenerateOnPropertyChangingEvent(CSharpCodeBuilder builder, string memberName)
	{
		builder.WriteIndent();
		builder.Write("OnPropertyChanging(\"");
		builder.Write(memberName);
		builder.WriteLine("\");");
	}

	private static IEnumerable<ISymbol> GetAllMembersRecursive(
		INamedTypeSymbol type,
		HashSet<string> processedMembers,
		HashSet<INamedTypeSymbol> processedTypes)
	{
		// Avoid processing the same type twice
		if ((type == null) || !processedTypes.Add(type))
		{
			yield break;
		}

		// Process members of the current type
		foreach (var member in type.GetMembers())
		{
			if (processedMembers.Add(member.Name))
			{
				yield return member;
			}
		}

		// Recursively process base type
		if ((type.BaseType != null) && (type.BaseType.SpecialType != SpecialType.System_Object))
		{
			foreach (var member in GetAllMembersRecursive(type.BaseType, processedMembers, processedTypes))
			{
				yield return member;
			}
		}

		// Process direct interfaces
		// Use Interfaces, not AllInterfaces, for explicit recursion
		foreach (var interfaceSymbol in type.Interfaces)
		{
			foreach (var member in GetAllMembersRecursive(interfaceSymbol, processedMembers, processedTypes))
			{
				yield return member;
			}
		}
	}

	private static IEnumerable<SourcePropertyInfo> GetNotifiableProperties(SourceTypeInfo type)
	{
		foreach (var p in type.Properties)
		{
			if (p.Attributes.Any(a => a.FullyQualifiedName == FullNameNotifyAttribute))
			{
				yield return p;
			}
		}
	}

	private static IMethodSymbol ImplementsMethod(IMethodSymbol[] methods, string methodName, params string[] parameterTypes)
	{
		foreach (var method in methods)
		{
			// Check method name and parameter count
			if ((method.Name != methodName)
				|| (method.Parameters.Length != parameterTypes.Length))
			{
				continue;
			}

			// Verify parameter types match
			var parametersMatch = true;
			for (var i = 0; i < parameterTypes.Length; i++)
			{
				var name = method.Parameters[i].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

				if (!Equals(name, parameterTypes[i]))
				{
					parametersMatch = false;
					break;
				}
			}
			if (parametersMatch)
			{
				return method;
			}
		}

		return null;
	}

	private static void ProcessNotifiable(CSharpCodeBuilder builder, SourceTypeInfo typeInfo)
	{
		var notifiableProperties = GetNotifiableProperties(typeInfo).ToArray();
		if (notifiableProperties.Length <= 0)
		{
			return;
		}

		var supportsOnPropertyChanged = ImplementsMethodRecursively(typeInfo.TypeSymbol, "OnPropertyChanged", "string");
		var supportsOnPropertyChanging = ImplementsMethodRecursively(typeInfo.TypeSymbol, "OnPropertyChanging", "string");

		if (!typeInfo.IsPartial)
		{
			DiagnosticReporter.ReportTypeIsNotPartial(typeInfo.TypeSymbol);
		}

		foreach (var property in notifiableProperties)
		{
			var fieldName = CalculateFieldName(property.PropertySymbol);
			var (propertyAccess, getterAccessibility, setterAccessibility) = CalculateAccessibilities(property.PropertySymbol);

			builder.IndentWriteLine($"private {property.GlobalFullyQualifiedName} {fieldName};");
			builder.IndentWrite($"{propertyAccess} {(property.IsVirtual ? "virtual " : "")}");
			if (!property.PropertySymbol.IsPartialDefinition)
			{
				DiagnosticReporter.ReportPropertyIsNotPartial(typeInfo.TypeSymbol, property.PropertySymbol);
			}
			builder.Write("partial ");
			builder.WriteLine($"{property.GlobalFullyQualifiedName} {property.Name}");
			builder.IndentWriteLine("{");
			builder.Indent++;

			builder.IndentWriteLine(string.IsNullOrWhiteSpace(getterAccessibility) ? $"get => {fieldName};" : $"{getterAccessibility} get => {fieldName};");
			builder.IndentWriteLine(string.IsNullOrWhiteSpace(setterAccessibility) ? "set" : $"{setterAccessibility} set");
			builder.IndentWriteLine("{");
			builder.Indent++;

			builder.IndentWrite("if (!global::System.Collections.Generic.EqualityComparer<");
			builder.Write(property.GlobalFullyQualifiedName);
			builder.Write(">.Default.Equals(value, ");
			builder.Write(fieldName);
			builder.WriteLine("))");
			builder.IndentWriteLine("{");
			builder.Indent++;

			if (supportsOnPropertyChanging != null)
			{
				GenerateOnPropertyChangingEvent(builder, property.Name);
			}

			builder.IndentWriteLine($"{fieldName} = value;");

			if (supportsOnPropertyChanged != null)
			{
				GenerateOnPropertyChangedEvent(builder, property.Name);
			}

			var alsoNotify = property.Attributes.Where(a => a.FullyQualifiedName == FullNameAlsoNotifyAttribute).ToList();
			if (alsoNotify.Count > 0)
			{
				var alsoNotifyMembers = alsoNotify
					.SelectMany(x => x.NamedArguments.TryGetValue("OtherProperties", out var values) ? (string[]) values : [])
					.Where(x => x != null)
					.ToList();

				foreach (var otherMember in alsoNotifyMembers)
				{
					GenerateOnPropertyChangedEvent(builder, otherMember);
				}
				
				alsoNotifyMembers = alsoNotify
					.SelectMany(x => x.ConstructorArguments.Cast<string>())
					.Where(x => x != null)
					.ToList();

				foreach (var otherMember in alsoNotifyMembers)
				{
					GenerateOnPropertyChangedEvent(builder, otherMember);
				}
			}

			builder.Indent--;
			builder.IndentWriteLine("}");
			builder.Indent--;
			builder.IndentWriteLine("}");
			builder.Indent--;
			builder.IndentWriteLine("}");
		}
	}

	#endregion
}