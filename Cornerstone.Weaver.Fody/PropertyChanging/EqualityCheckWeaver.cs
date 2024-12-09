#region References

using System.Linq;
using Cornerstone.Weaver.Fody.PropertyShared;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public class EqualityCheckWeaver
{
	#region Fields

	private Collection<Instruction> _instructions;
	private readonly PropertyData _propertyData;
	private readonly WeaverForPropertyChanging _typeEqualityFinder;

	#endregion

	#region Constructors

	public EqualityCheckWeaver(PropertyData propertyData, WeaverForPropertyChanging typeEqualityFinder)
	{
		_propertyData = propertyData;
		_typeEqualityFinder = typeEqualityFinder;
	}

	#endregion

	#region Methods

	public void Execute()
	{
		var property = _propertyData.PropertyDefinition;
		_instructions = property.SetMethod.Body.Instructions;

		if (_propertyData.BackingFieldReference == null)
		{
			CheckAgainstProperty();
		}
		else
		{
			CheckAgainstField();
		}
	}

	private void CheckAgainstField()
	{
		var fieldReference = _propertyData.BackingFieldReference.Resolve().GetGeneric();
		InjectEqualityCheck(Instruction.Create(OpCodes.Ldfld, fieldReference), fieldReference.FieldType);
	}

	private void CheckAgainstProperty()
	{
		var propertyReference = _propertyData.PropertyDefinition;
		var methodDefinition = _propertyData.PropertyDefinition.GetMethod.GetGeneric();
		InjectEqualityCheck(Instruction.Create(OpCodes.Call, methodDefinition), propertyReference.PropertyType);
	}

	private void InjectEqualityCheck(Instruction targetInstruction, TypeReference targetType)
	{
		var nopInstruction = _instructions.First();
		if (nopInstruction.OpCode != OpCodes.Nop)
		{
			nopInstruction = Instruction.Create(OpCodes.Nop);
			_instructions.Insert(0, nopInstruction);
		}
		if (targetType.Name == "String")
		{
			_instructions.Prepend(
				Instruction.Create(OpCodes.Ldarg_0),
				targetInstruction,
				Instruction.Create(OpCodes.Ldarg_1),
				Instruction.Create(OpCodes.Ldc_I4, _typeEqualityFinder.OrdinalStringComparison),
				Instruction.Create(OpCodes.Call, _typeEqualityFinder.StringEquals),
				Instruction.Create(OpCodes.Brfalse_S, nopInstruction),
				Instruction.Create(OpCodes.Ret));
			return;
		}
		var typeEqualityMethod = _typeEqualityFinder.FindTypeEquality(targetType);
		if (typeEqualityMethod == null)
		{
			if (targetType.IsGenericParameter)
			{
				_instructions.Prepend(
					Instruction.Create(OpCodes.Ldarg_0),
					targetInstruction,
					Instruction.Create(OpCodes.Box, targetType),
					Instruction.Create(OpCodes.Ldarg_1),
					Instruction.Create(OpCodes.Box, targetType),
					Instruction.Create(OpCodes.Ceq),
					Instruction.Create(OpCodes.Brfalse_S, nopInstruction),
					Instruction.Create(OpCodes.Ret));
			}
			else if (targetType.SupportsCeq())
			{
				_instructions.Prepend(
					Instruction.Create(OpCodes.Ldarg_0),
					targetInstruction,
					Instruction.Create(OpCodes.Ldarg_1),
					Instruction.Create(OpCodes.Ceq),
					Instruction.Create(OpCodes.Brfalse_S, nopInstruction),
					Instruction.Create(OpCodes.Ret));
			}
		}
		else
		{
			_instructions.Prepend(
				Instruction.Create(OpCodes.Ldarg_0),
				targetInstruction,
				Instruction.Create(OpCodes.Ldarg_1),
				Instruction.Create(OpCodes.Call, typeEqualityMethod),
				Instruction.Create(OpCodes.Brfalse_S, nopInstruction),
				Instruction.Create(OpCodes.Ret));
		}
	}

	#endregion
}