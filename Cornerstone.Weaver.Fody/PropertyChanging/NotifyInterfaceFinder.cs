#region References

using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public partial class WeaverForPropertyChanging
{
	#region Fields

	private readonly Dictionary<string, bool> _typeReferencesImplementingINotify = new();

	#endregion

	#region Methods

	public static bool HasPropertyChangingEvent(TypeDefinition typeDefinition)
	{
		return typeDefinition.Events.Any(IsPropertyChangingEvent);
	}

	public bool HierarchyImplementsINotify(TypeReference typeReference)
	{
		var fullName = typeReference.FullName;
		if (_typeReferencesImplementingINotify.TryGetValue(fullName, out var implementsINotify))
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
			typeDefinition = Resolve(typeReference);
		}

		if (HasPropertyChangingEvent(typeDefinition))
		{
			_typeReferencesImplementingINotify[fullName] = true;
			return true;
		}
		if (HasPropertyChangingField(typeDefinition))
		{
			_typeReferencesImplementingINotify[fullName] = true;
			return true;
		}
		var baseType = typeDefinition.BaseType;
		if (baseType == null)
		{
			_typeReferencesImplementingINotify[fullName] = false;
			return false;
		}
		var baseTypeImplementsINotify = HierarchyImplementsINotify(baseType);
		_typeReferencesImplementingINotify[fullName] = baseTypeImplementsINotify;
		return baseTypeImplementsINotify;
	}

	public static bool IsPropertyChangingEvent(EventDefinition eventDefinition)
	{
		return IsNamedPropertyChanging(eventDefinition) && IsPropertyChangingEventHandler(eventDefinition.EventType);
	}

	public static bool IsPropertyChangingEventHandler(TypeReference typeReference)
	{
		return typeReference.FullName is
			"System.ComponentModel.PropertyChangingEventHandler" or
			"Windows.UI.Xaml.Data.PropertyChangingEventHandler" or
			"System.Runtime.InteropServices.WindowsRuntime.EventRegistrationTokenTable`1<Windows.UI.Xaml.Data.PropertyChangingEventHandler>";
	}

	private static bool HasPropertyChangingField(TypeDefinition typeDefinition)
	{
		foreach (var fieldType in typeDefinition.Fields.Select(_ => _.FieldType))
		{
			if (fieldType.FullName == "Microsoft.FSharp.Control.FSharpEvent`2<System.ComponentModel.PropertyChangingEventHandler,System.ComponentModel.PropertyChangingEventArgs>")
			{
				return true;
			}
			if (fieldType.FullName == "Microsoft.FSharp.Control.FSharpEvent`2<Windows.UI.Xaml.Data.PropertyChangingEventHandler,Windows.UI.Xaml.Data.PropertyChangingEventArgs>")
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsNamedPropertyChanging(EventDefinition eventDefinition)
	{
		return eventDefinition.Name is
			"PropertyChanging" or
			"System.ComponentModel.INotifyPropertyChanging.PropertyChanging" or
			"Windows.UI.Xaml.Data.PropertyChanging";
	}

	#endregion
}