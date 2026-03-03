#region References

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Cornerstone.Generators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#endregion

#pragma warning disable RS1038 // This compiler extension should not be implemented in an assembly containing a reference to Microsoft.CodeAnalysis.Workspaces.

namespace Cornerstone.Generators;

[Generator(LanguageNames.CSharp)]
public partial class Generator : IIncrementalGenerator
{
	#region Constants

	public const string FullNameAlsoNotifyAttribute = "Cornerstone.Data.AlsoNotifyAttribute";
	public const string FullNameAttachedPropertyAttribute = "Cornerstone.Avalonia.AttachedPropertyAttribute";
	public const string FullNameDependencyInjectedPropertyAttribute = "Cornerstone.Runtime.DependencyInjectedPropertyAttribute";
	public const string FullNameDependencyInjectionConstructorAttribute = "Cornerstone.Runtime.DependencyInjectionConstructorAttribute";
	public const string FullNameDirectPropertyAttribute = "Cornerstone.Avalonia.DirectPropertyAttribute";
	public const string FullNameIComparable = "System.IComparable";
	public const string FullNameNotifyAttribute = "Cornerstone.Data.NotifyAttribute";
	public const string FullNamePackableAttribute = "Cornerstone.Serialization.PackableAttribute";
	public const string FullNamePackAttribute = "Cornerstone.Serialization.PackAttribute";
	public const string FullNameRelayCommand = "Cornerstone.Presentation.RelayCommand";
	public const string FullNameRelayCommandAttribute = "Cornerstone.Presentation.RelayCommandAttribute";
	public const string FullNameSourceReflectionAttribute = "Cornerstone.Reflection.SourceReflectionAttribute";
	public const string FullNameSourceReflectionTypeAttribute = "Cornerstone.Reflection.SourceReflectionTypeAttribute";
	public const string FullNameStyledPropertyAttribute = "Cornerstone.Avalonia.StyledPropertyAttribute";
	public const string FullNameUpdateableActionAttribute = "Cornerstone.Data.UpdateableActionAttribute";
	public const string FullNameUpdateableAttribute = "Cornerstone.Data.UpdateableAttribute";

	public const string GlobalIncludeExcludeSettings = "global::Cornerstone.Data.IncludeExcludeSettings";
	public const string GlobalReflectionExtensions = "global::Cornerstone.Extensions.ReflectionExtensions";
	public const string GlobalRelayCommand = "global::Cornerstone.Presentation.RelayCommand";
	public const string GlobalSourceAccessibility = "global::Cornerstone.Reflection.SourceAccessibility";
	public const string GlobalSourceAttributeInfo = "global::Cornerstone.Reflection.SourceAttributeInfo";
	public const string GlobalSourceConstructorInfo = "global::Cornerstone.Reflection.SourceConstructorInfo";
	public const string GlobalSourceFieldInfo = "global::Cornerstone.Reflection.SourceFieldInfo";
	public const string GlobalSourceInterfaceInfo = "global::Cornerstone.Reflection.SourceInterfaceInfo";
	public const string GlobalSourceMethodInfo = "global::Cornerstone.Reflection.SourceMethodInfo";
	public const string GlobalSourceNullableAnnotation = "global::Cornerstone.Reflection.SourceNullableAnnotation";
	public const string GlobalSourceParameterInfo = "global::Cornerstone.Reflection.SourceParameterInfo";
	public const string GlobalSourcePropertyInfo = "global::Cornerstone.Reflection.SourcePropertyInfo";
	public const string GlobalSourceReflector = "global::Cornerstone.Reflection.SourceReflector";
	public const string GlobalSourceTypeInfo = "global::Cornerstone.Reflection.SourceTypeInfo";
	public const string GlobalSystemArrayEmpty = "global::System.Array.Empty";
	public const string GlobalSystemHashSet = "global::System.Collections.Generic.HashSet";
	public const string GlobalSystemICommand = "global::System.Windows.Input.ICommand";
	public const string GlobalUpdateableAction = "global::Cornerstone.Data.UpdateableAction";
	public const string GlobalUpdateableActionAttribute = "global::Cornerstone.Data.UpdateableActionAttribute";
	public const string NameAttachedPropertyAttribute = "AttachedPropertyAttribute";

