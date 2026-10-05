#region References

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using static Cornerstone.Generators.Generator;
using SourcePropertyInfo = Cornerstone.Generators.Models.SourcePropertyInfo;
using SourceTypeInfo = Cornerstone.Generators.Models.SourceTypeInfo;

#endregion

namespace Cornerstone.Generators.Processors;

internal sealed class PresentationProcessor : ITypeProcessor
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

	void ITypeProcessor.Initialize(Compilation compilation)
	{
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

			builder.IndentWrite($"public static readonly global::Cornerstone.Presentation.AttachedProperty<{memberTypeFullName}> {member.Name}Property");
			builder.Write($" = global::Cornerstone.Presentation.PresentationProperty.RegisterAttached<{classFqtn}, {classFqtn}, {memberTypeFullName}>(nameof({member.Name})");
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

	private static string GetViewModelPropertyName(AttributeData data, string memberName)
	{
		if (TryGetNamedArgument(data, "ViewModelProperty", out var named)
			&& named.Value is string { Length: > 0 } namedValue)
		{
			return namedValue;
		}

		if ((data.ConstructorArguments.Length > 0)
			&& data.ConstructorArguments[0].Value is string { Length: > 0 } constructorValue)
		{
			return constructorValue;
		}

		if (TryGetNamedArgument(data, "ForwardToViewModel", out var forward)
			&& forward.Value is true)
		{
			return memberName;
		}

		return null;
	}

	private static void ProcessDirectProperties(CSharpCodeBuilder builder, SourceTypeInfo typeInfo)
	{
		var directProperties = GetDirectProperties(typeInfo);
		var classFqtn = typeInfo.FullyGlobalQualifiedName;
		var forwarded = new List<(SourcePropertyInfo Member, string ViewModelProperty, string NotifiedField, string MemberTypeFullName)>();

		foreach (var property in directProperties)
		{
			var member = property.Key;
			var memberTypeFullName = member.GlobalFullyQualifiedName;
			var viewModelProperty = GetViewModelPropertyName(property.Value, member.Name);

			if (viewModelProperty != null)
			{
				if (!member.IsPartial)
				{
					DiagnosticReporter.ReportPropertyIsNotPartial(typeInfo.TypeSymbol, member.PropertySymbol);
					continue;
				}

				var notifiedField = $"{CalculateFieldName(member.PropertySymbol)}Notified";
				var (propertyAccess, getterAccessibility, setterAccessibility) = CalculateAccessibilities(member.PropertySymbol);

				builder.IndentWriteLine($"private {memberTypeFullName} {notifiedField};");

				builder.IndentWrite($"public static readonly global::Cornerstone.Presentation.DirectProperty<{classFqtn}, {memberTypeFullName}> {member.Name}Property");
				builder.Write($" = global::Cornerstone.Presentation.PresentationProperty.RegisterDirect<{classFqtn}, {memberTypeFullName}>(nameof({member.Name})");
				builder.Write($", getter: x => x.ViewModel.{viewModelProperty}");
				if (member.CanWrite)
				{
					builder.Write($", setter: (x, v) => x.{member.Name} = v");
				}
				builder.WriteLine(");");

				builder.IndentWrite($"{propertyAccess} {(member.IsVirtual ? "virtual " : "")}");
				builder.Write("partial ");
				builder.WriteLine($"{memberTypeFullName} {member.Name}");
				builder.IndentWriteLine("{");
				builder.Indent++;
				builder.IndentWrite(string.IsNullOrWhiteSpace(getterAccessibility) ? "get => " : $"{getterAccessibility} get => ");
				builder.WriteLine($"ViewModel.{viewModelProperty};");
				if (member.CanWrite)
				{
					builder.IndentWriteLine(string.IsNullOrWhiteSpace(setterAccessibility) ? "set" : $"{setterAccessibility} set");
					builder.IndentWriteLine("{");
					builder.Indent++;
					builder.IndentWriteLine($"var old = ViewModel.{viewModelProperty};");
					builder.IndentWriteLine($"if (global::System.Collections.Generic.EqualityComparer<{memberTypeFullName}>.Default.Equals(old, value))");
					builder.IndentWriteLine("{");
					builder.Indent++;
					builder.IndentWriteLine("return;");
					builder.Indent--;
					builder.IndentWriteLine("}");
					builder.IndentWriteLine($"ViewModel.{viewModelProperty} = value;");
					builder.IndentWriteLine($"RaisePropertyChanged({member.Name}Property, old, value);");
					builder.IndentWriteLine($"{notifiedField} = value;");
					builder.Indent--;
					builder.IndentWriteLine("}");
				}
				builder.Indent--;
				builder.IndentWriteLine("}");

				forwarded.Add((member, viewModelProperty, notifiedField, memberTypeFullName));
				continue;
			}

			// Partial auto-properties: field + SetAndRaise accessors (notifies bindings).
			// Non-partial: custom get/set body already on the type — register only (TextEditor, MarkdownView).
			if (member.IsPartial)
			{
				var fieldName = CalculateFieldName(member.PropertySymbol);
				var (propertyAccess, getterAccessibility, setterAccessibility) = CalculateAccessibilities(member.PropertySymbol);

				builder.IndentWriteLine($"private {memberTypeFullName} {fieldName};");

				builder.IndentWrite($"public static readonly global::Cornerstone.Presentation.DirectProperty<{classFqtn}, {memberTypeFullName}> {member.Name}Property");
				builder.Write($" = global::Cornerstone.Presentation.PresentationProperty.RegisterDirect<{classFqtn}, {memberTypeFullName}>(nameof({member.Name})");
				builder.Write($", getter: x => x.{fieldName}");
				if (member.CanWrite)
				{
					builder.Write($", setter: (x, v) => x.{member.Name} = v");
				}
				builder.WriteLine(");");

				builder.IndentWrite($"{propertyAccess} {(member.IsVirtual ? "virtual " : "")}");
				builder.Write("partial ");
				builder.WriteLine($"{memberTypeFullName} {member.Name}");
				builder.IndentWriteLine("{");
				builder.Indent++;
				builder.IndentWrite(string.IsNullOrWhiteSpace(getterAccessibility) ? "get => " : $"{getterAccessibility} get => ");
				builder.WriteLine($"{fieldName};");
				if (member.CanWrite)
				{
					builder.IndentWrite(string.IsNullOrWhiteSpace(setterAccessibility) ? "set => " : $"{setterAccessibility} set => ");
					builder.WriteLine($"SetAndRaise({member.Name}Property, ref {fieldName}, value);");
				}
				builder.Indent--;
				builder.IndentWriteLine("}");
				continue;
			}

			builder.IndentWrite($"public static readonly global::Cornerstone.Presentation.DirectProperty<{classFqtn}, {memberTypeFullName}> {member.Name}Property");
			builder.Write($" = global::Cornerstone.Presentation.PresentationProperty.RegisterDirect<{classFqtn}, {memberTypeFullName}>(nameof({member.Name})");

			if (member.CanRead)
			{
				builder.Write($", getter: x => x.{member.Name}");
			}

			if (member.CanWrite)
			{
				builder.Write($", setter: (x, v) => x.{member.Name} = v");
			}

			builder.WriteLine(");");
		}

		if (forwarded.Count == 0)
		{
			return;
		}

		builder.IndentWriteLine("private void SyncForwardedViewModelProperties(string propertyName)");
		builder.IndentWriteLine("{");
		builder.Indent++;
		builder.IndentWriteLine("switch (propertyName)");
		builder.IndentWriteLine("{");
		builder.Indent++;
		builder.IndentWriteLine("case null:");
		builder.IndentWriteLine("{");
		builder.Indent++;
		foreach (var item in forwarded)
		{
			builder.IndentWriteLine($"SyncForwarded{item.Member.Name}();");
		}
		builder.IndentWriteLine("break;");
		builder.Indent--;
		builder.IndentWriteLine("}");
		foreach (var item in forwarded)
		{
			builder.IndentWriteLine($"case \"{item.ViewModelProperty}\":");
			builder.IndentWriteLine("{");
			builder.Indent++;
			builder.IndentWriteLine($"SyncForwarded{item.Member.Name}();");
			builder.IndentWriteLine("break;");
			builder.Indent--;
			builder.IndentWriteLine("}");
		}
		builder.Indent--;
		builder.IndentWriteLine("}");
		builder.Indent--;
		builder.IndentWriteLine("}");

		foreach (var item in forwarded)
		{
			builder.IndentWriteLine($"private void SyncForwarded{item.Member.Name}()");
			builder.IndentWriteLine("{");
			builder.Indent++;
			builder.IndentWriteLine("var vm = ViewModel;");
			builder.IndentWriteLine("if (vm == null)");
			builder.IndentWriteLine("{");
			builder.Indent++;
			builder.IndentWriteLine("return;");
			builder.Indent--;
			builder.IndentWriteLine("}");
			builder.IndentWriteLine($"var value = vm.{item.ViewModelProperty};");
			builder.IndentWriteLine($"if (global::System.Collections.Generic.EqualityComparer<{item.MemberTypeFullName}>.Default.Equals({item.NotifiedField}, value))");
			builder.IndentWriteLine("{");
			builder.Indent++;
			builder.IndentWriteLine("return;");
			builder.Indent--;
			builder.IndentWriteLine("}");
			builder.IndentWriteLine($"RaisePropertyChanged({item.Member.Name}Property, {item.NotifiedField}, value);");
			builder.IndentWriteLine($"{item.NotifiedField} = value;");
			builder.Indent--;
			builder.IndentWriteLine("}");
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

			builder.IndentWrite($"public static readonly global::Cornerstone.Presentation.StyledProperty<{memberTypeFullName}> {member.Name}Property");
			builder.Write($" = global::Cornerstone.Presentation.PresentationProperty.Register<{classFqtn}, {memberTypeFullName}>(nameof({member.Name})");

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