#region References

using System.Linq;
using Cornerstone.Weaver.Fody.PropertyShared;
using Mono.Cecil;
using Mono.Cecil.Cil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public class IlGeneratedByDependencyReader
{
	#region Fields

	private readonly TypeNode _node;

	#endregion

	#region Constructors

	public IlGeneratedByDependencyReader(TypeNode node)
	{
		_node = node;
	}

	#endregion

	#region Methods

	public bool IsFieldGetInstruction(Instruction instruction, out PropertyDefinition propertyDefinition)
	{
		if (instruction.OpCode.Code == Code.Ldfld)
		{
			if (instruction.Operand is FieldReference fieldReference)
			{
				var mapping = _node.Mappings.FirstOrDefault(x => FieldComparer(fieldReference, x.FieldDefinition));
				if (mapping != null)
				{
					propertyDefinition = mapping.PropertyDefinition;
					return true;
				}
			}
		}
		propertyDefinition = null;
		return false;
	}

	public bool IsPropertyGetInstruction(Instruction instruction, out PropertyDefinition propertyDefinition)
	{
		if (instruction.OpCode.IsCall())
		{
			if (instruction.Operand is MethodReference methodReference)
			{
				var mapping = _node.Mappings.FirstOrDefault(x => MethodComparer(methodReference, x.PropertyDefinition.GetMethod));
				if (mapping != null)
				{
					propertyDefinition = mapping.PropertyDefinition;
					return true;
				}
			}
		}
		propertyDefinition = null;
		return false;
	}

	public void Process()
	{
		foreach (var property in _node.TypeDefinition.Properties)
		{
			if (!property.CustomAttributes.ContainsAttribute(Constants.DoNotNotifyAttribute))
			{
				ProcessGet(property);
			}
		}
	}

	private static bool CoreMethodComparer(MethodReference methodReference, MethodDefinition methodDefinition)
	{
		return methodDefinition.GetSelfAndBaseMethods().Any(item => item == methodReference);
	}

	private static bool FieldComparer(FieldReference fieldReference, FieldDefinition fieldDefinition)
	{
		return (fieldReference.Name == fieldDefinition?.Name)
			&& (fieldReference.Resolve() == fieldDefinition);
	}

	private static bool MethodComparer(MethodReference methodReference, MethodDefinition methodDefinition)
	{
		return (methodReference.Name == methodDefinition?.Name)
			&& CoreMethodComparer(methodReference.Resolve(), methodDefinition);
	}

	private void ProcessGet(PropertyDefinition property)
	{
		//Exclude indexers
		if (property.HasParameters)
		{
			return;
		}

		var getMethod = property.GetMethod;

		//Exclude when no get
		if (getMethod == null)
		{
			return;
		}
		//Exclude when abstract
		if (getMethod.IsAbstract)
		{
			return;
		}
		var recursiveIlFinder = new RecursiveIlFinder(property.DeclaringType);
		recursiveIlFinder.Execute(getMethod);
		foreach (var instruction in recursiveIlFinder.Instructions)
		{
			ProcessInstructionForGet(property, instruction);
		}
	}

	private void ProcessInstructionForGet(PropertyDefinition property, Instruction instruction)
	{
		if (IsPropertyGetInstruction(instruction, out var usedProperty) ||
			IsFieldGetInstruction(instruction, out usedProperty))
		{
			if (usedProperty == property)
			{
				//skip where self reference
				return;
			}
			var dependency = new PropertyDependency
			{
				ShouldAlsoNotifyFor = property,
				WhenPropertyIsSet = usedProperty
			};
			_node.PropertyDependencies.Add(dependency);
		}
	}

	#endregion
}