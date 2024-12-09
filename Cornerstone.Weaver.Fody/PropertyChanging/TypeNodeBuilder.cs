#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Weaver.Fody.PropertyShared;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public partial class WeaverForPropertyChanging
{
	#region Fields

	private List<TypeDefinition> _allClasses;

	#endregion

	#region Methods

	public void BuildTypeNodes()
	{
		_allClasses = ModuleDefinition
			.GetTypes()
			.Where(_ => _.IsClass &&
				(_.BaseType != null))
			.ToList();
		
		while (_allClasses.FirstOrDefault() is { } typeDefinition)
		{
			AddClass(typeDefinition);
		}

		PopulateINotifyNodes(Nodes);
		foreach (var notifyNode in NotifyNodes)
		{
			Nodes.Remove(notifyNode);
		}
		PopulateInjectedINotifyNodes(Nodes);
	}

	private TypeNode AddClass(TypeDefinition typeDefinition)
	{
		_allClasses.Remove(typeDefinition);
		var typeNode = new TypeNode
		{
			TypeDefinition = typeDefinition
		};
		if (typeDefinition.BaseType.Scope.Name != ModuleDefinition.Name)
		{
			Nodes.Add(typeNode);
		}
		else
		{
			var baseType = Resolve(typeDefinition.BaseType);
			var parentNode = FindClassNode(baseType, Nodes);
			if (parentNode == null)
			{
				parentNode = AddClass(baseType);
			}
			parentNode.Nodes.Add(typeNode);
		}
		return typeNode;
	}

	private static TypeNode FindClassNode(TypeDefinition type, IEnumerable<TypeNode> typeNode)
	{
		foreach (var node in typeNode)
		{
			if (type == node.TypeDefinition)
			{
				return node;
			}
			var findNode = FindClassNode(type, node.Nodes);
			if (findNode != null)
			{
				return findNode;
			}
		}
		return null;
	}

	private static bool HasNotifyPropertyChangingAttribute(TypeDefinition typeDefinition)
	{
		return typeDefinition.CustomAttributes.ContainsAttribute(Constants.ImplementPropertyChangingAttribute);
	}

	private void PopulateInjectedINotifyNodes(List<TypeNode> typeNodes)
	{
		foreach (var node in typeNodes)
		{
			if (HasNotifyPropertyChangingAttribute(node.TypeDefinition))
			{
				InjectINotifyPropertyChangingInterface(node.TypeDefinition);
				NotifyNodes.Add(node);
				continue;
			}
			PopulateInjectedINotifyNodes(node.Nodes);
		}
	}

	private void PopulateINotifyNodes(List<TypeNode> typeNodes)
	{
		foreach (var node in typeNodes)
		{
			if (HierarchyImplementsINotify(node.TypeDefinition))
			{
				NotifyNodes.Add(node);
				continue;
			}
			PopulateINotifyNodes(node.Nodes);
		}
	}

	#endregion
}