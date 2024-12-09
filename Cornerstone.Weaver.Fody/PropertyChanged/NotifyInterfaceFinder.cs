#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Fields

	private readonly Dictionary<string, bool> _typesImplementingINotify = new();

	#endregion

	#region Methods

	public bool HierarchyImplementsINotify(TypeReference typeReference)
	{
		var fullName = typeReference.FullName;
		if (_typesImplementingINotify.TryGetValue(fullName, out var implementsINotify))
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
				typeDefinition = Resolve(typeReference);
			}
			catch (Exception ex)
			{
				EmitWarning($"Ignoring type {fullName} in type hierarchy => {ex.Message}");
				return false;
			}
		}

		foreach (var interfaceImplementation in typeDefinition.Interfaces)
		{
			if (interfaceImplementation.InterfaceType.Name == "INotifyPropertyChanged")
			{
				_typesImplementingINotify[fullName] = true;
				return true;
			}
		}

		var baseType = typeDefinition.BaseType;
		if (baseType == null)
		{
			_typesImplementingINotify[fullName] = false;
			return false;
		}

		var baseTypeImplementsINotify = HierarchyImplementsINotify(baseType);
		_typesImplementingINotify[fullName] = baseTypeImplementsINotify;
		return baseTypeImplementsINotify;
	}

	private static bool HasGeneratedPropertyChangedEvent(TypeDefinition typeDefinition)
	{
		if (!typeDefinition.HasEvents)
		{
			return false;
		}

		var evt = typeDefinition.Events.FirstOrDefault(i => i.Name == "PropertyChanged");
		return evt?.CustomAttributes.ContainsAttribute("System.CodeDom.Compiler.GeneratedCodeAttribute") is true;
	}

	#endregion
}