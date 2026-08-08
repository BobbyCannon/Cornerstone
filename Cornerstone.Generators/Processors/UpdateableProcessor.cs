#region References

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cornerstone.Generators.Models;
using Microsoft.CodeAnalysis;
using static Cornerstone.Generators.Generator;

#endregion

namespace Cornerstone.Generators.Processors;

internal sealed class UpdateableProcessor : ITypeProcessor
{
	#region Constants

	private const string UpdateableGenericName = "IUpdateable`1";
	private const string UpdateableName = "IUpdateable";

	#endregion

	#region Properties

	public bool EmitsInsideTypeBlock => true;

	#endregion

	#region Methods

	public void Process(CSharpCodeBuilder builder, SourceTypeInfo sourceTypeInfo)
	{
		var updateableProperties = GetUpdateableProperties(sourceTypeInfo);
		if (updateableProperties.Count <= 0)
		{
			return;
		}

		GenerateUpdateableIncludedProperties(builder, updateableProperties);

		var processUpdateWith = false;
		var isDirectOrParentUpdateable = ImplementsIUpdateableDirectly(sourceTypeInfo.TypeSymbol)
			|| AnyParentIsUpdateable(sourceTypeInfo.TypeSymbol);

		// ---------------------------------------------------------------------
		// 1. Typed UpdateWith (for the type itself)
		// ---------------------------------------------------------------------
		if (isDirectOrParentUpdateable)
		{
			var isSealed = sourceTypeInfo.TypeSymbol.IsSealed;
			var shouldOverride = ShouldOverrideUpdateWith(sourceTypeInfo.TypeSymbol, sourceTypeInfo.TypeSymbol);
			var invokeBase = ShouldInvokeBase(sourceTypeInfo.TypeSymbol);

			var typeProperties = !invokeBase && (shouldOverride || (sourceTypeInfo.TypeSymbol.BaseType?.IsAbstract == true))
				? GetAllProperties(sourceTypeInfo.TypeSymbol).ToList()
				: GetProperties(sourceTypeInfo.TypeSymbol).ToList();

			var properties = invokeBase
				? updateableProperties.Where(p => typeProperties.Any(x => x.Name == p.Name)).ToArray()
				: updateableProperties;

			if (properties.Count > 0)
			{
				ProcessUpdateWith(
					builder,
					sourceTypeInfo.TypeSymbol,
					shouldOverride ? "override" : isSealed ? string.Empty : "virtual",
					invokeBase,
					properties);

				processUpdateWith = true;
			}
		}

		// ---------------------------------------------------------------------
		// 2. UpdatePropertyWith (single method for the type)
		// ---------------------------------------------------------------------
		if (isDirectOrParentUpdateable)
		{
			var isSealed = sourceTypeInfo.TypeSymbol.IsSealed;
			var shouldOverride = ShouldOverrideUpdatePropertyWith(sourceTypeInfo.TypeSymbol);

			// For UpdatePropertyWith we almost always want all accessible properties
			// and we rarely want to call base (unless the base actually has a virtual one).
			var properties = updateableProperties;

			if (properties.Count > 0)
			{
				ProcessUpdatePropertyWith(
					builder,
					shouldOverride ? "override" : isSealed ? string.Empty : "virtual",
					shouldOverride, // only call base if we are actually overriding
					properties);
			}
		}

		// ---------------------------------------------------------------------
		// 3. Typed UpdateWith for other IUpdateable<T> interfaces
		// ---------------------------------------------------------------------
		var updateables = GetOtherUpdateables(sourceTypeInfo.TypeSymbol);

		foreach (var updateable in updateables)
		{
			var type = updateable.Type;
			var shouldOverride = ShouldOverrideUpdateWith(sourceTypeInfo.TypeSymbol, type);
			var typeProperties = !shouldOverride || type.IsAbstract
				? GetAllProperties(type)
				: GetProperties(type);

			var properties = updateableProperties
				.Where(u => typeProperties.Any(p => p.Name == u.Name))
				.ToList();

			if (properties.Count > 0)
			{
				ProcessUpdateWith(
					builder,
					type,
					shouldOverride ? "override" : "virtual",
					shouldOverride,
					properties);
			}
		}

		// ---------------------------------------------------------------------
		// 4. object dispatcher
		// ---------------------------------------------------------------------
		if (processUpdateWith || (updateables.Length > 0))
		{
			ProcessUpdateWithForObject(builder,
				sourceTypeInfo.TypeSymbol,
				updateables.Select(x => x.Type).ToArray());
		}
	}

