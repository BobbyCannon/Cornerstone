#region References

using System;
using System.Linq;
using Cornerstone.Weaver.Fody.PropertyShared;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public class EqualityCheckWeaver
{
	#region Fields

	private Collection<Instruction> _instructions;
	private readonly PropertyData _propertyData;
	private readonly TypeDefinition _typeDefinition;
	private readonly WeaverForPropertyChanged _typeEqualityFinder;

	#endregion

	#region Constructors

	public EqualityCheckWeaver(PropertyData propertyData, TypeDefinition typeDefinition, WeaverForPropertyChanged typeEqualityFinder)
	{
		_propertyData = propertyData;
		_typeDefinition = typeDefinition;
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
		if (_propertyData.BackingFieldReference.FieldType.FullName == _propertyData.PropertyDefinition.PropertyType.FullName)
		{
			InjectEqualityCheck(Instruction.Create(OpCodes.Ldfld, fieldReference), fieldReference.FieldType, fieldReference.DeclaringType);
		}
	}

	private void CheckAgainstProperty()
	{
		var propertyReference = _propertyData.PropertyDefinition;
		var methodDefinition = _propertyData.PropertyDefinition.GetMethod.GetGeneric();
		InjectEqualityCheck(Instruction.Create(OpCodes.Call, methodDefinition), propertyReference.PropertyType, propertyReference.DeclaringType);
	}

	private void InjectEqualityCheck(Instruction targetInstruction, TypeReference targetType, TypeReference declaringType)
	{
		if (ShouldSkipEqualityCheck())
		{
			_typeEqualityFinder.ModuleWeaver.WriteDebug($"\t\t\tEquality Check Skipped for {targetType.Name}");
			return;
		}
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
		var typeEqualityMethod = _propertyData.EqualsMethod;
		if (typeEqualityMethod == null)
		{
			var supportsCeq = false;

			try
			{
				supportsCeq = targetType.SupportsCeq();
			}
			catch (Exception ex)
			{
				_typeEqualityFinder.EmitWarning($"Ignoring Ceq of type {targetType.FullName} => {ex.Message}");
			}

			if (supportsCeq && (targetType.IsValueType || !_typeEqualityFinder.CheckForEqualityUsingBaseEquals))
			{
				_instructions.Prepend(
					Instruction.Create(OpCodes.Ldarg_0),
					targetInstruction,
					Instruction.Create(OpCodes.Ldarg_1),
					Instruction.Create(OpCodes.Ceq),
					Instruction.Create(OpCodes.Brfalse_S, nopInstruction),
					Instruction.Create(OpCodes.Ret));
			}
			else if (targetType.IsValueType && (_typeEqualityFinder.EqualityComparerTypeReference != null))
			{
				var module = _typeEqualityFinder.ModuleDefinition;
				var ec = _typeEqualityFinder.EqualityComparerTypeReference.Resolve();

				var specificEqualityComparerType = module.ImportReference(ec.MakeGenericInstanceType(targetType), declaringType);
				var defaultProperty = module.ImportReference(ec.Properties.Single(p => p.Name == "Default").GetMethod);
				var equalsMethod = module.ImportReference(ec.Methods.Single(p => (p.Name == "Equals") && (p.Parameters.Count == 2)));

				defaultProperty.DeclaringType = specificEqualityComparerType;
				equalsMethod.DeclaringType = specificEqualityComparerType;

				_instructions.Prepend(
					Instruction.Create(OpCodes.Call, defaultProperty),
					Instruction.Create(OpCodes.Ldarg_0),
					targetInstruction,
					Instruction.Create(OpCodes.Ldarg_1),
					Instruction.Create(OpCodes.Callvirt, equalsMethod),
					Instruction.Create(OpCodes.Brfalse_S, nopInstruction),
					Instruction.Create(OpCodes.Ret));
			}
			else if (targetType.IsValueType || targetType.IsGenericParameter)
			{
				_instructions.Prepend(
					Instruction.Create(OpCodes.Ldarg_0),
					targetInstruction,
					Instruction.Create(OpCodes.Box, targetType),
					Instruction.Create(OpCodes.Ldarg_1),
					Instruction.Create(OpCodes.Box, targetType),
					Instruction.Create(OpCodes.Call, _typeEqualityFinder.ObjectEqualsMethod),
					Instruction.Create(OpCodes.Brfalse_S, nopInstruction),
					Instruction.Create(OpCodes.Ret));
			}
			else
			{
				_instructions.Prepend(
					Instruction.Create(OpCodes.Ldarg_0),
					targetInstruction,
					Instruction.Create(OpCodes.Ldarg_1),
					Instruction.Create(OpCodes.Call, _typeEqualityFinder.ObjectEqualsMethod),
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

	private bool ShouldSkipEqualityCheck()
	{
		if (!_typeEqualityFinder.CheckForEquality)
		{
			return true;
		}

		var attribute = "Cornerstone.Weaver.DoNotCheckEqualityAttribute";

		return _typeDefinition.GetAllCustomAttributes().ContainsAttribute(attribute)
			|| _propertyData.PropertyDefinition.CustomAttributes.ContainsAttribute(attribute);
	}

	#endregion
}