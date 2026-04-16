#region References

using System.Collections.Generic;
using System.Linq;
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
					.SelectMany(x => (object[]) x.ConstructorArguments[0])
					.Where(x => x != null)
					.Select(x => (string) x)
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

	private static IEnumerable<SourcePropertyInfo> GetNotifiableProperties(SourceTypeInfo type)
	{
		var attribute = type.Attributes.FirstOrDefault(x => x.Name is NameNotifiableAttribute);
		var attributeProperties = new HashSet<string>();
		if (attribute != null)
		{
			var items = (object[]) attribute.ConstructorArguments[0];
			if ((items.Length == 1) && Equals(items[0], "*"))
			{
				foreach (var p in type.Properties
							.Where(x => x.IsPartial && x.CanRead && x.CanWrite))
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

	#endregion
}