	private static bool AnyParentIsUpdateable(INamedTypeSymbol typeSymbol)
	{
		foreach (var current in GetBaseTypes(typeSymbol))
		{
			foreach (var typeSymbolInterface in current.Interfaces)
			{
				if (InterfaceImplementsIUpdateable(typeSymbolInterface, typeSymbol))
				{
					return true;
				}
			}
		}

		return false;
	}

	[SuppressMessage("ReSharper", "InconsistentNaming")]
	private static void GenerateUpdateableIncludedProperties(CSharpCodeBuilder builder, IList<UpdateablePropertyOrder> updateableProperties)
	{
		const int SyncIncomingAdd = 0b0000_0001;
		const int SyncIncomingUpdate = 0b0000_0010;
		const int SyncOutgoing = 0b0000_0100;
		const int UnwrapProxyEntity = 0b0000_1000;
		const int Updateable = 0b0001_0000;
		const int PropertyChangeTracking = 0b0010_0000;
		const int PartialUpdate = 0b0100_0000;

		var dictionary = new Dictionary<string, List<string>>();

		foreach (var member in updateableProperties)
		{
			var memberName = member.Name;
			var attributeValue = member.Action;

			TestMemberForTracking(attributeValue, SyncIncomingAdd, nameof(SyncIncomingAdd), dictionary, memberName);
			TestMemberForTracking(attributeValue, SyncIncomingUpdate, nameof(SyncIncomingUpdate), dictionary, memberName);
			TestMemberForTracking(attributeValue, SyncOutgoing, nameof(SyncOutgoing), dictionary, memberName);
			TestMemberForTracking(attributeValue, UnwrapProxyEntity, nameof(UnwrapProxyEntity), dictionary, memberName);
			TestMemberForTracking(attributeValue, Updateable, nameof(Updateable), dictionary, memberName);
			TestMemberForTracking(attributeValue, PropertyChangeTracking, nameof(PropertyChangeTracking), dictionary, memberName);
			TestMemberForTracking(attributeValue, PartialUpdate, nameof(PartialUpdate), dictionary, memberName);
		}

		if (dictionary.Count <= 0)
		{
			return;
		}

		builder.IndentWriteLine($"public override {GlobalSystemHashSet}<string> GetDefaultIncludedProperties({GlobalUpdateableAction} action)");
		builder.IndentWriteLine("{");
		builder.Indent++;
		builder.IndentWriteLine("var response = base.GetDefaultIncludedProperties(action);");
		builder.IndentWriteLine("switch (action)");
		builder.IndentWriteLine("{");
		builder.Indent++;

		var keysInOrder = new[]
		{
			nameof(SyncIncomingAdd),
			nameof(SyncIncomingUpdate),
			nameof(SyncOutgoing),
			nameof(UnwrapProxyEntity),
			nameof(Updateable),
			nameof(PropertyChangeTracking),
			nameof(PartialUpdate)
		};

		var groupedKeys = keysInOrder
			.Where(key => dictionary.ContainsKey(key) && (dictionary[key].Count > 0))
			.GroupBy(key => string.Join(",", dictionary[key].OrderBy(x => x)))
			.Select(g => (Keys: g.ToList(), Members: dictionary[g.First()].OrderBy(x => x).ToList())).ToList();

		foreach (var group in groupedKeys)
		{
			foreach (var key in group.Keys)
			{
				builder.IndentWriteLine($"case {GlobalUpdateableAction}.{key}:");
			}
			builder.IndentWriteLine("{");
			builder.Indent++;

			for (var index = 0; index < group.Members.Count; index++)
			{
				var member = group.Members[index];
				builder.IndentWrite("response.Add(\"");
				builder.Write(member);
				builder.WriteLine("\");");
			}

			builder.IndentWriteLine("break;");
			builder.Indent--;
			builder.IndentWriteLine("}");
		}

		builder.Indent--;
		builder.IndentWriteLine("}");
		builder.IndentWriteLine("return response;");
		builder.Indent--;
		builder.IndentWriteLine("}");
	}

