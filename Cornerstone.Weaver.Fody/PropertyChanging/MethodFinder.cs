﻿#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Weaver.Fody.PropertyShared;
using Fody;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public partial class WeaverForPropertyChanging
{
	#region Methods

	public static InvokerTypes ClassifyInvokerMethod(MethodDefinition method)
	{
		if (IsPropertyChangingArgMethod(method))
		{
			return InvokerTypes.PropertyChangingArg;
		}

		if (IsBeforeMethod(method))
		{
			return InvokerTypes.Before;
		}

		return InvokerTypes.String;
	}

	public void FindMethodsForNodes()
	{
		foreach (var notifyNode in NotifyNodes)
		{
			var eventInvoker = RecursiveFindEventInvoker(notifyNode.TypeDefinition);
			if (eventInvoker == null)
			{
				eventInvoker = AddOnPropertyChangingMethod(notifyNode.TypeDefinition);
				if (eventInvoker == null)
				{
					var message = string.Format("\tCould not find EventInvoker method on type '{1}'. Possible names are '{0}'. It is possible you are inheriting from a base class and have not correctly set 'EventInvokerNames' or you are using a explicit PropertyChanging event and the event field is not visible to this instance. Please either correct 'EventInvokerNames' or implement your own EventInvoker on this class. No derived types will be processed. If you want to suppress this message place a [DoNotNotifyAttribute] on {1}.", string.Join(", ", EventInvokerNames), notifyNode.TypeDefinition.Name);
					ModuleWeaver.WriteWarning(message);
					continue;
				}
			}

			notifyNode.EventInvoker = eventInvoker;

			foreach (var childNode in notifyNode.Nodes)
			{
				ProcessChildNode(childNode, eventInvoker);
			}
		}
	}

	public static bool IsBeforeMethod(MethodDefinition method)
	{
		var parameters = method.Parameters;
		return (parameters.Count == 2)
			&& (parameters[0].ParameterType.FullName == "System.String")
			&& (parameters[1].ParameterType.FullName == "System.Object");
	}

	public static bool IsPropertyChangingArgMethod(MethodDefinition method)
	{
		var parameters = method.Parameters;
		return (parameters.Count == 1)
			&& (parameters[0].ParameterType.FullName == "System.ComponentModel.PropertyChangingEventArgs");
	}

	public static bool IsSingleStringMethod(MethodDefinition method)
	{
		var parameters = method.Parameters;
		return (parameters.Count == 1)
			&& (parameters[0].ParameterType.FullName == "System.String");
	}

	public EventInvokerMethod RecursiveFindEventInvoker(TypeDefinition typeDefinition)
	{
		var typeDefinitions = new Stack<TypeDefinition>();
		MethodDefinition methodDefinition;
		var currentTypeDefinition = typeDefinition;
		do
		{
			typeDefinitions.Push(currentTypeDefinition);

			if (FindEventInvokerMethodDefinition(currentTypeDefinition, out methodDefinition))
			{
				break;
			}

			var baseType = currentTypeDefinition.BaseType;

			if ((baseType == null) || (baseType.FullName == "System.Object"))
			{
				return null;
			}

			currentTypeDefinition = Resolve(baseType);
		} while (true);

		return new()
		{
			MethodReference = GetMethodReference(typeDefinitions, methodDefinition),
			IsVisibleFromChildren = IsVisibleFromChildren(methodDefinition),
			InvokerType = ClassifyInvokerMethod(methodDefinition)
		};
	}

	private EventInvokerMethod FindEventInvokerMethod(TypeDefinition type)
	{
		if (FindEventInvokerMethodDefinition(type, out var methodDefinition))
		{
			var methodReference = ModuleDefinition.ImportReference(methodDefinition);
			return new()
			{
				MethodReference = methodReference.GetGeneric(),
				IsVisibleFromChildren = IsVisibleFromChildren(methodDefinition),
				InvokerType = ClassifyInvokerMethod(methodDefinition)
			};
		}

		return null;
	}

	private bool FindEventInvokerMethodDefinition(TypeDefinition type, out MethodDefinition methodDefinition)
	{
		methodDefinition = type.Methods
			.Where(_ => (_.IsFamily || _.IsFamilyAndAssembly || _.IsPublic || _.IsFamilyOrAssembly) && EventInvokerNames.Contains(_.Name))
			.OrderByDescending(definition => definition.Parameters.Count)
			.FirstOrDefault(_ => IsBeforeMethod(_) || IsSingleStringMethod(_) || IsPropertyChangingArgMethod(_));
		if (methodDefinition == null)
		{
			methodDefinition = type.Methods
				.Where(_ => EventInvokerNames.Contains(_.Name))
				.OrderByDescending(definition => definition.Parameters.Count)
				.FirstOrDefault(_ => IsBeforeMethod(_) || IsSingleStringMethod(_) || IsPropertyChangingArgMethod(_));
		}

		return methodDefinition != null;
	}

	private static bool IsVisibleFromChildren(MethodDefinition methodDefinition)
	{
		return methodDefinition.IsFamilyOrAssembly || methodDefinition.IsFamily || methodDefinition.IsFamilyAndAssembly || methodDefinition.IsPublic;
	}

	private void ProcessChildNode(TypeNode node, EventInvokerMethod eventInvoker)
	{
		var childEventInvoker = FindEventInvokerMethod(node.TypeDefinition);
		if (childEventInvoker == null)
		{
			if (node.TypeDefinition.BaseType.IsGenericInstance)
			{
				var methodReference = MakeGeneric(node.TypeDefinition.BaseType, eventInvoker.MethodReference);
				eventInvoker = new()
				{
					InvokerType = eventInvoker.InvokerType,
					MethodReference = methodReference,
					IsVisibleFromChildren = eventInvoker.IsVisibleFromChildren
				};
			}
		}
		else
		{
			eventInvoker = childEventInvoker;
		}

		if (!eventInvoker.IsVisibleFromChildren)
		{
			var error = $"Cannot use '{eventInvoker.MethodReference.FullName}' in '{node.TypeDefinition.FullName}' since that method is not visible from the child class.";
			throw new WeavingException(error);
		}

		node.EventInvoker = eventInvoker;

		foreach (var childNode in node.Nodes)
		{
			ProcessChildNode(childNode, eventInvoker);
		}
	}

	#endregion
}