#region References

using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyShared;

public partial class WeaverForPropertyChange
{
	#region Methods

	public void FindMappings()
	{
		FindMappings(NotifyNodes);
	}

	public static IEnumerable<MemberMapping> GetMappings(TypeDefinition typeDefinition)
	{
		foreach (var property in typeDefinition.Properties)
		{
			var fieldDefinition = TryGetField(typeDefinition, property);
			yield return new()
			{
				PropertyDefinition = property,
				FieldDefinition = fieldDefinition
			};
		}
	}

	private static void FindMappings(List<TypeNode> notifyNodes)
	{
		foreach (var node in notifyNodes)
		{
			var typeDefinition = node.TypeDefinition;
			node.Mappings = GetMappings(typeDefinition).ToList();
			FindMappings(node.Nodes);
		}
	}

	private static FieldDefinition GetSingleField(PropertyDefinition property)
	{
		var fieldDefinition = GetSingleField(property, Code.Stfld, property.SetMethod);
		if (fieldDefinition != null)
		{
			return fieldDefinition;
		}
		return GetSingleField(property, Code.Ldfld, property.GetMethod);
	}

	private static FieldDefinition GetSingleField(PropertyDefinition property, Code code, MethodDefinition methodDefinition)
	{
		if (methodDefinition?.Body == null)
		{
			return null;
		}
		FieldReference fieldReference = null;
		foreach (var instruction in methodDefinition.Body.Instructions)
		{
			if (instruction.OpCode.Code == code)
			{
				//if fieldReference is not null then we are at the second one
				if (fieldReference != null)
				{
					return null;
				}

				if (instruction.Operand is not FieldReference field)
				{
					continue;
				}
				if (field.DeclaringType != property.DeclaringType)
				{
					continue;
				}
				if (field.FieldType != property.PropertyType)
				{
					continue;
				}
				var fieldDef = instruction.Operand as FieldDefinition;
				var fieldAttributes = fieldDef?.Attributes & FieldAttributes.InitOnly;
				if (fieldAttributes == FieldAttributes.InitOnly)
				{
					continue;
				}
				fieldReference = field;
			}
		}
		return fieldReference?.Resolve();
	}

	private static FieldDefinition TryGetField(TypeDefinition typeDefinition, PropertyDefinition property)
	{
		var propertyName = property.Name;
		var fieldsWithSameType = typeDefinition.Fields
			.Where(_ => (_.DeclaringType == typeDefinition) &&
				(_.FieldType.Resolve() == property.PropertyType.Resolve())).ToList();
		foreach (var field in fieldsWithSameType)
		{
			//AutoProp
			if (field.Name == $"<{propertyName}>k__BackingField")
			{
				return field;
			}
		}

		foreach (var field in fieldsWithSameType)
		{
			//diffCase
			var upperPropertyName = propertyName.ToUpper();
			var fieldUpper = field.Name.ToUpper();
			if (fieldUpper == upperPropertyName)
			{
				return field;
			}
			//underScore
			if (fieldUpper == ("_" + upperPropertyName))
			{
				return field;
			}
		}
		return GetSingleField(property);
	}

	#endregion
}