#region References

using System.Linq;
using Cornerstone.Weaver.Fody.PropertyShared;
using Mono.Cecil;
using Mono.Cecil.Cil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Methods

	private MethodDefinition InjectInterceptedMethod(TypeDefinition targetType, MethodDefinition innerOnPropertyChanged)
	{
		var delegateHolderInjector = new DelegateHolderInjector
		{
			TargetTypeDefinition = targetType,
			OnPropertyChangedMethodReference = innerOnPropertyChanged,
			WeaverForPropertyChanged = this
		};
		delegateHolderInjector.InjectDelegateHolder();
		var method = new MethodDefinition(EventInvokerNames.First(), GetMethodAttributes(targetType), ModuleWeaver.TypeSystem.VoidReference);

		var propertyName = new ParameterDefinition("propertyName", ParameterAttributes.None, ModuleWeaver.TypeSystem.StringReference);
		var parameters = method.Parameters;
		parameters.Add(propertyName);
		if (InterceptorType == InvokerTypes.BeforeAfter)
		{
			var before = new ParameterDefinition("before", ParameterAttributes.None, ModuleWeaver.TypeSystem.ObjectReference);
			parameters.Add(before);
			var after = new ParameterDefinition("after", ParameterAttributes.None, ModuleWeaver.TypeSystem.ObjectReference);
			parameters.Add(after);
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
		instructions.Add(Instruction.Create(OpCodes.Stfld, delegateHolderInjector.PropertyNameField));
		instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
		instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
		instructions.Add(Instruction.Create(OpCodes.Stfld, delegateHolderInjector.TargetField));
		instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
		instructions.Add(Instruction.Create(OpCodes.Ldftn, delegateHolderInjector.MethodDefinition));
		instructions.Add(Instruction.Create(OpCodes.Newobj, ActionConstructorReference));
		instructions.Add(Instruction.Create(OpCodes.Stloc_0));
		instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
		instructions.Add(Instruction.Create(OpCodes.Ldloc_0));
		instructions.Add(Instruction.Create(OpCodes.Ldloc_1));
		instructions.Add(Instruction.Create(OpCodes.Ldfld, delegateHolderInjector.PropertyNameField));
		if (InterceptorType == InvokerTypes.BeforeAfter)
		{
			instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
			instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
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

	#endregion
}