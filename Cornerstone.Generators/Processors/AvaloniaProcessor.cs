#region References

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using static Cornerstone.Generators.Generator;
using SourcePropertyInfo = Cornerstone.Generators.Models.SourcePropertyInfo;
using SourceTypeInfo = Cornerstone.Generators.Models.SourceTypeInfo;

#endregion

namespace Cornerstone.Generators.Processors;

internal sealed class AvaloniaProcessor : ITypeProcessor
{
	#region Properties

	public bool EmitsInsideTypeBlock => true;

	#endregion

	#region Methods

	public void Process(CSharpCodeBuilder builder, SourceTypeInfo typeInfo)
	{
		ProcessAttachedProperties(builder, typeInfo);
		ProcessDirectProperties(builder, typeInfo);
		ProcessStyledProperties(builder, typeInfo);
	}

	private static Dictionary<SourcePropertyInfo, AttributeData> GetAttachedProperties(SourceTypeInfo typeInfo)
	{
		var response = typeInfo
			.Properties
			.Select(x => (x, x.Attributes.FirstOrDefault(a => a.Name == NameAttachedPropertyAttribute)?.Data))
			.Where(x => x.Data != null)
			.ToDictionary(x => x.x, x => x.Data);
		return response;
	}

	private static Dictionary<SourcePropertyInfo, AttributeData> GetDirectProperties(SourceTypeInfo typeInfo)
	{
		var response = typeInfo
			.Properties
			.Select(x => (x, x.Attributes.FirstOrDefault(a => a.Name == NameDirectPropertyAttribute)?.Data))
			.Where(x => x.Data != null)
			.ToDictionary(x => x.x, x => x.Data);
		return response;
	}

	private static Dictionary<SourcePropertyInfo, AttributeData> GetStyledProperties(SourceTypeInfo typeInfo)
	{
		var response = typeInfo
			.Properties
			.Select(x => (x, x.Attributes.FirstOrDefault(a => a.Name == NameStyledPropertyAttribute)?.Data))
			.Where(x => x.Data != null)
			.ToDictionary(x => x.x, x => x.Data);
		return response;
	}

	private static void ProcessAttachedProperties(CSharpCodeBuilder builder, SourceTypeInfo typeInfo)
	{
		var attachedProperties = GetAttachedProperties(typeInfo);
		var classFqtn = typeInfo.FullyGlobalQualifiedName;

		foreach (var property in attachedProperties)
		{
			var member = property.Key;
			var memberTypeFullName = member.GlobalFullyQualifiedName;
			var (propertyAccess, getterAccessibility, setterAccessibility) = CalculateAccessibilities(property.Key.PropertySymbol);

			builder.IndentWrite($"public static readonly global::Avalonia.AttachedProperty<{memberTypeFullName}> {member.Name}Property");
			builder.Write($" = global::Avalonia.AvaloniaProperty.RegisterAttached<{classFqtn}, {classFqtn}, {memberTypeFullName}>(nameof({member.Name})");
			if (TryGetNamedArgument(property.Value, "DefaultValue", out var defaultValue))
			{
				builder.Write($", defaultValue: {CSharpCodeBuilder.GetConstantLiteral(defaultValue.Value)}");
			}
			if (TryGetNamedArgument(property.Value, "Inherits", out var inheritsValue))
			{
				builder.Write($", inheritsValue: {CSharpCodeBuilder.GetConstantLiteral(inheritsValue.Value)}");
			}
			builder.WriteLine(");");

			builder.IndentWrite($"{propertyAccess} {(property.Key.IsVirtual ? "virtual " : "")}");
			if (!property.Key.IsPartial)
			{
				DiagnosticReporter.ReportPropertyIsNotPartial(typeInfo.TypeSymbol, property.Key.PropertySymbol);
			}
			builder.Write("partial ");
			builder.WriteLine($"{memberTypeFullName} {member.Name}");
			builder.IndentWriteLine("{");
			builder.Indent++;
			builder.IndentWrite(string.IsNullOrWhiteSpace(getterAccessibility) ? "get => " : $"{getterAccessibility} get => ");
			builder.WriteLine($"GetValue({member.Name}Property);");
			builder.IndentWrite(string.IsNullOrWhiteSpace(setterAccessibility) ? "set => " : $"{setterAccessibility} set => ");
			builder.WriteLine($"SetValue({member.Name}Property, value);");
			builder.Indent--;
			builder.IndentWriteLine("}");
		}
	}

