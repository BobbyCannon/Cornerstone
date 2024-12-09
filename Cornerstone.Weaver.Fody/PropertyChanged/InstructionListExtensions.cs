#region References

using System.Linq;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public static class InstructionListExtensions
{
	#region Methods

	public static void Append(this Collection<Instruction> collection, params Instruction[] instructions)
	{
		foreach (var instruction in instructions)
		{
			collection.Add(instruction);
		}
	}

	public static int Insert(this Collection<Instruction> collection, int index, params Instruction[] instructions)
	{
		foreach (var instruction in instructions.Reverse())
		{
			collection.Insert(index, instruction);
		}
		return index + instructions.Length;
	}

	public static void Prepend(this Collection<Instruction> collection, params Instruction[] instructions)
	{
		for (var index = 0; index < instructions.Length; index++)
		{
			var instruction = instructions[index];
			collection.Insert(index, instruction);
		}
	}

	#endregion
}