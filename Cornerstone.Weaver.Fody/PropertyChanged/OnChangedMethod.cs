#region References

using System.Collections.Generic;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public class OnChangedMethod
{
	#region Fields

	public string ArgumentTypeFullName;
	public bool IsDefaultMethod;
	public MethodDefinition MethodDefinition;
	public MethodReference MethodReference;
	public OnChangedTypes OnChangedType;
	public readonly List<PropertyDefinition> Properties;

	#endregion

	#region Constructors

	public OnChangedMethod()
	{
		Properties = new();
	}

	#endregion
}