	private static void ProcessDirectProperties(CSharpCodeBuilder builder, SourceTypeInfo typeInfo)
	{
		var directProperties = GetDirectProperties(typeInfo);
		var classFqtn = typeInfo.FullyGlobalQualifiedName;

		foreach (var property in directProperties)
		{
			var member = property.Key;
			var memberTypeFullName = member.GlobalFullyQualifiedName;

			builder.IndentWrite($"public static readonly global::Avalonia.DirectProperty<{typeInfo.FullyGlobalQualifiedName}, {memberTypeFullName}> {member.Name}Property");
			builder.Write($" = global::Avalonia.AvaloniaProperty.RegisterDirect<{classFqtn}, {memberTypeFullName}>(nameof({member.Name})");

			if (property.Key.CanRead)
			{
				builder.Write($", getter: x => x.{property.Key.Name}");
			}

			if (property.Key.CanWrite)
			{
				builder.Write($", setter: (x, v) => x.{property.Key.Name} = v");
			}

			builder.WriteLine(");");
		}
	}

	private static void ProcessStyledProperties(CSharpCodeBuilder builder, SourceTypeInfo typeInfo)
	{
		var styledProperties = GetStyledProperties(typeInfo);
		var classFqtn = typeInfo.FullyGlobalQualifiedName;

		foreach (var property in styledProperties)
		{
			var member = property.Key;
			var memberTypeFullName = member.GlobalFullyQualifiedName;
			var (propertyAccess, getterAccessibility, setterAccessibility) = CalculateAccessibilities(property.Key.PropertySymbol);

			builder.IndentWrite($"public static readonly global::Avalonia.StyledProperty<{memberTypeFullName}> {member.Name}Property");
			builder.Write($" = global::Avalonia.AvaloniaProperty.Register<{classFqtn}, {memberTypeFullName}>(nameof({member.Name})");

			if (TryGetNamedArgument(property.Value, "DefaultValue", out var defaultValue))
			{
				builder.Write($", defaultValue: {CSharpCodeBuilder.GetConstantLiteral(defaultValue)}");
			}
			else if (TryGetNamedArgument(property.Value, "DefaultValueCallback", out var defaultValueCallback))
			{
				if (defaultValueCallback is { Type.SpecialType: SpecialType.System_String, Value: string { Length: > 0 } methodName })
				{
					builder.Write($", defaultValue: {methodName}()");
				}
			}
			if (TryGetNamedArgument(property.Value, "Coerce", out var coerceValue))
			{
				builder.Write($", coerce: {coerceValue.Value}");
			}
			if (TryGetNamedArgument(property.Value, "DefaultBindingMode", out var defaultBindingMode))
			{
				var bindingFqtn = ToFullQualifiedTypeName(defaultBindingMode);
				builder.Write($", defaultBindingMode: {bindingFqtn}");
			}
			if (TryGetNamedArgument(property.Value, "EnableDataValidation", out var enableDataValidation))
			{
				var valueFqtn = ToFullQualifiedTypeName(enableDataValidation);
				builder.Write($", enableDataValidation: {valueFqtn}");
			}
			if (TryGetNamedArgument(property.Value, "Inherits", out var inherits))
			{
				var valueFqtn = ToFullQualifiedTypeName(inherits);
				builder.Write($", inherits: {valueFqtn}");
			}
			if (TryGetNamedArgument(property.Value, "Validate", out var validate))
			{
				if (validate is { Type.SpecialType: SpecialType.System_String, Value: string { Length: > 0 } methodName })
				{
					builder.Write($", validate: {methodName}");
				}
			}
			builder.WriteLine(");");

			builder.IndentWrite($"{propertyAccess} {(property.Key.IsVirtual ? "virtual " : "")}");
			if (!property.Key.IsPartial)
			{
				DiagnosticReporter.ReportPropertyIsNotPartial(typeInfo.TypeSymbol, property.Key.PropertySymbol);
			}
			builder.Write("partial ");
			builder.WriteLine($"{memberTypeFullName} {member.Name}");
			builder.IndentWriteLine("{");
			builder.Indent++;
			builder.IndentWrite(string.IsNullOrWhiteSpace(getterAccessibility) ? "get => " : $"{getterAccessibility} get => ");
			builder.WriteLine($"GetValue({member.Name}Property);");
			builder.IndentWrite(string.IsNullOrWhiteSpace(setterAccessibility) ? "set => " : $"{setterAccessibility} set => ");
			builder.WriteLine($"SetValue({member.Name}Property, value);");
			builder.Indent--;
			builder.IndentWriteLine("}");
		}
	}

	private static bool TryGetNamedArgument(AttributeData attributeData, string name, out TypedConstant value)
	{
		foreach (var argument in attributeData.NamedArguments)
		{
			if (argument.Key != name)
			{
				continue;
			}

			value = argument.Value;
			return true;
		}

		value = default;
		return false;
	}

	#endregion
}