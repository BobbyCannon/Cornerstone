#region References

using System.Collections.Generic;
using System.Linq;
using Cornerstone.Weaver.Fody.PropertyShared;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using TypeSystem = Fody.TypeSystem;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public class PropertyWeaver
{
	#region Fields

	private Collection<Instruction> _instructions;
	private readonly PropertyData _propertyData;
	private MethodBody _setMethodBody;
	private readonly TypeNode _typeNode;
	private readonly TypeSystem _typeSystem;

	private readonly WeaverForPropertyChanging _weaverForPropertyChanging;

	#endregion

	#region Constructors

	public PropertyWeaver(WeaverForPropertyChanging weaverForPropertyChanging, PropertyData propertyData, TypeNode typeNode, TypeSystem typeSystem)
	{
		_weaverForPropertyChanging = weaverForPropertyChanging;
		_propertyData = propertyData;
		_typeNode = typeNode;
		_typeSystem = typeSystem;
	}

	#endregion

	#region Methods

	public Instruction CallEventInvoker()
	{
		return Instruction.Create(OpCodes.Callvirt, _typeNode.EventInvoker.MethodReference);
	}

	public Instruction CreateCall(MethodReference methodReference)
	{
		return Instruction.Create(OpCodes.Callvirt, methodReference);
	}

	public void Execute()
	{
		_weaverForPropertyChanging.ModuleWeaver.WriteInfo("\t\t" + _propertyData.PropertyDefinition.Name);
		var property = _propertyData.PropertyDefinition;
		_setMethodBody = property.SetMethod.Body;
		_instructions = property.SetMethod.Body.Instructions;

		foreach (var instruction in GetInstructions())
		{
			Inject(instruction);
		}
	}

	private int AddBeforeInvokerCall(int index, PropertyDefinition property)
	{
		var beforeVariable = new VariableDefinition(_typeSystem.ObjectReference);
		_setMethodBody.Variables.Add(beforeVariable);
		var getMethod = property.GetMethod.GetGeneric();

		return _instructions.Insert(index,
			Instruction.Create(OpCodes.Ldarg_0),
			CreateCall(getMethod),
			Instruction.Create(OpCodes.Box, property.GetMethod.ReturnType),
			Instruction.Create(OpCodes.Stloc, beforeVariable),
			Instruction.Create(OpCodes.Ldarg_0),
			Instruction.Create(OpCodes.Ldstr, property.Name),
			Instruction.Create(OpCodes.Ldloc, beforeVariable),
			CallEventInvoker());
	}

	private int AddEventInvokeCall(int index, PropertyDefinition property)
	{
		index = AddOnChangingMethodCall(index, property);
		if (_propertyData.AlreadyNotifies.Contains(property.Name))
		{
			_weaverForPropertyChanging.ModuleWeaver.WriteInfo($"\t\t\t{property.Name} skipped since call already exists");
			return index;
		}
		_weaverForPropertyChanging.ModuleWeaver.WriteInfo($"\t\t\t{property.Name}");
		if (_typeNode.EventInvoker.InvokerType == InvokerTypes.Before)
		{
			return AddBeforeInvokerCall(index, property);
		}
		if (_typeNode.EventInvoker.InvokerType == InvokerTypes.PropertyChangingArg)
		{
			return AddPropertyChangedArgInvokerCall(index, property);
		}
		return AddSimpleInvokerCall(index, property);
	}

	private int AddOnChangingMethodCall(int index, PropertyDefinition property)
	{
		if (!_weaverForPropertyChanging.InjectOnPropertyNameChanging)
		{
			return index;
		}
		var onChangingMethodName = $"On{property.Name}Changing";
		if (ContainsCallToMethod(onChangingMethodName))
		{
			return index;
		}
		var onChangingMethod = _typeNode
			.OnChangingMethods
			.FirstOrDefault(_ => _.Name == onChangingMethodName);
		if (onChangingMethod == null)
		{
			return index;
		}
		return _instructions.Insert(index, Instruction.Create(OpCodes.Ldarg_0), CreateCall(onChangingMethod));
	}

	private int AddPropertyChangedArgInvokerCall(int index, PropertyDefinition property)
	{
		return _instructions.Insert(index,
			Instruction.Create(OpCodes.Ldarg_0),
			Instruction.Create(OpCodes.Ldstr, property.Name),
			Instruction.Create(OpCodes.Newobj, _weaverForPropertyChanging.ComponentModelPropertyChangingEventConstructorReference),
			CallEventInvoker());
	}

	private int AddSimpleInvokerCall(int index, PropertyDefinition property)
	{
		return _instructions.Insert(index,
			Instruction.Create(OpCodes.Ldarg_0),
			Instruction.Create(OpCodes.Ldstr, property.Name),
			CallEventInvoker());
	}

	private bool ContainsCallToMethod(string onChangingMethodName)
	{
		return _instructions.Select(_ => _.Operand)
			.OfType<MethodReference>()
			.Any(_ => _.Name == onChangingMethodName);
	}

	private IEnumerable<Instruction> FindSetFieldInstructions()
	{
		return from instruction in _instructions
			where instruction.OpCode == OpCodes.Stfld
			let fieldReference = instruction.Operand as FieldReference
			where fieldReference != null
			where fieldReference.Name == _propertyData.BackingFieldReference.Name
			select instruction.Previous.Previous;
	}

	private List<Instruction> GetInstructions()
	{
		if (_propertyData.BackingFieldReference == null)
		{
			return [_instructions.First()];
		}
		var setFieldInstructions = FindSetFieldInstructions().ToList();
		if (setFieldInstructions.Count == 0)
		{
			return [_instructions.First()];
		}

		return setFieldInstructions;
	}

	private void Inject(Instruction instruction)
	{
		InjectNop(instruction);
		var index = _instructions.IndexOf(instruction);
		var propertyDefinitions = _propertyData.AlsoNotifyFor.Distinct();

		index = propertyDefinitions.Aggregate(index, AddEventInvokeCall);
		AddEventInvokeCall(index, _propertyData.PropertyDefinition);
	}

	private void InjectNop(Instruction instruction)
	{
		Instruction nop;
		if ((instruction.Previous != null) && (instruction.Previous.OpCode == OpCodes.Nop))
		{
			nop = instruction.Previous;
		}
		else
		{
			nop = Instruction.Create(OpCodes.Nop);
			_instructions.Insert(_instructions.IndexOf(instruction), nop);
		}
		foreach (var innerInstruction in _instructions)
		{
			if (innerInstruction == instruction)
			{
				continue;
			}
			var flowControl = innerInstruction.OpCode.FlowControl;
			if ((flowControl != FlowControl.Branch) && (flowControl != FlowControl.Cond_Branch))
			{
				continue;
			}
			if (innerInstruction.Operand != instruction)
			{
				continue;
			}
			innerInstruction.Operand = nop;
		}
	}

	#endregion
}