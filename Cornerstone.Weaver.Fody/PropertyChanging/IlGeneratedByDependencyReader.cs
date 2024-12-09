#region References

using System;
using System.Linq;
using Cornerstone.Weaver.Fody.PropertyShared;
using Mono.Cecil;
using Mono.Cecil.Cil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public class IlGeneratedByDependencyReader
{
	#region Fields

	private Func<FieldReference, FieldDefinition, bool> _fieldComparer;
	private Func<MethodReference, MethodDefinition, bool> _methodComparer;
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
				var mapping = _node.Mappings.FirstOrDefault(_ => _fieldComparer(fieldReference, _.FieldDefinition));
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
				var mapping = _node.Mappings.FirstOrDefault(x => _methodComparer(methodReference, x.PropertyDefinition.GetMethod));
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
		if (_node.TypeDefinition.HasGenericParameters)
		{
			_methodComparer = GenericMethodComparer;
			_fieldComparer = GenericFieldComparer;
		}
		else
		{
			_methodComparer = NonGenericMethodComparer;
			_fieldComparer = NonGenericFieldComparer;
		}
		foreach (var property in _node.TypeDefinition.Properties)
		{
			if (!property.CustomAttributes.ContainsAttribute(Constants.AttributeOfDoNotNotify))
			{
				ProcessGet(property);
			}
		}
	}

	private static bool GenericFieldComparer(FieldReference fieldReference, FieldDefinition fieldDefinition)
	{
		return fieldDefinition == fieldReference.Resolve();
	}

	private static bool GenericMethodComparer(MethodReference methodReference, MethodDefinition methodDefinition)
	{
		return methodDefinition == methodReference.Resolve();
	}

	private static bool NonGenericFieldComparer(FieldReference fieldReference, FieldDefinition fieldDefinition)
	{
		return fieldDefinition == fieldReference;
	}

	private static bool NonGenericMethodComparer(MethodReference methodReference, MethodDefinition methodDefinition)
	{
		return methodDefinition == methodReference;
	}

	private void ProcessGet(PropertyDefinition property)
	{
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