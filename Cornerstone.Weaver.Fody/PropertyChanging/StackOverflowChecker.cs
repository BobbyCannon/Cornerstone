#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Weaver.Fody.PropertyShared;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public partial class WeaverForPropertyChanging
{
	#region Methods

	public void CheckForStackOverflow()
	{
		CheckForStackOverflow(NotifyNodes);
	}

	public bool CheckIfGetterCallsSetter(PropertyDefinition propertyDefinition)
	{
		if (propertyDefinition.GetMethod != null)
		{
			var instructions = propertyDefinition.GetMethod.Body.Instructions;
			foreach (var instruction in instructions)
			{
				if ((instruction.OpCode == OpCodes.Call)
					&& (instruction.Operand == propertyDefinition.SetMethod))
				{
					return true;
				}
			}
		}

		return false;
	}

	public bool CheckIfGetterCallsVirtualBaseSetter(PropertyDefinition propertyDefinition)
	{
		if (propertyDefinition.SetMethod.IsVirtual)
		{
			var baseType = Resolve(propertyDefinition.DeclaringType.BaseType);
			var baseProperty = baseType.Properties.FirstOrDefault(_ => _.Name == propertyDefinition.Name);

			if ((baseProperty != null) && (propertyDefinition.GetMethod != null))
			{
				var instructions = propertyDefinition.GetMethod.Body.Instructions;
				foreach (var instruction in instructions)
				{
					if (instruction.OpCode != OpCodes.Call)
					{
						continue;
					}

					if (instruction.Operand is not MethodReference operand)
					{
						continue;
					}
					if (operand.FullName == baseProperty.SetMethod.FullName)
					{
						return true;
					}
				}
			}
		}

		return false;
	}

	private void CheckForStackOverflow(IEnumerable<TypeNode> notifyNodes)
	{
		foreach (var node in notifyNodes)
		{
			foreach (var propertyData in node.PropertyData)
			{
				if (node.EventInvoker.InvokerType == InvokerTypes.Before)
				{
					if (CheckIfGetterCallsSetter(propertyData.PropertyDefinition))
					{
						throw new WeavingException($"{propertyData.PropertyDefinition.GetName()} Getter calls setter which will cause a stack overflow as the setter uses the getter for obtaining the before values.");
					}

					if (CheckIfGetterCallsVirtualBaseSetter(propertyData.PropertyDefinition))
					{
						throw new WeavingException($"{propertyData.PropertyDefinition.GetName()} Getter of calls virtual setter of base class which will cause a stack overflow as the setter uses the getter for obtaining the before values.");
					}
				}
			}

			CheckForStackOverflow(node.Nodes);
		}
	}

	#endregion
}