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

	public static void ProcessAvalonia(CSharpCodeBuilder builder, SourceTypeInfo typeInfo)
	{
		ProcessAttachedProperties(builder, typeInfo);
		ProcessDirectProperties(builder, typeInfo);
		ProcessStyledProperties(builder, typeInfo);
	}

	/// <summary>
	/// Tries to get a given named argument value from an <see cref="AttributeData" /> instance, if present.
	/// </summary>
	/// <param name="attributeData"> The target <see cref="AttributeData" /> instance to check. </param>
	/// <param name="name"> The name of the argument to check. </param>
	/// <param name="value"> The resulting argument value, if present. </param>
	/// <returns> Whether <paramref name="attributeData" /> contains an argument named <paramref name="name" /> with a valid value. </returns>
	public static bool TryGetNamedArgument(AttributeData attributeData, string name, out TypedConstant value)
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

			// Static Attached Property
			builder.IndentWrite($"public static readonly global::Avalonia.AttachedProperty<{memberTypeFullName}> {member.Name}Property");
			builder.Write($" = global::Avalonia.AvaloniaProperty.RegisterAttached<{classFqtn}, {classFqtn}, {memberTypeFullName}>(nameof({member.Name})");
			if (TryGetNamedArgument(property.Value, "DefaultValue", out var defaultValue))
			{
				builder.Write($", defaultValue: {defaultValue.Value}");
			}
			if (TryGetNamedArgument(property.Value, "Inherits", out var inheritsValue))
			{
				builder.Write($", inheritsValue: {inheritsValue.Value}");
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

			// Static Direct Property
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

			// Property is not needed to be generated for direct properties.
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

			// Static Styled Property
			builder.IndentWrite($"public static readonly global::Avalonia.StyledProperty<{memberTypeFullName}> {member.Name}Property");
			builder.Write($" = global::Avalonia.AvaloniaProperty.Register<{classFqtn}, {memberTypeFullName}>(nameof({member.Name})");

			if (TryGetNamedArgument(property.Value, "DefaultValue", out var defaultValue))
			{
				builder.Write($", defaultValue: {defaultValue.Value}");
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

	#endregion
}