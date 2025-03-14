#region References

using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public static class AlreadyNotifyFinder
{
	#region Methods

	public static IEnumerable<string> GetAlreadyNotifies(this PropertyDefinition propertyDefinition, string methodName)
	{
		if (propertyDefinition.SetMethod.IsAbstract)
		{
			yield break;
		}
		var instructions = propertyDefinition.SetMethod.Body.Instructions;
		for (var index = 0; index < instructions.Count; index++)
		{
			var instruction = instructions[index];
			if (instruction.IsCallToMethod(methodName, out var propertyNameIndex))
			{
				var before = instructions[index - propertyNameIndex];
				if (before.OpCode == OpCodes.Ldstr)
				{
					yield return (string) before.Operand;
				}
			}
		}
	}

	#endregion
}