﻿#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Weaver.Fody.PropertyShared;
using Fody;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Fields

	private readonly string _isChangedPropertyName = "IsChanged";

	#endregion

	#region Methods

	public void FindIsChangedMethod()
	{
		foreach (var notifyNode in NotifyNodes)
		{
			var eventInvoker = RecursiveFindMethod(notifyNode.TypeDefinition);

			notifyNode.IsChangedInvoker = eventInvoker;

			foreach (var childNode in notifyNode.Nodes)
			{
				ProcessChildNode(childNode, eventInvoker);
			}
		}
	}

	public MethodReference RecursiveFindMethod(TypeDefinition typeDefinition)
	{
		var typeDefinitions = new Stack<TypeDefinition>();
		MethodDefinition methodDefinition;
		var currentTypeDefinition = typeDefinition;
		do
		{
			typeDefinitions.Push(currentTypeDefinition);
			if (FindIsChangedEventInvokerMethodDefinition(currentTypeDefinition, out methodDefinition))
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

		return GetMethodReference(typeDefinitions, methodDefinition);
	}

	private MethodReference FindEventInvokerMethodRef(TypeDefinition type)
	{
		if (!FindIsChangedEventInvokerMethodDefinition(type, out var methodDefinition))
		{
			return null;
		}
		var methodReference = ModuleDefinition.ImportReference(methodDefinition);
		return methodReference.GetGeneric();
	}

	private bool FindIsChangedEventInvokerMethodDefinition(TypeDefinition type, out MethodDefinition methodDefinition)
	{
		//todo: check bool type
		methodDefinition = null;
		var propertyDefinition = type.Properties
			.FirstOrDefault(x =>
				(x.Name == _isChangedPropertyName) &&
				x.SetMethod is { IsPublic: true }
			);

		if (propertyDefinition != null)
		{
			if (propertyDefinition.PropertyType.FullName != ModuleWeaver.TypeSystem.BooleanDefinition.FullName)
			{
				EmitWarning($"Found '{propertyDefinition.GetName()}' but is was of type '{propertyDefinition.PropertyType.Name}' instead of '{ModuleWeaver.TypeSystem.BooleanDefinition.Name}' so it will not be used.");
				return false;
			}
			if (propertyDefinition.SetMethod.IsStatic)
			{
				throw new WeavingException($"Found '{propertyDefinition.GetName()}' but is was static. Change it to non static.");
			}
			methodDefinition = propertyDefinition.SetMethod;
		}
		return methodDefinition != null;
	}

	private void ProcessChildNode(TypeNode node, MethodReference changedInvokerMethod)
	{
		var childEventInvoker = FindEventInvokerMethodRef(node.TypeDefinition);
		if (childEventInvoker == null)
		{
			if (changedInvokerMethod != null)
			{
				if (node.TypeDefinition.BaseType.IsGenericInstance)
				{
					var methodReference = MakeGeneric(node.TypeDefinition.BaseType, changedInvokerMethod);
					changedInvokerMethod = methodReference;
				}
			}
		}
		else
		{
			changedInvokerMethod = childEventInvoker;
		}

		node.IsChangedInvoker = changedInvokerMethod;

		foreach (var childNode in node.Nodes)
		{
			ProcessChildNode(childNode, changedInvokerMethod);
		}
	}

	#endregion
}