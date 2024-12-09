#region References

using Mono.Cecil;
using Mono.Collections.Generic;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Fields

	private static readonly string AssemblyName = typeof(WeaverForPropertyChanged).Assembly.GetName().Name;
	private static readonly string AssemblyVersion = typeof(WeaverForPropertyChanged).Assembly.GetName().Version.ToString();

	#endregion

	#region Methods

	public void MarkAsGeneratedCode(Collection<CustomAttribute> customAttributes)
	{
		AddGeneratedCodeAttribute(customAttributes);
		AddDebuggerNonUserCodeAttribute(customAttributes);
	}

	private void AddDebuggerNonUserCodeAttribute(Collection<CustomAttribute> customAttributes)
	{
		var debuggerAttribute = new CustomAttribute(DebuggerNonUserCodeAttributeConstructor);
		customAttributes.Add(debuggerAttribute);
	}

	private void AddGeneratedCodeAttribute(Collection<CustomAttribute> customAttributes)
	{
		var attribute = new CustomAttribute(GeneratedCodeAttributeConstructor);
		attribute.ConstructorArguments.Add(new(ModuleWeaver.TypeSystem.StringReference, AssemblyName));
		attribute.ConstructorArguments.Add(new(ModuleWeaver.TypeSystem.StringReference, AssemblyVersion));
		customAttributes.Add(attribute);
	}

	#endregion
}