#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Weaver.Fody.PropertyShared;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public partial class WeaverForPropertyChanging
{
	#region Methods

	public void ProcessOnChangingMethods()
	{
		ProcessOnChangingMethods(NotifyNodes);
	}

	private IEnumerable<MethodReference> GetOnChangingMethods(TypeNode notifyNode)
	{
		var methods = notifyNode.TypeDefinition.Methods;

		return methods.Where(_ => !_.IsStatic &&
				!_.IsAbstract &&
				(_.Parameters.Count == 0) &&
				_.Name.StartsWith("On") &&
				_.Name.EndsWith("Changing"))
			.Select(methodDefinition =>
			{
				var typeDefinitions = new Stack<TypeDefinition>();
				typeDefinitions.Push(notifyNode.TypeDefinition);

				return GetMethodReference(typeDefinitions, methodDefinition);
			});
	}

	private void ProcessOnChangingMethods(List<TypeNode> notifyNodes)
	{
		foreach (var notifyNode in notifyNodes)
		{
			notifyNode.OnChangingMethods = GetOnChangingMethods(notifyNode).ToList();
			ProcessOnChangingMethods(notifyNode.Nodes);
		}
	}

	#endregion
}