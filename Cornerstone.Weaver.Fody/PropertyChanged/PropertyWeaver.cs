#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Weaver.Fody.PropertyShared;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using TypeSystem = Fody.TypeSystem;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public class PropertyWeaver(
	WeaverForPropertyChanged weaverForPropertyChanged,
	PropertyData propertyData,
	TypeNode typeNode,
	TypeSystem typeSystem)
{
	#region Fields

	private Collection<Instruction> _instructions;
	private MethodBody _setMethodBody;

	#endregion

	#region Methods

	public IEnumerable<Instruction> CallEventInvoker(PropertyDefinition propertyDefinition)
	{
		var method = typeNode.EventInvoker.MethodReference;

		if (method.HasGenericParameters)
		{
			var genericMethod = new GenericInstanceMethod(method);
			genericMethod.GenericArguments.Add(propertyDefinition.PropertyType);
			method = genericMethod;
		}

		var instructionList = new List<Instruction>
		{
			Instruction.Create(typeNode.TypeDefinition.GetCallOpCode(), method)
		};

		if (method.ReturnType.FullName != typeSystem.VoidReference.FullName)
		{
			instructionList.Add(Instruction.Create(OpCodes.Pop));
		}

		return instructionList;
	}

	public Instruction CreateCall(MethodReference methodReference)
	{
		return Instruction.Create(typeNode.TypeDefinition.GetCallOpCode(), methodReference);
	}

	public Instruction CreateIsChangedInvoker()
	{
		return Instruction.Create(typeNode.TypeDefinition.GetCallOpCode(), typeNode.IsChangedInvoker);
	}

	public void Execute()
	{
		weaverForPropertyChanged.ModuleWeaver.WriteDebug("\t\t" + propertyData.PropertyDefinition.Name);
		var property = propertyData.PropertyDefinition;
		_setMethodBody = property.SetMethod.Body;
		_instructions = property.SetMethod.Body.Instructions;

		var indexes = GetIndexes();
		indexes.Reverse();
		foreach (var index in indexes)
		{
			InjectAtIndex(index);
		}
	}

	private int AddBeforeAfterGenericInvokerCall(int index, PropertyDefinition property)
	{
		var beforeVariable = new VariableDefinition(property.PropertyType);
		_setMethodBody.Variables.Add(beforeVariable);
		var afterVariable = new VariableDefinition(property.PropertyType);
		_setMethodBody.Variables.Add(afterVariable);

		index = InsertVariableAssignmentFromCurrentValue(index, property, afterVariable);
		index = _instructions.Insert(index,
			Instruction.Create(OpCodes.Ldarg_0),
			Instruction.Create(OpCodes.Ldstr, property.Name),
			Instruction.Create(OpCodes.Ldloc, beforeVariable),
			Instruction.Create(OpCodes.Ldloc, afterVariable)
		);

		index = _instructions.Insert(index, CallEventInvoker(property).ToArray());

		return AddBeforeVariableAssignment(index, property, beforeVariable);
	}

	private int AddBeforeAfterInvokerCall(int index, PropertyDefinition property)
	{
		var beforeVariable = new VariableDefinition(typeSystem.ObjectReference);
		_setMethodBody.Variables.Add(beforeVariable);
		var afterVariable = new VariableDefinition(typeSystem.ObjectReference);
		_setMethodBody.Variables.Add(afterVariable);

		index = InsertVariableAssignmentFromCurrentValue(index, property, afterVariable);
		index = _instructions.Insert(index,
			Instruction.Create(OpCodes.Ldarg_0),
			Instruction.Create(OpCodes.Ldstr, property.Name),
			Instruction.Create(OpCodes.Ldloc, beforeVariable),
			Instruction.Create(OpCodes.Ldloc, afterVariable));

		index = _instructions.Insert(index, CallEventInvoker(property).ToArray());

		return AddBeforeVariableAssignment(index, property, beforeVariable);
	}

	private int AddBeforeAfterOnChangedCall(int index, PropertyDefinition property, MethodReference methodReference, bool useTypedParameters = false)
	{
		var variableType = useTypedParameters ? property.PropertyType : typeSystem.ObjectReference;

		var beforeVariable = new VariableDefinition(variableType);
		_setMethodBody.Variables.Add(beforeVariable);
		var afterVariable = new VariableDefinition(variableType);
		_setMethodBody.Variables.Add(afterVariable);
		index = InsertVariableAssignmentFromCurrentValue(index, property, afterVariable);

		index = _instructions.Insert(index,
			Instruction.Create(OpCodes.Ldarg_0),
			Instruction.Create(OpCodes.Ldloc, beforeVariable),
			Instruction.Create(OpCodes.Ldloc, afterVariable),
			CreateCall(methodReference)
		);

		return AddBeforeVariableAssignment(index, property, beforeVariable);
	}

	private int AddBeforeVariableAssignment(int index, PropertyDefinition property, VariableDefinition variable)
	{
		var i = BuildVariableAssignmentInstructions(property, variable).ToArray();
		_instructions.Prepend(i);
		return index + i.Length;
	}

	private int AddEventInvokeCall(int index, List<OnChangedMethod> onChangedMethods, PropertyDefinition property)
	{
		index = AddOnChangedMethodCalls(index, onChangedMethods, property);
		if (propertyData.AlreadyNotifies.Contains(property.Name))
		{
			weaverForPropertyChanged.ModuleWeaver.WriteDebug($"\t\t\t{property.Name} skipped since call already exists");
			return index;
		}

		weaverForPropertyChanged.ModuleWeaver.WriteDebug($"\t\t\t{property.Name}");
		if (typeNode.EventInvoker.InvokerType == InvokerTypes.BeforeAfterGeneric)
		{
			return AddBeforeAfterGenericInvokerCall(index, property);
		}

		if (typeNode.EventInvoker.InvokerType == InvokerTypes.BeforeAfter)
		{
			return AddBeforeAfterInvokerCall(index, property);
		}

		if (typeNode.EventInvoker.InvokerType == InvokerTypes.PropertyChangedArg)
		{
			return AddPropertyChangedArgInvokerCall(index, property);
		}

		if (typeNode.EventInvoker.InvokerType == InvokerTypes.SenderPropertyChangedArg)
		{
			return AddSenderPropertyChangedArgInvokerCall(index, property);
		}

		return AddSimpleInvokerCall(index, property);
	}

	private int AddIsChangedSetterCall(int index)
	{
		if (!weaverForPropertyChanged.EnableIsChangedProperty || (typeNode.IsChangedInvoker == null) ||
			propertyData.PropertyDefinition.CustomAttributes.ContainsAttribute(Constants.DoNotSetChangedAttribute) ||
			(propertyData.PropertyDefinition.Name == "IsChanged"))
		{
			return index;
		}
		weaverForPropertyChanged.ModuleWeaver.WriteDebug("\t\t\tSet IsChanged");
		return _instructions.Insert(index,
			Instruction.Create(OpCodes.Ldarg_0),
			Instruction.Create(OpCodes.Ldc_I4, 1),
			CreateIsChangedInvoker());
	}

	private int AddOnChangedMethodCalls(int index, List<OnChangedMethod> onChangedMethods, PropertyDefinition propertyDefinition)
	{
		foreach (var onChangedMethod in onChangedMethods)
		{
			if (onChangedMethod.IsDefaultMethod)
			{
				if (!weaverForPropertyChanged.InjectOnPropertyNameChanged)
				{
					continue;
				}

				if (ContainsCallToMethod(onChangedMethod.MethodReference.Name))
				{
					continue;
				}
			}

			switch (onChangedMethod.OnChangedType)
			{
				case OnChangedTypes.NoArg:
					index = AddSimpleOnChangedCall(index, onChangedMethod.MethodReference);
					break;

				case OnChangedTypes.BeforeAfter:
					index = AddBeforeAfterOnChangedCall(index, propertyDefinition, onChangedMethod.MethodReference);
					break;

				case OnChangedTypes.BeforeAfterTyped:
					if (propertyDefinition.PropertyType.FullName != onChangedMethod.ArgumentTypeFullName)
					{
						var methodDefinition = onChangedMethod.MethodDefinition;
						weaverForPropertyChanged.EmitConditionalWarning(methodDefinition, $"Unsupported signature for a On_PropertyName_Changed method: {methodDefinition.Name} in {methodDefinition.DeclaringType.FullName}");
						break;
					}
					index = AddBeforeAfterOnChangedCall(index, propertyDefinition, onChangedMethod.MethodReference, true);
					break;
			}
		}

		return index;
	}

	private int AddPropertyChangedArgInvokerCall(int index, PropertyDefinition property)
	{
		index = _instructions.Insert(index,
			Instruction.Create(OpCodes.Ldarg_0),
			Instruction.Create(OpCodes.Ldsfld, weaverForPropertyChanged.EventArgsCache.GetEventArgsField(property.Name)));

		return _instructions.Insert(index, CallEventInvoker(property).ToArray());
	}

	private int AddSenderPropertyChangedArgInvokerCall(int index, PropertyDefinition property)
	{
		index = _instructions.Insert(index,
			Instruction.Create(OpCodes.Ldarg_0),
			Instruction.Create(OpCodes.Ldarg_0),
			Instruction.Create(OpCodes.Ldsfld, weaverForPropertyChanged.EventArgsCache.GetEventArgsField(property.Name)));

		return _instructions.Insert(index, CallEventInvoker(property).ToArray());
	}

	private int AddSimpleInvokerCall(int index, PropertyDefinition property)
	{
		index = _instructions.Insert(index,
			Instruction.Create(OpCodes.Ldarg_0),
			Instruction.Create(OpCodes.Ldstr, property.Name));

		return _instructions.Insert(index, CallEventInvoker(property).ToArray());
	}

	private int AddSimpleOnChangedCall(int index, MethodReference methodReference)
	{
		return _instructions.Insert(index,
			Instruction.Create(OpCodes.Ldarg_0),
			CreateCall(methodReference));
	}

	private IEnumerable<Instruction> BuildVariableAssignmentInstructions(PropertyDefinition property, VariableDefinition variable)
	{
		var getMethod = property.GetMethod.GetGeneric();

		yield return Instruction.Create(OpCodes.Ldarg_0);
		yield return CreateCall(getMethod);

		var returnType = property.GetMethod.ReturnType;
		if (returnType.FullName != variable.VariableType.FullName)
		{
			yield return Instruction.Create(OpCodes.Box, returnType);
		}
		yield return Instruction.Create(OpCodes.Stloc, variable);
	}

	private bool ContainsCallToMethod(string onChangingMethodName)
	{
		return _instructions.Select(_ => _.Operand)
			.OfType<MethodReference>()
			.Any(_ => _.Name == onChangingMethodName);
	}

	private IEnumerable<int> FindSetFieldInstructions()
	{
		for (var index = 0; index < _instructions.Count; index++)
		{
			var instruction = _instructions[index];
			if (instruction.OpCode == OpCodes.Stfld)
			{
				if (instruction.Operand is not FieldReference fieldReference1)
				{
					continue;
				}

				if (fieldReference1.Name == propertyData.BackingFieldReference.Name)
				{
					yield return index + 1;
				}

				continue;
			}

			if (instruction.OpCode != OpCodes.Ldflda)
			{
				continue;
			}

			if (instruction.Next == null)
			{
				continue;
			}

			if (instruction.Next.OpCode != OpCodes.Initobj)
			{
				continue;
			}

			if (instruction.Operand is not FieldReference fieldReference2)
			{
				continue;
			}

			if (fieldReference2.Name == propertyData.BackingFieldReference.Name)
			{
				yield return index + 2;
			}
		}
	}

	private List<int> GetIndexes()
	{
		if (propertyData.BackingFieldReference == null)
		{
			return [_instructions.Count - 1];
		}

		var setFieldInstructions = FindSetFieldInstructions().ToList();
		if (setFieldInstructions.Count == 0)
		{
			return [_instructions.Count - 1];
		}

		return setFieldInstructions;
	}

	private static List<OnChangedMethod> GetMethodsForProperty(TypeNode typeNode, PropertyDefinition property)
	{
		return (from method in typeNode.OnChangedMethods
			from prop in method.Properties
			where prop == property
			select method).ToList();
	}

	private void InjectAtIndex(int index)
	{
		index = AddIsChangedSetterCall(index);

		foreach (var alsoNotifyForDefinition in propertyData.AlsoNotifyFor.Distinct())
		{
			var alsoNotifyMethods = GetMethodsForProperty(propertyData.ParentType, alsoNotifyForDefinition);

			index = AddEventInvokeCall(index, alsoNotifyMethods, alsoNotifyForDefinition);
		}

		var onChangedMethods = GetMethodsForProperty(propertyData.ParentType, propertyData.PropertyDefinition);
		AddEventInvokeCall(index, onChangedMethods, propertyData.PropertyDefinition);
	}

	private int InsertVariableAssignmentFromCurrentValue(int index, PropertyDefinition property, VariableDefinition variable)
	{
		var i = BuildVariableAssignmentInstructions(property, variable).ToArray();
		_instructions.Insert(index, i);
		return index + i.Length;
	}

	#endregion
}