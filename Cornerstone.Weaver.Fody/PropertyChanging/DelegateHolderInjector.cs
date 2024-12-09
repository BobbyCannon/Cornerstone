#region References

using Mono.Cecil;
using Mono.Cecil.Cil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public class DelegateHolderInjector
{
	#region Fields

	public MethodDefinition ConstructorDefinition;
	public MethodDefinition MethodDefinition;
	public MethodReference OnPropertyChangingMethodReference;
	public FieldDefinition PropertyName;
	public FieldDefinition Target;
	public TypeDefinition TargetTypeDefinition;
	public TypeDefinition TypeDefinition;
	public WeaverForPropertyChanging WeaverForPropertyChanging;

	#endregion

	#region Methods

	public void InjectDelegateHolder()
	{
		var attributes = TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.NestedPrivate | TypeAttributes.BeforeFieldInit;
		TypeDefinition = new(null, "<>PropertyNotificationDelegateHolder", attributes, WeaverForPropertyChanging.ModuleWeaver.TypeSystem.ObjectReference);
		CreateFields(TargetTypeDefinition);
		CreateOnPropChanging(OnPropertyChangingMethodReference);
		CreateConstructor();
		TargetTypeDefinition.NestedTypes.Add(TypeDefinition);
	}

	private void CreateConstructor()
	{
		var attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
		ConstructorDefinition = new(".ctor", attributes, WeaverForPropertyChanging.ModuleWeaver.TypeSystem.VoidReference);
		ConstructorDefinition.Body.Instructions.Append(
			Instruction.Create(OpCodes.Ldarg_0),
			Instruction.Create(OpCodes.Call, WeaverForPropertyChanging.ObjectConstructor),
			Instruction.Create(OpCodes.Ret)
		);
		TypeDefinition.Methods.Add(ConstructorDefinition);
	}

	private void CreateFields(TypeDefinition targetTypeDefinition)
	{
		Target = new("target", FieldAttributes.Public, targetTypeDefinition);
		TypeDefinition.Fields.Add(Target);
		PropertyName = new("propertyName", FieldAttributes.Public, WeaverForPropertyChanging.ModuleWeaver.TypeSystem.StringReference);
		TypeDefinition.Fields.Add(PropertyName);
	}

	private void CreateOnPropChanging(MethodReference onPropertyChangingMethodReference)
	{
		var attributes = MethodAttributes.Public | MethodAttributes.HideBySig;
		MethodDefinition = new("OnPropertyChanging", attributes, WeaverForPropertyChanging.ModuleWeaver.TypeSystem.VoidReference);
		MethodDefinition.Body.Instructions.Append(
			Instruction.Create(OpCodes.Ldarg_0),
			Instruction.Create(OpCodes.Ldfld, Target),
			Instruction.Create(OpCodes.Ldarg_0),
			Instruction.Create(OpCodes.Ldfld, PropertyName),
			Instruction.Create(OpCodes.Callvirt, onPropertyChangingMethodReference),
			Instruction.Create(OpCodes.Ret)
		);
		TypeDefinition.Methods.Add(MethodDefinition);
	}

	#endregion
}