#region References

using Mono.Cecil;
using Mono.Cecil.Cil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public class DelegateHolderInjector
{
	#region Fields

	public MethodDefinition ConstructorDefinition;

	public MethodDefinition MethodDefinition;
	public MethodReference OnPropertyChangedMethodReference;
	public FieldDefinition PropertyNameField;
	public FieldDefinition TargetField;
	public TypeDefinition TargetTypeDefinition;
	public TypeDefinition TypeDefinition;
	public WeaverForPropertyChanged WeaverForPropertyChanged;

	#endregion

	#region Methods

	public void InjectDelegateHolder()
	{
		var attributes = TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.NestedPrivate | TypeAttributes.BeforeFieldInit;
		TypeDefinition = new(null, "<>PropertyNotificationDelegateHolder", attributes, WeaverForPropertyChanged.ModuleWeaver.TypeSystem.ObjectReference);
		CreateFields(TargetTypeDefinition);
		CreateOnPropChanged(OnPropertyChangedMethodReference);
		CreateConstructor();
		TargetTypeDefinition.NestedTypes.Add(TypeDefinition);
	}

	private void CreateConstructor()
	{
		var attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
		ConstructorDefinition = new(".ctor", attributes, WeaverForPropertyChanged.ModuleWeaver.TypeSystem.VoidReference);
		ConstructorDefinition.Body.Instructions.Append(
			Instruction.Create(OpCodes.Ldarg_0),
			Instruction.Create(OpCodes.Call, WeaverForPropertyChanged.ObjectConstructor),
			Instruction.Create(OpCodes.Ret)
		);
		TypeDefinition.Methods.Add(ConstructorDefinition);
	}

	private void CreateFields(TypeDefinition targetTypeDefinition)
	{
		TargetField = new("target", FieldAttributes.Public, targetTypeDefinition);
		TypeDefinition.Fields.Add(TargetField);
		PropertyNameField = new("propertyName", FieldAttributes.Public, WeaverForPropertyChanged.ModuleWeaver.TypeSystem.StringReference);
		TypeDefinition.Fields.Add(PropertyNameField);
	}

	private void CreateOnPropChanged(MethodReference onPropertyChangedMethodReference)
	{
		var attributes = MethodAttributes.Public | MethodAttributes.HideBySig;
		MethodDefinition = new("OnPropertyChanged", attributes, WeaverForPropertyChanged.ModuleWeaver.TypeSystem.VoidReference);
		MethodDefinition.Body.Instructions.Append(
			Instruction.Create(OpCodes.Ldarg_0),
			Instruction.Create(OpCodes.Ldfld, TargetField),
			Instruction.Create(OpCodes.Ldarg_0),
			Instruction.Create(OpCodes.Ldfld, PropertyNameField),
			Instruction.Create(OpCodes.Callvirt, onPropertyChangedMethodReference),
			Instruction.Create(OpCodes.Ret)
		);
		TypeDefinition.Methods.Add(MethodDefinition);
	}

	#endregion
}