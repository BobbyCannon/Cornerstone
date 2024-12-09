#region References

using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyShared;

public class RecursiveIlFinder
{
	#region Fields

	public readonly List<Instruction> Instructions;
	private readonly List<MethodDefinition> _processedMethods;
	private readonly TypeDefinition _typeDefinition;

	#endregion

	#region Constructors

	public RecursiveIlFinder(TypeDefinition typeDefinition)
	{
		Instructions = [];
		_processedMethods = [];
		_typeDefinition = typeDefinition;
	}

	#endregion

	#region Methods

	public void Execute(MethodDefinition getMethod)
	{
		_processedMethods.Add(getMethod);
		if (getMethod.Body == null)
		{
			return;
		}
		foreach (var instruction in getMethod.Body.Instructions)
		{
			Instructions.Add(instruction);
			if (!IsCall(instruction.OpCode))
			{
				continue;
			}

			if (instruction.Operand is not MethodDefinition methodDefinition)
			{
				continue;
			}
			if (methodDefinition.IsGetter || methodDefinition.IsSetter)
			{
				continue;
			}
			if (_processedMethods.Contains(methodDefinition))
			{
				continue;
			}
			if (methodDefinition.DeclaringType != _typeDefinition)
			{
				continue;
			}
			Execute(methodDefinition);
		}
	}

	private static bool IsCall(OpCode opCode)
	{
		return (opCode == OpCodes.Call) ||
			(opCode == OpCodes.Callvirt) ||
			(opCode == OpCodes.Calli) ||
			(opCode == OpCodes.Ldftn);
	}

	#endregion
}