	private static IList<IPropertySymbol> GetAllProperties(INamedTypeSymbol type)
	{
		return GetAllMembers(type)
			.OfType<IPropertySymbol>()
			.ToList();
	}

	private static IEnumerable<INamedTypeSymbol> GetBaseTypes(INamedTypeSymbol typeSymbol)
	{
		var current = typeSymbol.BaseType;

		while (current != null)
		{
			yield return current;
			current = current.BaseType;
		}
	}

	private static UpdateableDetails[] GetOtherUpdateables(INamedTypeSymbol typeSymbol)
	{
		var typeArguments = new List<UpdateableDetails>();
		var typeMemberNames = GetAllMembers(typeSymbol)
			.Where(x => x is IPropertySymbol)
			.Select(x => x.Name)
			.ToList();

		foreach (var implementedInterface in typeSymbol.Interfaces)
		{
			if ((implementedInterface.MetadataName != UpdateableGenericName) ||
				(implementedInterface.TypeArguments.Length != 1))
			{
				continue;
			}

			if (SymbolEqualityComparer.Default.Equals(implementedInterface.TypeArguments[0], typeSymbol))
			{
				continue;
			}

			if (implementedInterface.TypeArguments[0] is not INamedTypeSymbol typeArg)
			{
				continue;
			}

			var allMembers = GetAllMembers(typeArg)
				.GroupBy(x => x.Name)
				.Select(x => x.First());

			var propertyNames = allMembers
				.OfType<IPropertySymbol>()
				.Where(x => typeMemberNames.Contains(x.Name))
				.Distinct(SymbolEqualityComparer.Default)
				.ToArray();

			var hasUpdateWith = HasUpdateWithMethod(typeSymbol, typeArg);
			var found = typeArguments.FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.Type, typeSymbol));

