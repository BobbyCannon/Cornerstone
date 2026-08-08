#region References

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using static Cornerstone.Generators.Generator;
using SourcePropertyInfo = Cornerstone.Generators.Models.SourcePropertyInfo;
using SourceTypeInfo = Cornerstone.Generators.Models.SourceTypeInfo;

#endregion

namespace Cornerstone.Generators.Processors;

internal sealed class NotifiableProcessor : ITypeProcessor
{
	#region Properties

	public bool EmitsInsideTypeBlock => true;

	#endregion

	#region Methods

	public void Process(CSharpCodeBuilder builder, SourceTypeInfo typeInfo)
	{
		var notifiableProperties = GetNotifiableProperties(typeInfo).ToArray();
		if (notifiableProperties.Length <= 0)
		{
			return;
		}

		var supportsOnPropertyChangedWithValues = ImplementsMethodRecursively(
			typeInfo.TypeSymbol, "OnPropertyChanged", "string", "TValue", "TValue") != null;

		var supportsOnPropertyChanged = supportsOnPropertyChangedWithValues
			|| (ImplementsMethodRecursively(typeInfo.TypeSymbol, "OnPropertyChanged", "string") != null);

		var supportsOnPropertyChangingWithValues = ImplementsMethodRecursively(
			typeInfo.TypeSymbol, "OnPropertyChanging", "string", "TValue", "TValue") != null;

		var supportsOnPropertyChanging = supportsOnPropertyChangingWithValues
			|| (ImplementsMethodRecursively(typeInfo.TypeSymbol, "OnPropertyChanging", "string") != null);

		var supportsNotifyComputed = (ImplementsMethodRecursively(
				typeInfo.TypeSymbol, "NotifyComputedPropertyChanged", "string") != null)
			|| (ImplementsMethodRecursively(
				typeInfo.TypeSymbol, "NotifyComputedPropertyChanged", "string", "T") != null);

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

			// Getter
			builder.IndentWriteLine(string.IsNullOrWhiteSpace(getterAccessibility)
				? $"get => {fieldName};"
				: $"{getterAccessibility} get => {fieldName};");

			// Setter
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

			// Capture old value only when needed
			if (supportsOnPropertyChangingWithValues || supportsOnPropertyChangedWithValues)
			{
				builder.IndentWriteLine($"var oldValue = {fieldName};");
			}

			if (supportsOnPropertyChanging)
			{
				GenerateOnPropertyChanging(builder, property.Name, supportsOnPropertyChangingWithValues);
			}

			builder.IndentWriteLine($"{fieldName} = value;");

			if (supportsOnPropertyChanged)
			{
				GenerateOnPropertyChanged(builder, property.Name, supportsOnPropertyChangedWithValues);
			}

			//
			// AlsoNotify → treat as computed properties
			//
			var alsoNotifyMembers = GetAlsoNotifyMembers(property);

			foreach (var otherMember in alsoNotifyMembers)
			{
				if (supportsNotifyComputed)
				{
					builder.IndentWrite("NotifyComputedPropertyChanged(\"");
					builder.Write(otherMember);
					builder.WriteLine("\");");
				}
				else if (supportsOnPropertyChangedWithValues)
				{
					// Prefer the rich overload when available
					builder.IndentWrite("OnPropertyChanged(\"");
					builder.Write(otherMember);
					builder.WriteLine("\", default, default);");
				}
				else if (supportsOnPropertyChanged)
				{
					// Classic fallback
					builder.IndentWrite("OnPropertyChanged(\"");
					builder.Write(otherMember);
					builder.WriteLine("\");");
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

	private static void GenerateOnPropertyChanged(CSharpCodeBuilder builder, string memberName, bool withValues)
	{
		builder.IndentWrite("OnPropertyChanged(\"");
		builder.Write(memberName);
		builder.WriteLine(withValues ? "\", oldValue, value);" : "\");");
	}

	private static void GenerateOnPropertyChanging(CSharpCodeBuilder builder, string memberName, bool withValues)
	{
		builder.IndentWrite("OnPropertyChanging(\"");
		builder.Write(memberName);
		builder.WriteLine(withValues ? "\", oldValue, value);" : "\");");
	}

	private static List<string> GetAlsoNotifyMembers(SourcePropertyInfo property)
	{
		var result = new List<string>();

		var alsoNotifyAttributes = property.Attributes
			.Where(a => a.FullyQualifiedName == FullNameAlsoNotifyAttribute)
			.ToList();

		foreach (var attr in alsoNotifyAttributes)
		{
			// Named argument: OtherProperties = new[] { ... }
			if (attr.NamedArguments.TryGetValue("OtherProperties", out var namedValues)
				&& namedValues is object[] namedArray)
			{
				result.AddRange(namedArray.OfType<string>());
			}

			// Constructor argument
			if ((attr.ConstructorArguments.Length > 0)
				&& attr.ConstructorArguments[0] is object[] ctorArray)
			{
				result.AddRange(ctorArray.OfType<string>());
			}
		}

		return result.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
	}

	private static IEnumerable<SourcePropertyInfo> GetNotifiableProperties(SourceTypeInfo type)
	{
		var attribute = type.Attributes.FirstOrDefault(x => x.Name is NameNotifiableAttribute);
		var attributeProperties = new HashSet<string>();

		if (attribute != null)
		{
			var items = attribute.ConstructorArguments.Length == 1
				? (object[]) attribute.ConstructorArguments[0]
				: [];

			if ((items.Length == 1) && Equals(items[0], "*"))
			{
				foreach (var p in type.Properties.Where(x => x.IsPartial && x.CanRead && x.CanWrite))
				{
					yield return p;
				}
				yield break;
			}

			foreach (var property in items)
			{
				attributeProperties.Add((string) property);
			}
		}

		foreach (var p in type.Properties)
		{
			if (attributeProperties.Contains(p.Name)
				|| p.Attributes.Any(a => a.FullyQualifiedName == FullNameNotifyAttribute))
			{
				yield return p;
			}
		}
	}

	void ITypeProcessor.Initialize(Compilation compilation)
	{
	}

	#endregion
}