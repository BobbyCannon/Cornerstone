#region References

using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Fields

	private readonly HashSet<string> _assemblyLevelAttributeNames = [Constants.FilterTypeAttribute];

	private readonly HashSet<string> _typeLevelAttributeNames =
	[
		Constants.DoNotCheckEqualityAttribute,
		Constants.DoNotNotifyAttribute,
		Constants.DoNotSetChangedAttribute,
		Constants.AlsoNotifyForAttribute,
		Constants.DependsOnAttribute,
		Constants.AddINotifyPropertyChangedInterfaceAttribute,
		Constants.SuppressPropertyChangedWarningsAttribute,
		Constants.OnChangedMethodAttribute
	];

	#endregion

	#region Methods

	public void CleanAttributes()
	{
		foreach (var type in ModuleDefinition.GetTypes())
		{
			ProcessType(type);
		}

		ProcessAssembly();
	}

	private void ProcessAssembly()
	{
		var assembly = ModuleDefinition.Assembly;
		RemoveAttributes(assembly, _assemblyLevelAttributeNames);
	}

	private void ProcessType(TypeDefinition type)
	{
		RemoveAttributes(type, _typeLevelAttributeNames);

		foreach (var property in type.Properties)
		{
			RemoveAttributes(property, _typeLevelAttributeNames);
		}

		foreach (var field in type.Fields)
		{
			RemoveAttributes(field, _typeLevelAttributeNames);
		}

		foreach (var method in type.Methods)
		{
			RemoveAttributes(method, _typeLevelAttributeNames);
		}
	}

	private static void RemoveAttributes(ICustomAttributeProvider member, IEnumerable<string> attributeNames)
	{
		if (!member.HasCustomAttributes)
		{
			return;
		}

		var attributes = member.CustomAttributes
			.Where(attribute => attributeNames.Contains(attribute.Constructor.DeclaringType.FullName));

		foreach (var customAttribute in attributes.ToList())
		{
			member.CustomAttributes.Remove(customAttribute);
		}
	}

	#endregion
}