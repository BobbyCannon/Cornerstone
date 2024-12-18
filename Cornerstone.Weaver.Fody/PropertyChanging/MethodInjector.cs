﻿#region References

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

	public EventInvokerMethod AddOnPropertyChangingMethod(TypeDefinition targetType)
	{
		var propertyChangingField = FindPropertyChangingField(targetType);
		if (propertyChangingField == null)
		{
			return null;
		}
		if (FoundInterceptor)
		{
			if (targetType.HasGenericParameters)
			{
				var message = $"Error processing '{targetType.Name}'. Interception is not supported on generic types. To manually work around this problem add a [DoNotNotify] to the class and then manually implement INotifyPropertyChanging for that class and all child classes. If you would like this feature handled automatically please feel free to submit a pull request.";
				throw new WeavingException(message);
			}
			var methodDefinition = GetMethodDefinition(targetType, propertyChangingField);

			return new()
			{
				MethodReference = InjectInterceptedMethod(targetType, methodDefinition).GetGeneric(),
				IsVisibleFromChildren = true,
				InvokerType = InterceptorType
			};
		}
		return new()
		{
			MethodReference = InjectMethod(targetType, EventInvokerNames.First(), propertyChangingField).GetGeneric(),
			IsVisibleFromChildren = true,
			InvokerType = InterceptorType
		};
	}

	public static FieldReference FindPropertyChangingField(TypeDefinition targetType)
	{
		var findPropertyChangingField = targetType.Fields.FirstOrDefault(x => IsPropertyChangingEventHandler(x.FieldType));
		return findPropertyChangingField?.GetGeneric();
	}

	private static MethodAttributes GetMethodAttributes(TypeDefinition targetType)
	{
		if (targetType.IsSealed)
		{
			return MethodAttributes.Public | MethodAttributes.HideBySig;
		}
		return MethodAttributes.Virtual | MethodAttributes.Public | MethodAttributes.NewSlot;
	}

	private MethodDefinition GetMethodDefinition(TypeDefinition targetType, FieldReference propertyChangingField)
	{
		var eventInvokerName = "Inner" + EventInvokerNames.First();
		var methodDefinition = targetType.Methods.FirstOrDefault(_ => _.Name == eventInvokerName);
		if ((methodDefinition?.Parameters.Count == 1) && (methodDefinition.Parameters[0].ParameterType.FullName == "System.String"))
		{
			return methodDefinition;
		}
		return InjectMethod(targetType, eventInvokerName, propertyChangingField);
	}

	private MethodDefinition InjectInterceptedMethod(TypeDefinition targetType, MethodDefinition innerOnPropertyChanging)
	{
		var delegateHolderInjector = new DelegateHolderInjector
		{
			TargetTypeDefinition = targetType,
			OnPropertyChangingMethodReference = innerOnPropertyChanging,
			WeaverForPropertyChanging = this
		};
		delegateHolderInjector.InjectDelegateHolder();
		var method = new MethodDefinition(EventInvokerNames.First(), GetMethodAttributes(targetType), ModuleWeaver.TypeSystem.VoidReference);

		var propertyName = new ParameterDefinition("propertyName", ParameterAttributes.None, ModuleWeaver.TypeSystem.StringReference);
		method.Parameters.Add(propertyName);
		if (InterceptorType == InvokerTypes.Before)
		{
			var before = new ParameterDefinition("before", ParameterAttributes.None, ModuleWeaver.TypeSystem.ObjectReference);
			method.Parameters.Add(before);
		}

		var action = new VariableDefinition(ActionTypeReference);
		method.Body.Variables.Add(action);

		var variableDefinition = new VariableDefinition(delegateHolderInjector.TypeDefinition);
		method.Body.Variables.Add(variableDefinition);

		var instructions = method.Body.Instructions;

		var last = Instruction.Create(OpCodes.Ret);
		instructions.Add(Instruction.Create(OpCodes.Newobj, delegateHolderInjector.ConstructorDefinition));
		instructions.Add(Instruction.Create(OpCodes.Stloc_1));
		instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
		instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
		instructions.Add(Instruction.Create(OpCodes.Stfld, delegateHolderInjector.PropertyName));
		instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
		instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
		instructions.Add(Instruction.Create(OpCodes.Stfld, delegateHolderInjector.Target));
		instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
		instructions.Add(Instruction.Create(OpCodes.Ldftn, delegateHolderInjector.MethodDefinition));
		instructions.Add(Instruction.Create(OpCodes.Newobj, ActionConstructorReference));
		instructions.Add(Instruction.Create(OpCodes.Stloc_0));
		instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
		instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
		instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
		instructions.Add(Instruction.Create(OpCodes.Ldfld, delegateHolderInjector.PropertyName));
		if (InterceptorType == InvokerTypes.Before)
		{
			instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
			instructions.Add(Instruction.Create(OpCodes.Call, InterceptMethod));
		}
		else
		{
			instructions.Add(Instruction.Create(OpCodes.Call, InterceptMethod));
		}

		instructions.Add(last);
		method.Body.InitLocals = true;

		targetType.Methods.Add(method);
		return method;
	}

	private MethodDefinition InjectMethod(TypeDefinition targetType, string eventInvokerName, FieldReference propertyChangingField)
	{
		var method = new MethodDefinition(eventInvokerName, GetMethodAttributes(targetType), ModuleWeaver.TypeSystem.VoidReference);
		method.Parameters.Add(new("propertyName", ParameterAttributes.None, ModuleWeaver.TypeSystem.StringReference));

		var handlerVariable = new VariableDefinition(PropChangingHandlerReference);
		method.Body.Variables.Add(handlerVariable);
		var boolVariable = new VariableDefinition(ModuleWeaver.TypeSystem.BooleanReference);
		method.Body.Variables.Add(boolVariable);

		var instructions = method.Body.Instructions;

		var last = Instruction.Create(OpCodes.Ret);
		instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
		instructions.Add(Instruction.Create(OpCodes.Ldfld, propertyChangingField));
		instructions.Add(Instruction.Create(OpCodes.Stloc_0));
		instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
		instructions.Add(Instruction.Create(OpCodes.Ldnull));
		instructions.Add(Instruction.Create(OpCodes.Ceq));
		instructions.Add(Instruction.Create(OpCodes.Stloc_1));
		instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
		instructions.Add(Instruction.Create(OpCodes.Brtrue_S, last));
		instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
		instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
		instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
		instructions.Add(Instruction.Create(OpCodes.Newobj, ComponentModelPropertyChangingEventConstructorReference));
		instructions.Add(Instruction.Create(OpCodes.Callvirt, ComponentModelPropertyChangingEventHandlerInvokeReference));

		instructions.Add(last);
		method.Body.InitLocals = true;
		targetType.Methods.Add(method);
		return method;
	}

	#endregion
}