	public const string NameDependencyInjectedPropertyAttribute = "DependencyInjectedPropertyAttribute";
	public const string NameDependencyInjectionConstructorAttribute = "DependencyInjectionConstructorAttribute";
	public const string NameDirectPropertyAttribute = "DirectPropertyAttribute";
	public const string NameIComparable = "IComparable";
	public const string NameIComparableOfT = "IComparable`1";
	public const string NamePackableAttribute = "PackableAttribute";
	public const string NamePackAttribute = "PackAttribute";
	public const string NameRelayCommandAttribute = "RelayCommandAttribute";
	public const string NameSourceReflectionTypeAttribute = "SourceReflectionTypeAttribute";
	public const string NameStyledPropertyAttribute = "StyledPropertyAttribute";
	public const string UpdateableActionAttribute = "UpdateableActionAttribute";
	public const string UpdateableAttribute = "UpdateableAttribute";

	#endregion

	#region Fields

	private static readonly SymbolDisplayFormat _noGenericParameterTypeQualifiedNameFormat;
	private Dictionary<string, SourceTypeInfo> _typesLookup;
	private INamedTypeSymbol[] _typeSymbolsForCollectionDefinitions;

	#endregion

	#region Constructors

	static Generator()
	{
		_noGenericParameterTypeQualifiedNameFormat = new(
			genericsOptions: SymbolDisplayGenericsOptions.None,
			typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
		);
	}

	#endregion

	#region Methods

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var watch = Stopwatch.StartNew();
		var providers = Combine(
			GetSourceTypeInfoForAvalonia(context),
			GetSourceTypeInfoForComparable(context),
			GetSourceTypeInfoForPackable(context),
			GetSourceTypeInfoForPropertyChange(context),
			GetSourceTypeInfoForRelayCommand(context),
			GetSourceTypeInfoForSourceReflection(context),
			GetSourceTypeInfoForUnitTests(context),
			GetSourceTypeInfoForUpdateable(context)
		);

		var compilationAndClasses = providers
			.Select(static (array, _) =>
				array.Distinct(SourceTypeInfoByFullyQualifiedNameComparer.Instance)
					.ToImmutableArray()
			)
			.Combine(context.CompilationProvider);