			if ((found == null) && !hasUpdateWith)
			{
				typeArguments.Add(new UpdateableDetails { Type = typeArg, Members = propertyNames });
			}
		}

		return typeArguments.ToArray();
	}

	private static IEnumerable<IPropertySymbol> GetProperties(INamedTypeSymbol type)
	{
		return type
			.GetMembers()
			.Where(x => x is IPropertySymbol)
			.Cast<IPropertySymbol>();
	}

	private static IList<UpdateablePropertyOrder> GetUpdateableProperties(SourceTypeInfo typeInfo)
	{
		var allUpdateableProperties = new List<UpdateablePropertyOrder>();
		var typeAssignments = new Dictionary<string, (int Action, int Order)>();
		var hierarchy = new List<INamedTypeSymbol>();
		var current = typeInfo.TypeSymbol;
		while (current != null)
		{
			hierarchy.Add(current);
			current = current.BaseType;
		}
		hierarchy.Reverse();

		foreach (var type in hierarchy)
		{
			var attributes = type.GetAttributes()
				.Where(a => a.AttributeClass?.ToDisplayString() == FullNameUpdateableAttribute)
				.ToArray();

			foreach (var attribute in attributes)
			{
				var action = (int) attribute.ConstructorArguments[0].Value!;
				var actionPropertiesArg = attribute.ConstructorArguments[1];
				var inheritProperties = (attribute.ConstructorArguments.Length > 2)
					&& (bool) attribute.ConstructorArguments[2].Value!;

				var actionProperties = actionPropertiesArg.Kind == TypedConstantKind.Array
					? actionPropertiesArg.Values.Select(x => (string) x.Value!).ToArray()
					: ["*"];

				if ((actionProperties.Length == 1)
					&& Equals(actionProperties[0], "*"))
				{
					var candidates = inheritProperties
						? GetAllMembers(type).OfType<IPropertySymbol>()
						: type.GetMembers().OfType<IPropertySymbol>();

					foreach (var p in candidates)
					{
						if (p.IsIndexer || !p.CanBeReferencedByName)
						{
							continue;
						}

						if (!typeAssignments.ContainsKey(p.Name))
						{
							typeAssignments[p.Name] = (action, 0);
						}
					}
				}
				else
				{
					for (var index = 0; index < actionProperties.Length; index++)
					{
						var p = actionProperties[index];
						typeAssignments[p] = (action, index);
					}
				}
			}
		}

		// Now collect final properties that exist on the target type
		var allProperties = GetAllMembers(typeInfo.TypeSymbol)
			.OfType<IPropertySymbol>()
			.GroupBy(x => x.Name)
			.Select(x => x.First())
			.ToArray();

		foreach (var property in allProperties)
		{
			var canAccess = !property.IsIndexer && property.CanBeReferencedByName;
			var canWrite = property.SetMethod?.DeclaredAccessibility == Accessibility.Public;

			// Property-level attribute takes precedence
			var propAttribute = property.GetAttributes()
				.FirstOrDefault(a => a.AttributeClass?.MetadataName == UpdateableActionAttribute);

			if (propAttribute != null)
			{
				allUpdateableProperties.Add(new UpdateablePropertyOrder
				{
					Action = (int) propAttribute.ConstructorArguments[0].Value!,
					CanAccess = canAccess,
					CanWrite = canWrite,
					Name = property.Name,
					Order = (int) propAttribute.ConstructorArguments[1].Value!,
					PropertySymbol = property
				});
			}
			else if (typeAssignments.TryGetValue(property.Name, out var values))
			{
				allUpdateableProperties.Add(new UpdateablePropertyOrder
				{
					Action = values.Action,
					CanAccess = canAccess,
					CanWrite = canWrite,
					Name = property.Name,
					Order = values.Order,
					PropertySymbol = property
				});
			}
		}

		return allUpdateableProperties
			.OrderBy(x => x.Order)
			.ThenBy(x => x.Name)
			.ToList();
	}

	private static bool HasUpdateWithMethod(INamedTypeSymbol typeSymbol, INamedTypeSymbol typeArg)
	{
		var currentType = typeSymbol;
		while (currentType != null)
		{
			if (currentType.GetMembers("UpdateWith")
				.OfType<IMethodSymbol>().Any(m => (m.Parameters.Length == 2)
					&& SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, typeArg)
					&& (m.Parameters[1].Type.Name == "IncludeExcludeSettings")
					&& (m.ReturnType.SpecialType == SpecialType.System_Boolean)
					&& !m.IsAbstract))
			{
				return true;
			}
			currentType = currentType.BaseType;
		}

		return false;
	}

	private static bool ImplementsIUpdateableDirectly(INamedTypeSymbol typeSymbol)
	{
		if (typeSymbol == null)
		{
			return false;
		}

		foreach (var typeSymbolInterface in typeSymbol.Interfaces)
		{
			if (InterfaceImplementsIUpdateable(typeSymbolInterface, typeSymbol))
			{
				return true;
			}
		}

		return false;
	}

	void ITypeProcessor.Initialize(Compilation compilation)
	{
	}

	private static bool InterfaceImplementsIUpdateable(INamedTypeSymbol interfaceSymbol, INamedTypeSymbol typeSymbol)
	{
		if (interfaceSymbol.MetadataName is UpdateableName)
		{
			return true;
		}

		if (interfaceSymbol.MetadataName is UpdateableGenericName
			&& (interfaceSymbol.TypeArguments.Length == 1))
		{
			if (SymbolEqualityComparer.Default.Equals(interfaceSymbol.TypeArguments[0], typeSymbol))
			{
				return true;
			}
		}

		return false;
	}

	private static void ProcessUpdatePropertyWith(CSharpCodeBuilder builder, string methodModifier, bool invokeBase, IList<UpdateablePropertyOrder> members)
	{
		builder.IndentWriteLine("");
		builder.IndentWrite("public ");
		if (!string.IsNullOrWhiteSpace(methodModifier))
		{
			builder.Write($"{methodModifier} ");
		}
		builder.WriteLine("bool UpdatePropertyWith(string propertyName, object value)");
		builder.IndentWriteLine("{");
		builder.IncreaseIndent();

		builder.IndentWriteLine("return propertyName switch");
		builder.IndentWriteLine("{");
		builder.IncreaseIndent();

		foreach (var member in members)
		{
			if (!member.CanAccess)
			{
				continue;
			}

			var propertyType = member.PropertySymbol.Type;
			var fullyQualifiedType = propertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

			// Collection case
			if (propertyType is INamedTypeSymbol
				{
					IsGenericType: true,
					OriginalDefinition.Name: "List"
					or "IList"
					or "PresentationList"
					or "IPresentationList"
					or "SpeedyList"
				} namedType)
			{
				var elementType = namedType.TypeArguments[0];
				var globalPropertyType = propertyType.ToDisplayString(SymbolDisplayFormats.GlobalFullyQualifiedName);
				var globalElementType = elementType.ToDisplayString(SymbolDisplayFormats.GlobalFullyQualifiedName);

				builder.IndentWriteLine($"nameof({member.Name}) => TryUpdateProperty<{globalPropertyType}, {globalElementType}>({member.Name}, ({globalPropertyType})value, true),");
				continue;
			}

			// Normal writable property
			if (member.CanWrite)
			{
				builder.IndentWriteLine($"nameof({member.Name}) => TryUpdateProperty({member.Name}, ({fullyQualifiedType})value, true, x => {member.Name} = x),");
			}
		}

		// Fallback
		builder.IndentWriteLine(invokeBase
			? "_ => base.UpdatePropertyWith(propertyName, value)"
			: "_ => false");

		builder.DecreaseIndent();
		builder.IndentWriteLine("};");
		builder.DecreaseIndent();
		builder.IndentWriteLine("}");
	}

	private static void ProcessUpdateWith(CSharpCodeBuilder builder, INamedTypeSymbol classType, string methodModifier, bool invokeBase, IList<UpdateablePropertyOrder> members)
	{
		var fullyQualifiedTypeName = classType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

		builder.IndentWriteLine("");
		builder.IndentWrite("public ");
		if (!string.IsNullOrWhiteSpace(methodModifier))
		{
			builder.Write($"{methodModifier} ");
		}
		builder.WriteLine($"bool UpdateWith({fullyQualifiedTypeName} update, {GlobalIncludeExcludeSettings} settings)");
		builder.IndentWriteLine("{");
		builder.IncreaseIndent();

		builder.IndentWriteLine("if (update == null)");
		builder.IndentWriteLine("{");
		builder.IncreaseIndent();
		builder.IndentWriteLine("return false;");
		builder.DecreaseIndent();
		builder.IndentWriteLine("}");

		foreach (var member in members)
		{
			if (!member.CanAccess)
			{
				continue;
			}

			// Check if the property type is a generic collection
			var propertyType = member.PropertySymbol.Type;
			if (propertyType is INamedTypeSymbol
				{
					IsGenericType: true,
					OriginalDefinition.Name: "List"
					or "IList"
					or "PresentationList"
					or "IPresentationList"
					or "SpeedyList"
				} namedType)
			{
				var elementType = namedType.TypeArguments[0];
				var globalPropertyType = propertyType.ToDisplayString(SymbolDisplayFormats.GlobalFullyQualifiedName);
				var globalElementType = elementType.ToDisplayString(SymbolDisplayFormats.GlobalFullyQualifiedName);
				builder.IndentWriteLine($"TryUpdateProperty<{globalPropertyType}, {globalElementType}>({member.Name}, update.{member.Name}, settings.ShouldProcessProperty(nameof({member.Name})));");
				continue;
			}

			if (member.CanWrite)
			{
				builder.IndentWriteLine($"TryUpdateProperty({member.Name}, update.{member.Name}, settings.ShouldProcessProperty(nameof({member.Name})), x => {member.Name} = x);");

				//$"TryUpdateProperty({member.Name}, update.{member.Name}, settings.ShouldProcessProperty(nameof({member.Name})));"
			}
		}

		builder.IndentWriteLine(invokeBase ? "return base.UpdateWith(update, settings);" : "return true;");
		builder.DecreaseIndent();
		builder.IndentWriteLine("}");
	}

	private static void ProcessUpdateWithForObject(CSharpCodeBuilder builder, INamedTypeSymbol classInfo, IEnumerable<INamedTypeSymbol> updateables)
	{
		builder.IndentWriteLine("");
		builder.IndentWriteLine($"public override bool UpdateWith(object update, {GlobalIncludeExcludeSettings} settings)");
		builder.IndentWriteLine("{");
		builder.Indent++;

		builder.IndentWriteLine("return update switch");
		builder.IndentWriteLine("{");
		builder.Indent++;

		var allUpdateables = updateables.ToList();
		allUpdateables.Add(classInfo);
		allUpdateables = allUpdateables.OrderByDescending(GetDepth).ToList();

		foreach (var other in allUpdateables)
		{
			builder.IndentWriteLine($"{other.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} value => UpdateWith(value, settings),");
		}

		builder.IndentWriteLine("_ => base.UpdateWith(update, settings)");
		builder.Indent--;
		builder.IndentWriteLine("};");
		builder.Indent--;
		builder.IndentWriteLine("}");
	}

	private static bool ShouldInvokeBase(INamedTypeSymbol typeSymbol)
	{
		foreach (var current in GetBaseTypes(typeSymbol))
		{
			if (current.IsAbstract)
			{
				return false;
			}

			if (ImplementsIUpdateableDirectly(current))
			{
				var attribute = typeSymbol
					.GetAttributes()
					.FirstOrDefault(x =>
						x.AttributeClass?.Name == nameof(FullNameUpdateableAttribute)
					);

				if (attribute != null)
				{
					return attribute
						.NamedArguments
						.Any(a => a is
						{
							Key: "InvokeBase",
							Value.Value: true
						});
				}
			}
		}

		return false;
	}

	private static bool ShouldOverrideUpdatePropertyWith(INamedTypeSymbol typeSymbol)
	{
		foreach (var current in GetBaseTypes(typeSymbol))
		{
			var methods = current
				.GetMembers("UpdatePropertyWith")
				.OfType<IMethodSymbol>()
				.Where(m =>
					(m.MethodKind == MethodKind.Ordinary) &&
					m is
					{
						DeclaredAccessibility: Accessibility.Public,
						IsStatic: false,
						ReturnType.SpecialType: SpecialType.System_Boolean,
						Parameters.Length: 2
					} &&
					(m.Parameters[0].Type.SpecialType == SpecialType.System_String) &&
					(m.Parameters[1].Type.SpecialType == SpecialType.System_Object));

			if (methods.Any(x => x.IsAbstract || x.IsVirtual || x.IsOverride))
			{
				return true;
			}
		}

		return false;
	}

	private static bool ShouldOverrideUpdateWith(INamedTypeSymbol typeSymbol, INamedTypeSymbol parameterType)
	{
		foreach (var current in GetBaseTypes(typeSymbol))
		{
			var methods = current
				.GetMembers("UpdateWith")
				.OfType<IMethodSymbol>()
				.Where(m =>
					(m.MethodKind == MethodKind.Ordinary)
					&& m is
					{
						DeclaredAccessibility: Accessibility.Public,
						IsStatic: false,
						ReturnType.SpecialType: SpecialType.System_Boolean,
						Parameters.Length: >= 1
					}
					&& SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, parameterType)
				).ToList();

			if (methods.Any(x => x.IsAbstract || x.IsVirtual || x.IsOverride))
			{
				return true;
			}

			var updateables = GetOtherUpdateables(current);
			if (updateables.Any(x => SymbolEqualityComparer.Default.Equals(x.Type, parameterType)))
			{
				return true;
			}
		}

		return false;
	}

	private static void TestMemberForTracking(int attributeValue, int flagValue, string name, Dictionary<string, List<string>> dictionary, string value)
	{
		if ((attributeValue & flagValue) != flagValue)
		{
			return;
		}

		if (dictionary.TryGetValue(name, out var existingList))
		{
			existingList.Add(value);
		}
		else
		{
			dictionary.Add(name, [value]);
		}
	}

	#endregion

	#region Classes

	public class UpdateableDetails
	{
		#region Fields

		public ISymbol[] Members;
		public INamedTypeSymbol Type;

		#endregion
	}

	#endregion
}