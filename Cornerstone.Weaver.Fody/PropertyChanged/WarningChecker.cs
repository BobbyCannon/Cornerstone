﻿#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Weaver.Fody.PropertyShared;
using Mono.Cecil;
using Mono.Cecil.Cil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Methods

	public string CheckForWarning(PropertyData propertyData, InvokerTypes invokerType)
	{
		var propertyDefinition = propertyData.PropertyDefinition;
		var setMethod = propertyDefinition.SetMethod;

		if ((setMethod.Name == "set_Item") && (setMethod.Parameters.Count == 2) && (setMethod.Parameters[1].Name == "value"))
		{
			return "Property is an indexer.";
		}
		if (setMethod.Parameters.Count > 1)
		{
			return "Property takes more than one parameter.";
		}
		if (setMethod.IsAbstract)
		{
			return "Property is abstract.";
		}
		if ((propertyData.BackingFieldReference == null) && (propertyDefinition.GetMethod == null))
		{
			return "Property has no field set logic or it contains multiple sets and the names cannot be mapped to a property.";
		}
		if ((invokerType == InvokerTypes.BeforeAfter) && (propertyDefinition.GetMethod == null))
		{
			return "When using a before/after invoker the property have a 'get'.";
		}
		return null;
	}

	public void CheckForWarnings()
	{
		CheckForWarnings(NotifyNodes);
	}

	public void EmitConditionalWarning(ICustomAttributeProvider member, string message)
	{
		if (SuppressWarnings)
		{
			return;
		}

		const string suppressAttrName = Constants.SuppressPropertyChangedWarningsAttribute;

		if (member.HasCustomAttributes &&
			member.CustomAttributes.ContainsAttribute(suppressAttrName))
		{
			return;
		}

		if (member is IMemberDefinition memberDefinition)
		{
			var declaringType = memberDefinition.DeclaringType;
			if (declaringType.HasCustomAttributes &&
				declaringType.CustomAttributes.ContainsAttribute(suppressAttrName))
			{
				return;
			}
		}

		// Get the first sequence point of the method to get an approximate location for the warning
		SequencePoint sequencePoint;
		if (member is MethodDefinition method &&
			method.DebugInformation.HasSequencePoints)
		{
			sequencePoint = method.DebugInformation.SequencePoints.FirstOrDefault();
		}
		else
		{
			sequencePoint = null;
		}

		if (!message.EndsWith("."))
		{
			message += ".";
		}

		ModuleWeaver.WriteWarning($"{message} You can suppress this warning with [SuppressPropertyChangedWarnings].", sequencePoint);
	}

	public void EmitWarning(string message)
	{
		if (SuppressWarnings)
		{
			return;
		}

		ModuleWeaver.WriteWarning(message);
	}

	private void CheckForWarnings(List<TypeNode> notifyNodes)
	{
		foreach (var node in notifyNodes)
		{
			foreach (var propertyData in node.PropertyData.ToList())
			{
				var warning = CheckForWarning(propertyData, node.EventInvoker.InvokerType);
				if (warning == null)
				{
					continue;
				}

				EmitConditionalWarning(propertyData.PropertyDefinition,
					$"{propertyData.PropertyDefinition.GetName()} {warning} Property will be ignored."
				);
				node.PropertyData.Remove(propertyData);
			}
			CheckForWarnings(node.Nodes);
		}
	}

	#endregion
}