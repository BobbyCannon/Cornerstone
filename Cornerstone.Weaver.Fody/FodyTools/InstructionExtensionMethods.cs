#region References

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

#endregion

namespace Cornerstone.Weaver.Fody.FodyTools;

[GeneratedCode("FodyTools", "1.0")]
internal static class InstructionExtensionMethods
{
	#region Methods

	/// <summary> Clones the specified instruction. </summary>
	/// <param name="instruction"> The instruction. </param>
	/// <returns> A new instruction with the same OpCode and Operand. </returns>
	public static Instruction Clone(this Instruction instruction)
	{
		var clone = Instruction.Create(OpCodes.Nop);

		clone.OpCode = instruction.OpCode;
		clone.Operand = instruction.Operand;

		return clone;
	}

	/// <summary>
	/// Computes the stack delta of an instruction. This is an excerpt of Mono.Cecil (https://github.com/jbevain/cecil/blob/eea822cad4b6f320c9e1da642fcbc0c129b00a6e/Mono.Cecil.Cil/CodeWriter.cs#L437)
	/// </summary>
	/// <param name="instruction"> The instruction. </param>
	/// <param name="stackSize"> Size of the stack, adjusted by the instructions impact. </param>
	/// <exception cref="InvalidOperationException"> Can't compute stack delta along nonlinear instruction path. </exception>
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public static void ComputeStackDelta(this Instruction instruction, ref int stackSize)
	{
		switch (instruction.OpCode.FlowControl)
		{
			case FlowControl.Call:
			{
				var method = (IMethodSignature) instruction.Operand;
				// pop 'this' argument
				if (method.HasImplicitThis() && (instruction.OpCode.Code != Code.Newobj))
				{
					stackSize--;
				}
				// pop normal arguments
				if (method.HasParameters)
				{
					stackSize -= method.Parameters.Count;
				}
				// pop function pointer
				if (instruction.OpCode.Code == Code.Calli)
				{
					stackSize--;
				}
				// push return value
				if ((method.ReturnType.MetadataType != MetadataType.Void) || (instruction.OpCode.Code == Code.Newobj))
				{
					stackSize++;
				}
				break;
			}

			case FlowControl.Branch:
			case FlowControl.Break:
			case FlowControl.Throw:
			case FlowControl.Return:
				throw new InvalidOperationException("Can't compute stack delta along nonlinear instruction path.");

			default:
				instruction.OpCode.StackBehaviourPop.ComputePopDelta(ref stackSize);
				instruction.OpCode.StackBehaviourPush.ComputePushDelta(ref stackSize);
				break;
		}
	}

	/// <summary>
	/// Removes the instruction at the specified position; throws if the op code of the instruction to remove does not match one of the specified <paramref name="expectedOpCodes" />
	/// </summary>
	/// <param name="instructions"> The instructions. </param>
	/// <param name="index"> The index. </param>
	/// <param name="expectedOpCodes"> The expected op codes. </param>
	/// <exception cref="InvalidOperationException"> Expected op code at index {index}: {expectedOpCode}, but was {opCode} </exception>
	public static void RemoveAt(this IList<Instruction> instructions, int index, params OpCode[] expectedOpCodes)
	{
		var opCode = instructions[index].OpCode;
		if (!expectedOpCodes.Contains(opCode))
		{
			throw new InvalidOperationException($"Expected op codes at index {index}: {string.Join(",", expectedOpCodes)}; found op code: {opCode}");
		}

		instructions.RemoveAt(index);
	}

	/// <summary>
	/// Replaces the instructions OpCode and Operand and returns a copy of the original instruction.
	/// </summary>
	/// <param name="instruction"> The instruction. </param>
	/// <param name="opCode"> The op code. </param>
	/// <param name="operand"> The operand. </param>
	/// <returns> A copy of the original instruction. </returns>
	public static Instruction ReplaceWith(this Instruction instruction, OpCode opCode, object operand = null)
	{
		var clone = instruction.Clone();

		instruction.OpCode = opCode;
		instruction.Operand = operand;

		return clone;
	}

	[SuppressMessage("ReSharper", "InconsistentNaming")]
	private static void ComputePopDelta(this StackBehaviour pop_behavior, ref int stack_size)
	{
		switch (pop_behavior)
		{
			case StackBehaviour.Popi:
			case StackBehaviour.Popref:
			case StackBehaviour.Pop1:
				stack_size--;
				break;
			case StackBehaviour.Pop1_pop1:
			case StackBehaviour.Popi_pop1:
			case StackBehaviour.Popi_popi:
			case StackBehaviour.Popi_popi8:
			case StackBehaviour.Popi_popr4:
			case StackBehaviour.Popi_popr8:
			case StackBehaviour.Popref_pop1:
			case StackBehaviour.Popref_popi:
				stack_size -= 2;
				break;
			case StackBehaviour.Popi_popi_popi:
			case StackBehaviour.Popref_popi_popi:
			case StackBehaviour.Popref_popi_popi8:
			case StackBehaviour.Popref_popi_popr4:
			case StackBehaviour.Popref_popi_popr8:
			case StackBehaviour.Popref_popi_popref:
				stack_size -= 3;
				break;
			case StackBehaviour.PopAll:
				stack_size = 0;
				break;
		}
	}

	[SuppressMessage("ReSharper", "InconsistentNaming")]
	private static void ComputePushDelta(this StackBehaviour push_behaviour, ref int stack_size)
	{
		switch (push_behaviour)
		{
			case StackBehaviour.Push1:
			case StackBehaviour.Pushi:
			case StackBehaviour.Pushi8:
			case StackBehaviour.Pushr4:
			case StackBehaviour.Pushr8:
			case StackBehaviour.Pushref:
				stack_size++;
				break;
			case StackBehaviour.Push1_push1:
				stack_size += 2;
				break;
		}
	}

	private static bool HasImplicitThis(this IMethodSignature self)
	{
		return self.HasThis && !self.ExplicitThis;
	}

	#endregion
}