#region References

using System;
using System.Collections.Generic;
using Cornerstone.Weaver.Fody.FodyTools;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody;

public abstract class CornerstoneWeaver
{
	#region Constructors

	protected CornerstoneWeaver(ModuleWeaver moduleWeaver)
	{
		Definitions = new();
		ModuleDefinition = moduleWeaver.ModuleDefinition;
		ModuleWeaver = moduleWeaver;
		TypeSystem = moduleWeaver;
	}

	#endregion

	#region Properties

	public ModuleDefinition ModuleDefinition { get; }

	public ModuleWeaver ModuleWeaver { get; }

	public ITypeSystem TypeSystem { get; }

	protected Dictionary<string, TypeDefinition> Definitions { get; }

	#endregion

	#region Methods

	public static bool ConsumeAttribute(ICustomAttributeProvider attributeProvider, string attributeName)
	{
		var attribute = attributeProvider.GetAttribute(attributeName);

		if (attribute == null)
		{
			return false;
		}

		attributeProvider.CustomAttributes.Remove(attribute);
		return true;
	}

	protected bool HierarchyImplements(TypeReference typeReference, string interfaceName, IDictionary<string, bool> typesImplements)
	{
		return HierarchyImplements(this, typeReference, interfaceName, typesImplements);
	}

	public static bool HierarchyImplements(CornerstoneWeaver weaver, TypeReference typeReference, string interfaceName, IDictionary<string, bool> typesImplements)
	{
		var fullName = typeReference.FullName;
		if (typesImplements.TryGetValue(fullName, out var implementsINotify))
		{
			return implementsINotify;
		}

		TypeDefinition typeDefinition;
		if (typeReference.IsDefinition)
		{
			typeDefinition = (TypeDefinition) typeReference;
		}
		else
		{
			try
			{
				typeDefinition = weaver.Resolve(typeReference);
			}
			catch (Exception ex)
			{
				weaver.ModuleWeaver.WriteWarning($"Ignoring type {fullName} in type hierarchy => {ex.Message}");
				return false;
			}
		}

		foreach (var interfaceImplementation in typeDefinition.Interfaces)
		{
			if (interfaceImplementation.InterfaceType.Name == interfaceName)
			{
				typesImplements[fullName] = true;
				return true;
			}
		}

		var baseType = typeDefinition.BaseType;
		if (baseType == null)
		{
			typesImplements[fullName] = false;
			return false;
		}

		var baseTypeImplementsInterface = HierarchyImplements(weaver, baseType, interfaceName, typesImplements);
		typesImplements[fullName] = baseTypeImplementsInterface;
		return baseTypeImplementsInterface;
	}

	public abstract void Weave();

	protected TypeDefinition Resolve(TypeReference reference)
	{
		if (Definitions.TryGetValue(reference.FullName, out var definition))
		{
			return definition;
		}
		return Definitions[reference.FullName] = InnerResolve(reference);
	}

	private static TypeDefinition InnerResolve(TypeReference reference)
	{
		TypeDefinition result;

		try
		{
			result = reference.Resolve();
		}
		catch (Exception exception)
		{
			throw new($"Could not resolve '{reference.FullName}'.", exception);
		}

		if (result == null)
		{
			throw new($"Could not resolve '{reference.FullName}'. TypeReference.Resolve returned null.");
		}

		return result;
	}

	#endregion
}