		context.RegisterSourceOutput(compilationAndClasses, (spc, combined) =>
		{
			DiagnosticReporter.Initialize(spc);

			var (typesToProcess, compilation) = combined;

			_typesLookup = typesToProcess.ToDictionary(x => x.FullyGlobalQualifiedName, x => x);
			_typeSymbolsForCollectionDefinitions = new[]
			{
				compilation.GetTypeByMetadataName("Cornerstone.Collections.SpeedyList`1"),
				compilation.GetTypeByMetadataName("Cornerstone.Collections.ISpeedyList`1"),
				compilation.GetTypeByMetadataName("System.Array"),
				compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1"),
				compilation.GetTypeByMetadataName("System.Collections.Generic.Collection`1"),
				compilation.GetTypeByMetadataName("System.Collections.Generic.ICollection`1"),
				compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyCollection`1"),
				compilation.GetTypeByMetadataName("System.Collections.Generic.List`1"),
				compilation.GetTypeByMetadataName("System.Collections.Generic.IList`1")
			}.Where(s => s != null).ToArray();

			foreach (var typeInfo in typesToProcess)
			{
				var generatedSource = GenerateClassSource(typeInfo);
				if (string.IsNullOrWhiteSpace(generatedSource))
				{
					continue;
				}

				spc.AddSource($"{typeInfo.FullyQualifiedCodeName}.g.cs", generatedSource);
			}

			GenerateModuleInitializer(spc, typesToProcess);
			GenerateUnitTestMain(spc, compilation, typesToProcess);

			DiagnosticReporter.WriteLine($"Cornerstone: {watch.Elapsed}");
		});
	}

	public static string ToReflectionDisplayString(ITypeSymbol typeSymbol)
	{
		if (typeSymbol.Kind == SymbolKind.TypeParameter)
		{
			return typeSymbol.Name;
		}

		if (typeSymbol is IArrayTypeSymbol arrayType)
		{
			return $"{ToReflectionDisplayString(arrayType.ElementType)}[]";
		}

		if (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedType)
		{
			StringBuilder builder = new();

			builder.Append($"{namedType.ToDisplayString(_noGenericParameterTypeQualifiedNameFormat)}`{namedType.TypeParameters.Length}[");
			for (var i = 0; i < namedType.TypeArguments.Length; i++)
			{
				var argument = namedType.TypeArguments[i];
				builder.Append(ToReflectionDisplayString(argument));
				if (i < (namedType.TypeArguments.Length - 1))
				{
					builder.Append(',');
				}
			}
			builder.Append(']');
			return builder.ToString();
		}

		return typeSymbol.ToDisplayString(SymbolDisplayFormats.FullyQualifiedName);
	}

	public static bool TryGetEnumFieldName(ITypeSymbol symbol, object value, out string fieldName)
	{
		if (symbol.TypeKind is not TypeKind.Enum)
		{
			fieldName = null;

			return false;
		}

		// The default value of the enum is the value of its first constant field
		foreach (var memberSymbol in symbol.GetMembers())
		{
			if (memberSymbol is not IFieldSymbol { IsConst: true, ConstantValue: { } fieldValue } fieldSymbol)
			{
				continue;
			}

			if (fieldValue.Equals(value))
			{
				fieldName = fieldSymbol.Name;

				return true;
			}
		}
		fieldName = null;

		return false;
	}

	protected static string ToFullQualifiedTypeName(TypedConstant typeConstant)
	{
		if (typeConstant.IsNull)
		{
			return "null";
		}

		return (typeConstant.Kind, typeConstant.Value) switch
		{
			(TypedConstantKind.Primitive, string text) => text,
			(TypedConstantKind.Primitive, bool flag) => flag ? "true" : "false",
			(TypedConstantKind.Primitive, { } value) => value.ToString(),
			(TypedConstantKind.Type, ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			(TypedConstantKind.Enum, { } value) when TryGetEnumFieldName(typeConstant.Type!, value, out var fieldName)
				=> $"{typeConstant.Type!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{fieldName}",
			(TypedConstantKind.Enum, not null) => typeConstant.Type!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			_ => throw new ArgumentException("Invalid typed constant type")
		};
	}

	private static (string property, string getter, string setter) CalculateAccessibilities(IPropertySymbol notifiable)
	{
		var getter = notifiable.GetMethod?.DeclaredAccessibility ?? Accessibility.Private;
		var setter = notifiable.SetMethod?.DeclaredAccessibility ?? Accessibility.Private;
		var access = getter > setter ? getter : setter;
		return new(
			CSharpCodeBuilder.AccessibilityToString(access),
			getter < access ? CSharpCodeBuilder.AccessibilityToString(getter) : string.Empty,
			setter < access ? CSharpCodeBuilder.AccessibilityToString(setter) : string.Empty
		);
	}

	private static string CalculateFieldName(ISymbol member)
	{
		var name = member.Name;
		return $"_{char.ToLower(name[0])}{name.Substring(1)}";
	}

	private static string CalculatePropertyName(ISymbol member)
	{
		var name = member.Name;
		var removePrefixes = new[] { "_" };

		foreach (var removePrefix in removePrefixes)
		{
			if (name.StartsWith(removePrefix))
			{
				name = name.Substring(removePrefix.Length);
			}
		}

		name = $"{char.ToUpper(name[0])}{name.Substring(1)}";
		return name;
	}

	private static IncrementalValueProvider<ImmutableArray<SourceTypeInfo>> Combine(params IncrementalValueProvider<ImmutableArray<SourceTypeInfo>>[] providers)
	{
		var combined = providers[0];

		for (var i = 1; i < providers.Length; i++)
		{
			var next = providers[i];
			combined = combined.Combine(next).Select(static (pair, _) => pair.Left.AddRange(pair.Right));
		}

		return combined;
	}

	private static SourceParameterInfo[] CreateParameters(IMethodSymbol method)
	{
		return method.Parameters
			.Select(x => new SourceParameterInfo
			{
				Name = x.Name,
				IsOut = x.RefKind == RefKind.Out,
				IsParameterTypeRefLike = x.Type.IsRefLikeType,
				IsParameterTypePointer = x.Type.Kind == SymbolKind.PointerType,
				IsRef = x.RefKind == RefKind.Ref,
				IsTypeParameter = x.Type.Kind == SymbolKind.TypeParameter,
				HasNestedTypeParameter = x.Type.TypeKind == TypeKind.TypeParameter,
				NullableAnnotation = x.NullableAnnotation,
				ParameterType = x.Type.ToDisplayString(SymbolDisplayFormats.GlobalFullyQualifiedName),
				HasDefaultValue = x.HasExplicitDefaultValue,
				DefaultValue = (x.HasExplicitDefaultValue ? x.ExplicitDefaultValue : null)!,
				DisplayType = ToReflectionDisplayString(x.Type)
			})
			.ToArray();
	}

	private string GenerateClassSource(SourceTypeInfo typeInfo)
	{
		var builder = new CSharpCodeBuilder();
		builder.WriteAutoGeneratedComment();

		var typeNamespace = !typeInfo.TypeSymbol.ContainingNamespace.IsGlobalNamespace
			? typeInfo.TypeSymbol.ContainingNamespace.ToDisplayString()
			: null;

		if (typeNamespace != null)
		{
			builder.IndentWriteLine($"namespace {typeNamespace}");
			builder.IndentWriteLine("{");
			builder.IncreaseIndent();
		}

		foreach (var outerType in typeInfo.OuterTypes)
		{
			builder.IndentWriteLine($"partial {outerType}");
			builder.IndentWriteLine("{");
			builder.Indent++;
		}

		builder.StartType(typeInfo.TypeSymbol);
		var length = builder.Length;

		ProcessAvalonia(builder, typeInfo);
		ProcessComparable(builder, typeInfo);
		ProcessPackable(builder, typeInfo);
		ProcessNotifiable(builder, typeInfo);
		ProcessRelayCommands(builder, typeInfo);
		ProcessUpdateable(builder, typeInfo);

		if (length == builder.Length)
		{
			builder.Clear();
		}
		else
		{
			builder.EndType();

			for (var i = 0; i < typeInfo.OuterTypes.Count; i++)
			{
				builder.Indent--;
				builder.IndentWriteLine("}");
			}

			if (typeNamespace != null)
			{
				builder.DecreaseIndent();
				builder.IndentWriteLine("}");
			}
		}

		ProcessSourceReflection(builder, typeInfo);

		if (builder.Length <= 0)
		{
			return null;
		}

		builder.InsertWriteLine(0, "using System.Collections.Generic;");
		builder.InsertWriteLine(0, "using System;");
		
		var text = builder.ToString();
		Trace.WriteLine(text);
		return text;
	}

	private static int GetDepth(INamedTypeSymbol t)
	{
		var depth = 0;
		while (t != null)
		{
			depth++;
			t = t.BaseType;
		}
		return depth;
	}

	private static BaseTypeDeclarationSyntax GetEnclosingTypeDeclaration(SyntaxNode node)
	{
		while (node != null)
		{
			switch (node)
			{
				case EnumDeclarationSyntax sValue:
				{
					return sValue;
				}
				case TypeDeclarationSyntax sValue:
				{
					return sValue;
				}
				default:
				{
					node = node.Parent;
					break;
				}
			}
		}

		return null;
	}

	private IncrementalValueProvider<ImmutableArray<SourceTypeInfo>> GetSourceTypeInfoForAvalonia(IncrementalGeneratorInitializationContext context)
	{
		var attached = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				FullNameAttachedPropertyAttribute,
				static (node, _) => node is PropertyDeclarationSyntax,
				TransformType)
			.Where(static cls => cls is not null)
			.Collect();

		var direct = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				FullNameDirectPropertyAttribute,
				static (node, _) => node is PropertyDeclarationSyntax,
				TransformType)
			.Where(static cls => cls is not null)
			.Collect();

		var styled = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				FullNameStyledPropertyAttribute,
				static (node, _) => node is PropertyDeclarationSyntax,
				TransformType)
			.Where(static cls => cls is not null)
			.Collect();

		var combined = Combine(attached, direct, styled);
		return combined;
	}

	private IncrementalValueProvider<ImmutableArray<SourceTypeInfo>> GetSourceTypeInfoForComparable(IncrementalGeneratorInitializationContext context)
	{
		var comparableProvider = context.SyntaxProvider
			.CreateSyntaxProvider(
				static (node, _) =>
					node is ClassDeclarationSyntax
						or StructDeclarationSyntax
						or RecordDeclarationSyntax,
				static (ctx, ct) =>
				{
					if (ctx.Node is not TypeDeclarationSyntax typeDecl)
					{
						return null;
					}

					var symbol = ctx.SemanticModel.GetDeclaredSymbol(typeDecl, ct);
					if (symbol is null or { IsAbstract: true } or { IsStatic: true })
					{
						return null;
					}

					return ShouldImplementComparable(symbol)
						? ProcessTypeSymbol(symbol)
						: null;
				})
			.Where(static x => x is not null)
			.Collect();

		var combined = Combine(comparableProvider);
		return combined;
	}

	private IncrementalValueProvider<ImmutableArray<SourceTypeInfo>> GetSourceTypeInfoForPackable(IncrementalGeneratorInitializationContext context)
	{
		var pack = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				FullNamePackAttribute,
				static (node, _) => node is PropertyDeclarationSyntax,
				TransformType).Where(static cls => cls is not null).Collect();

		var combined = Combine(pack);
		return combined;
	}

	private IncrementalValueProvider<ImmutableArray<SourceTypeInfo>> GetSourceTypeInfoForPropertyChange(IncrementalGeneratorInitializationContext context)
	{
		var alsoNotify = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				FullNameAlsoNotifyAttribute,
				static (node, _) => node is PropertyDeclarationSyntax,
				TransformType).Where(static cls => cls is not null).Collect();

		var notify = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				FullNameNotifyAttribute,
				static (node, _) => node is PropertyDeclarationSyntax,
				TransformType).Where(static cls => cls is not null).Collect();

		var combined = Combine(alsoNotify, notify);
		return combined;
	}

	private IncrementalValueProvider<ImmutableArray<SourceTypeInfo>> GetSourceTypeInfoForRelayCommand(IncrementalGeneratorInitializationContext context)
	{
		var relayCommandProvider = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				FullNameRelayCommandAttribute,
				static (node, _) => node is MethodDeclarationSyntax,
				TransformType).Where(static cls => cls is not null).Collect();

		var combined = Combine(relayCommandProvider);
		return combined;
	}

	private IncrementalValueProvider<ImmutableArray<SourceTypeInfo>> GetSourceTypeInfoForSourceReflection(IncrementalGeneratorInitializationContext context)
	{
		var typeLevelProvider = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				FullNameSourceReflectionAttribute,
				IsInterestingType,
				TransformType).Where(static x => x is not null).Collect();

		var assemblyLevelProvider = context
			.CompilationProvider
			.Select(static (compilation, ct) =>
			{
				ct.ThrowIfCancellationRequested();

				var assembly = compilation.Assembly;
				var builder = ImmutableArray.CreateBuilder<SourceTypeInfo>();

				foreach (var attr in assembly.GetAttributes())
				{
					var attrClass = attr.AttributeClass;
					if (attrClass?.OriginalDefinition.Name != NameSourceReflectionTypeAttribute)
					{
						continue;
					}

					if (attrClass.TypeArguments.Length != 1)
					{
						continue;
					}

					if (attrClass.TypeArguments[0] is not INamedTypeSymbol targetType)
					{
						continue;
					}

					var info = ProcessTypeSymbol(targetType);
					if (info is null)
					{
						continue;
					}

					builder.Add(info);
				}

				return builder.ToImmutable();
			});

		var combined = Combine(typeLevelProvider, assemblyLevelProvider);
		return combined;
	}

	private IncrementalValueProvider<ImmutableArray<SourceTypeInfo>> GetSourceTypeInfoForUnitTests(IncrementalGeneratorInitializationContext context)
	{
		var mstestProvider = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				MSTestTestMethodAttributeFullName,
				static (node, _) => node is MethodDeclarationSyntax,
				TransformType).Where(static cls => cls is not null).Collect();

		var mstestInitializeProvider = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				MSTestTestInitializeAttributeFullName,
				static (node, _) => node is MethodDeclarationSyntax,
				TransformType).Where(static cls => cls is not null).Collect();

		var mstestCleanupProvider = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				MSTestTestCleanupAttributeFullName,
				static (node, _) => node is MethodDeclarationSyntax,
				TransformType).Where(static cls => cls is not null).Collect();

		var nunitProvider = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				NUnitTestAttributeFullName,
				static (node, _) => node is MethodDeclarationSyntax,
				TransformType).Where(static cls => cls is not null).Collect();

		var combined = Combine(mstestProvider, mstestInitializeProvider, mstestCleanupProvider, nunitProvider);
		return combined;
	}

	private IncrementalValueProvider<ImmutableArray<SourceTypeInfo>> GetSourceTypeInfoForUpdateable(IncrementalGeneratorInitializationContext context)
	{
		var updateableActionProvider = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				FullNameUpdateableActionAttribute,
				static (node, _) => node is PropertyDeclarationSyntax,
				TransformType).Where(static cls => cls is not null).Collect();

		var combined = Combine(updateableActionProvider);
		return combined;
	}

	private static bool HasTypeParameter(ITypeSymbol type)
	{
		if (type.Kind == SymbolKind.TypeParameter)
		{
			return true;
		}

		if (type is IArrayTypeSymbol array)
		{
			return HasTypeParameter(array.ElementType);
		}

		if (type is INamedTypeSymbol { IsGenericType: true } namedType)
		{
			foreach (var argument in namedType.TypeArguments)
			{
				if (HasTypeParameter(argument))
				{
					return true;
				}
			}
		}

		return false;
	}

	private bool IsInterestingType(SyntaxNode node, CancellationToken cancellationToken)
	{
		return node switch
		{
			ClassDeclarationSyntax
				or StructDeclarationSyntax
				or RecordDeclarationSyntax
				or EnumDeclarationSyntax => true,
			_ => false
		};
	}

	private static SourceTypeInfo ProcessTypeSymbol(INamedTypeSymbol typeSymbol)
	{
		var response = new SourceTypeInfo
		{
			Accessibility = typeSymbol.DeclaredAccessibility,
			BaseFullyGlobalQualifiedTypeName = typeSymbol.BaseType?.ToDisplayString(SymbolDisplayFormats.GlobalFullyQualifiedName)!,
			EnumUnderlyingType = typeSymbol.EnumUnderlyingType?.ToDisplayString(SymbolDisplayFormats.GlobalFullyQualifiedName)!,
			FullyQualifiedName = typeSymbol.ToDisplayString(SymbolDisplayFormats.FullyQualifiedName),
			FullyGlobalQualifiedName = typeSymbol.ToDisplayString(SymbolDisplayFormats.GlobalFullyQualifiedName),
			IsAbstract = typeSymbol.IsAbstract,
			IsClass = typeSymbol.TypeKind == TypeKind.Class,
			IsEnum = (typeSymbol.EnumUnderlyingType != null)
				|| (typeSymbol.TypeKind == TypeKind.Enum),
			IsGenericType = typeSymbol.IsGenericType,
			IsGenericTypeDefinition = typeSymbol.IsGenericType && (typeSymbol.TypeArguments[0].Kind == SymbolKind.TypeParameter),
			IsPartial = typeSymbol.DeclaringSyntaxReferences.Any(x =>
				x.GetSyntax() is TypeDeclarationSyntax syntax &&
				syntax.Modifiers.Any(SyntaxKind.PartialKeyword)),
			IsReadOnly = typeSymbol.IsReadOnly,
			IsRefLikeType = typeSymbol.IsRefLikeType,
			IsStatic = typeSymbol.IsStatic,
			IsStruct = typeSymbol.TypeKind == TypeKind.Struct,
			Name = typeSymbol.Name,
			TypeSymbol = typeSymbol
		};

		UpdateAttributes(response, typeSymbol);
		UpdateConstructors(response, typeSymbol);
		UpdateFields(response, typeSymbol);
		UpdateInterfaces(response, typeSymbol);
		UpdateMethods(response, typeSymbol);
		UpdateOuterTypes(response, typeSymbol);
		UpdateProperties(response, typeSymbol);

		return response;
	}

	private static bool RequiresOverride(IMethodSymbol methodSymbol)
	{
		if (methodSymbol
			is { IsAbstract: false }
			or { IsVirtual: true })
		{
			return true;
		}

		return methodSymbol is { IsAbstract: true }
			&& methodSymbol.ContainingType.IsAbstract
			&& (methodSymbol.ContainingType.TypeKind != TypeKind.Interface);
	}

	private SourceTypeInfo TransformType(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
	{
		// Find the enclosing class/struct/record/enum
		var typeDeclaration = GetEnclosingTypeDeclaration(ctx.TargetNode);
		if (typeDeclaration == null)
		{
			return null;
		}

		if (ModelExtensions.GetDeclaredSymbol(ctx.SemanticModel, typeDeclaration) is not INamedTypeSymbol typeSymbol)
		{
			return null;
		}

		var response = ProcessTypeSymbol(typeSymbol);
		return response;
	}

	private static void UpdateAttributes(SourceMemberInfo info, ISymbol symbol)
	{
		foreach (var attribute in symbol.GetAttributes())
		{
			if (attribute.AttributeClass == null)
			{
				continue;
			}

			var attributeInfo = new SourceAttributeInfo
			{
				ConstructorArguments = attribute.ConstructorArguments
					.SelectMany(x => x.Kind == TypedConstantKind.Array ? x.Values.Select(y => y.Value) : [x.Value])
					.ToArray(),
				Data = attribute,
				FullyGlobalQualifiedName = attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormats.GlobalFullyQualifiedName),
				FullyQualifiedName = attribute.AttributeClass?.ToDisplayString(SymbolDisplayFormats.FullyQualifiedName),
				Name = attribute.AttributeClass?.Name,
				NamedArguments = attribute.NamedArguments.ToDictionary(x => x.Key, x => x.Value.Value),
				TypeSymbol = attribute.AttributeClass
			};

			info.Attributes.Add(attributeInfo);
		}
	}

	private static void UpdateConstructors(SourceTypeInfo info, INamedTypeSymbol typeSymbol)
	{
		foreach (var constructor in typeSymbol.Constructors)
		{
			var constructorInfo = new SourceConstructorInfo
			{
				Name = constructor.Name,
				IsStatic = constructor.IsStatic,
				ContainingType = constructor.ContainingType,
				Accessibility = constructor.DeclaredAccessibility,
				IsDependencyConstructor = constructor
					.GetAttributes().Any(x => x.AttributeClass?.Name == NameDependencyInjectionConstructorAttribute),
				Parameters = CreateParameters(constructor)
			};

			UpdateAttributes(constructorInfo, typeSymbol);
			info.Constructors.Add(constructorInfo);
		}
	}

	private static void UpdateFields(SourceTypeInfo info, INamedTypeSymbol typeSymbol)
	{
		var members = typeSymbol.GetMembers().ToArray();
		var fields = members.OfType<IFieldSymbol>().ToArray();

		foreach (var field in fields)
		{
			var fieldInfo = new SourceFieldInfo
			{
				Accessibility = field.DeclaredAccessibility,
				Name = field.Name,
				NullableAnnotation = field.NullableAnnotation,
				IsRequired = field.IsRequired,
				IsConstant = field.IsConst,
				ConstantValue = field.ConstantValue!,
				IsStatic = field.IsStatic,
				IsReadOnly = field.IsReadOnly,

				//IsGenericDictionaryType = IsCompliantGenericDictionaryInterface(field.Type),
				//IsGenericEnumerableType = IsCompliantGenericEnumerableInterface(field.Type),

				FullyQualifiedTypeName = field.Type.ToDisplayString(SymbolDisplayFormats.FullyQualifiedName)
			};

			UpdateAttributes(fieldInfo, field);
			info.Fields.Add(fieldInfo);
		}
	}

	private static void UpdateInterfaces(SourceTypeInfo info, INamedTypeSymbol symbol)
	{
		foreach (var i in symbol.Interfaces)
		{
			var attributeInfo = new SourceInterfaceInfo
			{
				Name = i.Name,
				FullyQualifiedName = i.OriginalDefinition.ToDisplayString(SymbolDisplayFormats.FullyQualifiedName),
				FullyGlobalQualifiedName = i.OriginalDefinition.ToDisplayString(SymbolDisplayFormats.GlobalFullyQualifiedName)
			};

			info.Interfaces.Add(attributeInfo);
		}
	}

	private static void UpdateMethods(SourceTypeInfo info, INamedTypeSymbol typeSymbol)
	{
		var members = typeSymbol.GetMembers();
		var methods = members
			.OfType<IMethodSymbol>().Where(x =>
				(x.MethodKind == MethodKind.Ordinary)
				&& !x.IsImplicitlyDeclared
				&& !x.IsGenericMethod
			).ToArray();

		foreach (var method in methods)
		{
			var methodInfo = new SourceMethodInfo
			{
				Accessibility = method.DeclaredAccessibility,
				FullyQualifiedTypeName = method.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				IsAbstract = method.IsAbstract,
				IsGenericMethod = method.IsGenericMethod,
				IsOverride = method.IsOverride,
				IsPartial = method.IsPartialDefinition,
				IsStatic = method.IsStatic,
				IsVirtual = method.IsVirtual,
				Name = method.Name,
				Parameters = CreateParameters(method),
				ReturnNullableAnnotation = method.ReturnNullableAnnotation,
				ReturnType = method.ReturnsVoid
					? "void"
					: HasTypeParameter(method.ReturnType)
						? null
						: method.ReturnType.ToDisplayString(SymbolDisplayFormats.FullyQualifiedName),
				TypeParameters = method.TypeParameters.Select(x => new SourceTypeParameterInfo
				{
					Name = x.Name,
					HasUnmanagedTypeConstraint = x.HasUnmanagedTypeConstraint,
					HasValueTypeConstraint = x.HasValueTypeConstraint,
					HasTypeParameterInConstraintTypes = x.ConstraintTypes.Any(HasTypeParameter),
					ConstraintTypes = x.ConstraintTypes.Select(t => t.ToDisplayString(SymbolDisplayFormats.FullyQualifiedName)).ToArray()
				}).ToArray()
			};

			UpdateAttributes(methodInfo, method);
			info.Methods.Add(methodInfo);
		}
	}

	private static void UpdateOuterTypes(SourceTypeInfo info, INamedTypeSymbol typeSymbol)
	{
		info.OuterTypes.Clear();

		for (var outerType = typeSymbol.ContainingType; outerType != null; outerType = outerType.ContainingType)
		{
			info.OuterTypes.Add(outerType.ToDisplayString(SymbolDisplayFormats.TypeDeclaration));
		}

		info.OuterTypes.Reverse();
	}

	private static void UpdateProperties(SourceTypeInfo info, INamedTypeSymbol typeSymbol)
	{
		var members = typeSymbol.GetMembers();

		foreach (var property in members.OfType<IPropertySymbol>())
		{
			var propertyInfo = new SourcePropertyInfo
			{
				Accessibility = property.DeclaredAccessibility,
				CanRead = property.GetMethod != null,
				CanWrite = property.SetMethod != null,
				GetMethodAccessibility = property.GetMethod?.DeclaredAccessibility ?? Accessibility.NotApplicable,
				IsAbstract = property.IsAbstract,
				IsDependencyInjected = property.GetAttributes().Any(x => x.AttributeClass?.Name == NameDependencyInjectedPropertyAttribute),
				IsIndexer = property.IsIndexer,
				IsInitOnly = property.SetMethod?.IsInitOnly == true,
				IsPartial = property.IsPartialDefinition,
				IsRequired = property.IsRequired,
				IsReadOnly = property.IsReadOnly,
				IsStatic = property.IsStatic,
				IsVirtual = property.IsVirtual,
				FullyQualifiedName = property.Type.ToDisplayString(SymbolDisplayFormats.FullyQualifiedName),
				GlobalFullyQualifiedName = property.Type.ToDisplayString(SymbolDisplayFormats.GlobalFullyQualifiedName),
				Name = property.Name,
				PropertySymbol = property,
				SetMethodAccessibility = property.SetMethod?.DeclaredAccessibility ?? Accessibility.NotApplicable
			};
			propertyInfo.Parameters.AddRange(
				property.Parameters.Select(x =>
					new SourceParameterInfo
					{
						Name = x.Name,
						ParameterType = x.Type.ToDisplayString(SymbolDisplayFormats.FullyQualifiedName),
						ParameterSymbol = x.Type,
						NullableAnnotation = x.NullableAnnotation,
						HasDefaultValue = x.HasExplicitDefaultValue,
						HasNestedTypeParameter = x.Type.TypeKind == TypeKind.TypeParameter,
						DefaultValue = (x.HasExplicitDefaultValue ? x.ExplicitDefaultValue : null)!
					}));
			UpdateAttributes(propertyInfo, property);
			info.Properties.Add(propertyInfo);
		}
	}

	#endregion

	#region Classes

	private sealed class SourceTypeInfoByFullyQualifiedNameComparer : IEqualityComparer<SourceTypeInfo>
	{
		#region Fields

		public static readonly SourceTypeInfoByFullyQualifiedNameComparer Instance;

		#endregion

		#region Constructors

		static SourceTypeInfoByFullyQualifiedNameComparer()
		{
			Instance = new();
		}

		#endregion

		#region Methods

		public bool Equals(SourceTypeInfo x, SourceTypeInfo y)
		{
			return StringComparer.Ordinal.Equals(x?.FullyGlobalQualifiedName, y?.FullyGlobalQualifiedName);
		}

		public int GetHashCode(SourceTypeInfo obj)
		{
			return StringComparer.Ordinal.GetHashCode(obj.FullyGlobalQualifiedName ?? "");
		}

		#endregion
	}

	#endregion
}