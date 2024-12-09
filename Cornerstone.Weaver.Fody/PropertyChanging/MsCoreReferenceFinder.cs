#region References

using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanging;

public partial class WeaverForPropertyChanging
{
	#region Fields

	public MethodReference ActionConstructorReference;
	public TypeReference ActionTypeReference;
	public MethodReference ComponentModelPropertyChangingEventConstructorReference;
	public MethodReference ComponentModelPropertyChangingEventHandlerInvokeReference;
	public MethodReference DelegateCombineMethodRef;
	public MethodReference DelegateRemoveMethodRef;
	public GenericInstanceMethod InterlockedCompareExchangeForPropChangingHandler;
	public MethodDefinition NullableEqualsMethod;
	public MethodReference ObjectConstructor;
	public TypeReference PropChangingHandlerReference;
	public TypeReference PropChangingInterfaceReference;

	#endregion

	#region Methods

	public void FindCoreReferences()
	{
		var objectDefinition = ModuleWeaver.FindTypeDefinition("System.Object");
		var constructorDefinition = objectDefinition.Methods.First(_ => _.IsConstructor);
		ObjectConstructor = ModuleDefinition.ImportReference(constructorDefinition);

		var nullableDefinition = ModuleWeaver.FindTypeDefinition("System.Nullable");
		NullableEqualsMethod = ModuleDefinition.ImportReference(nullableDefinition).Resolve().Methods.First(_ => _.Name == "Equals");

		var actionDefinition = ModuleWeaver.FindTypeDefinition("System.Action");
		ActionTypeReference = ModuleDefinition.ImportReference(actionDefinition);

		var actionConstructor = actionDefinition.Methods.First(_ => _.IsConstructor);
		ActionConstructorReference = ModuleDefinition.ImportReference(actionConstructor);

		var propChangingInterfaceDefinition = ModuleWeaver.FindTypeDefinition("System.ComponentModel.INotifyPropertyChanging");
		var propChangingHandlerDefinition = ModuleWeaver.FindTypeDefinition("System.ComponentModel.PropertyChangingEventHandler");
		var propChangingArgsDefinition = ModuleWeaver.FindTypeDefinition("System.ComponentModel.PropertyChangingEventArgs");

		PropChangingInterfaceReference = ModuleDefinition.ImportReference(propChangingInterfaceDefinition);
		PropChangingHandlerReference = ModuleDefinition.ImportReference(propChangingHandlerDefinition);
		ComponentModelPropertyChangingEventHandlerInvokeReference = ModuleDefinition.ImportReference(propChangingHandlerDefinition.Methods.First(_ => _.Name == "Invoke"));
		ComponentModelPropertyChangingEventConstructorReference = ModuleDefinition.ImportReference(propChangingArgsDefinition.Methods.First(_ => _.IsConstructor));

		var delegateDefinition = ModuleWeaver.FindTypeDefinition("System.Delegate");
		var combineMethodDefinition = delegateDefinition.Methods
			.Single(_ =>
				(_.Name == "Combine") &&
				(_.Parameters.Count == 2) &&
				_.Parameters.All(_ => _.ParameterType == delegateDefinition));
		DelegateCombineMethodRef = ModuleDefinition.ImportReference(combineMethodDefinition);
		var removeMethodDefinition = delegateDefinition.Methods.First(_ => _.Name == "Remove");
		DelegateRemoveMethodRef = ModuleDefinition.ImportReference(removeMethodDefinition);

		var interlockedDefinition = ModuleWeaver.FindTypeDefinition("System.Threading.Interlocked");
		var genericCompareExchangeMethodDefinition = interlockedDefinition
			.Methods.First(_ =>
				_.IsStatic &&
				(_.Name == "CompareExchange") &&
				(_.GenericParameters.Count == 1) &&
				(_.Parameters.Count == 3));
		var genericCompareExchangeMethod = ModuleDefinition.ImportReference(genericCompareExchangeMethodDefinition);

		InterlockedCompareExchangeForPropChangingHandler = new(genericCompareExchangeMethod);
		InterlockedCompareExchangeForPropChangingHandler.GenericArguments.Add(PropChangingHandlerReference);
	}

	public static IEnumerable<string> GetAssembliesForScanning()
	{
		yield return "mscorlib";
		yield return "System";
		yield return "System.Runtime";
		yield return "System.Core";
		yield return "netstandard";
		yield return "System.Collections";
		yield return "System.ObjectModel";
		yield return "System.Threading";
		yield return "FSharp.Core";
	}

	#endregion
}