#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Weaver.Fody.PropertyChanged;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyShared;

public class TypeNode
{
	#region Fields

	public List<PropertyDefinition> AllProperties;
	public EventInvokerMethod EventInvoker;
	public MethodReference IsChangedInvoker;
	public MethodReference IsChangingInvoker;
	public List<MemberMapping> Mappings;
	public readonly List<TypeNode> Nodes;
	public List<OnChangedMethod> OnChangedMethods;
	public List<MethodReference> OnChangingMethods;
	public readonly List<PropertyData> PropertyData;
	public readonly List<PropertyDependency> PropertyDependencies;
	public TypeDefinition TypeDefinition;

	#endregion

	#region Constructors

	public TypeNode()
	{
		Mappings = [];
		Nodes = [];
		PropertyData = [];
		PropertyDependencies = [];
	}

	#endregion

	#region Properties

	public IEnumerable<PropertyDefinition> DeclaredProperties => AllProperties.Where(prop => prop.DeclaringType == TypeDefinition);

	#endregion
}