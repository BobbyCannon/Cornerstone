#region References

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

#endregion

namespace Cornerstone.Weaver.Fody.PropertyChanged;

public partial class WeaverForPropertyChanged
{
	#region Fields

	public MethodReference ActionConstructorReference;
	public TypeReference ActionTypeReference;
	public MethodReference DebuggerNonUserCodeAttributeConstructor;
	public MethodReference DelegateCombineMethodRef;
	public MethodReference DelegateRemoveMethodRef;
	public TypeReference EqualityComparerTypeReference;
	public MethodReference GeneratedCodeAttributeConstructor;
	public GenericInstanceMethod InterlockedCompareExchangeForPropChangedHandler;
	public MethodDefinition NullableEqualsMethod;
	public MethodReference ObjectConstructor;
	public MethodReference ObjectEqualsMethod;
	public TypeReference PropChangedHandlerReference;
	public TypeReference PropChangedInterfaceReference;
	public TypeReference PropertyChangedEventArgsReference;
	public MethodReference PropertyChangedEventConstructorReference;
	public MethodReference PropertyChangedEventHandlerInvokeReference;
	public MethodReference StringEquals;
	public Lazy<MethodReference> Trigger;

	#endregion

	#region Methods

	public void FindCoreReferences()
	{
		var objectDefinition = ModuleWeaver.FindTypeDefinition("System.Object");
		var constructorDefinition = objectDefinition.Methods.First(_ => _.IsConstructor);
		ObjectConstructor = ModuleDefinition.ImportReference(constructorDefinition);
		var objectEqualsMethodDefinition = objectDefinition.Methods
			.First(_ => (_.Name == "Equals") &&
				(_.Parameters.Count == 2));
		ObjectEqualsMethod = ModuleDefinition.ImportReference(objectEqualsMethodDefinition);

		var stringEquals = ModuleWeaver.FindTypeDefinition("System.String")
			.Methods
			.First(_ => _.IsStatic &&
				(_.Name == "Equals") &&
				(_.Parameters.Count == 3) &&
				(_.Parameters[0].ParameterType.Name == "String") &&
				(_.Parameters[1].ParameterType.Name == "String") &&
				(_.Parameters[2].ParameterType.Name == "StringComparison"));
		StringEquals = ModuleDefinition.ImportReference(stringEquals);

		var nullableDefinition = ModuleWeaver.FindTypeDefinition("System.Nullable");
		NullableEqualsMethod = ModuleDefinition.ImportReference(nullableDefinition).Resolve().Methods.First(_ => _.Name == "Equals");

		EqualityComparerTypeReference = ModuleWeaver.FindTypeDefinition("System.Collections.Generic.EqualityComparer`1");

		var actionDefinition = ModuleWeaver.FindTypeDefinition("System.Action");
		ActionTypeReference = ModuleDefinition.ImportReference(actionDefinition);

		var actionConstructor = actionDefinition.Methods.First(_ => _.IsConstructor);
		ActionConstructorReference = ModuleDefinition.ImportReference(actionConstructor);

		var propChangedInterfaceDefinition = ModuleWeaver.FindTypeDefinition("System.ComponentModel.INotifyPropertyChanged");
		PropChangedInterfaceReference = ModuleDefinition.ImportReference(propChangedInterfaceDefinition);
		var propChangedHandlerDefinition = ModuleWeaver.FindTypeDefinition("System.ComponentModel.PropertyChangedEventHandler");
		PropChangedHandlerReference = ModuleDefinition.ImportReference(propChangedHandlerDefinition);
		PropertyChangedEventHandlerInvokeReference = ModuleDefinition.ImportReference(propChangedHandlerDefinition.Methods.First(_ => _.Name == "Invoke"));
		var propChangedArgsDefinition = ModuleWeaver.FindTypeDefinition("System.ComponentModel.PropertyChangedEventArgs");
		PropertyChangedEventArgsReference = ModuleDefinition.ImportReference(propChangedArgsDefinition);
		PropertyChangedEventConstructorReference = ModuleDefinition.ImportReference(propChangedArgsDefinition.Methods.First(_ => _.IsConstructor));

		var delegateDefinition = ModuleWeaver.FindTypeDefinition("System.Delegate");
		var combineMethodDefinition = delegateDefinition.Methods
			.Single(x =>
				(x.Name == "Combine") &&
				(x.Parameters.Count == 2) &&
				x.Parameters.All(p => p.ParameterType == delegateDefinition));
		DelegateCombineMethodRef = ModuleDefinition.ImportReference(combineMethodDefinition);
		var removeMethodDefinition = delegateDefinition.Methods.First(_ => _.Name == "Remove");
		DelegateRemoveMethodRef = ModuleDefinition.ImportReference(removeMethodDefinition);

		var interlockedDefinition = ModuleWeaver.FindTypeDefinition("System.Threading.Interlocked");
		var genericCompareExchangeMethodDefinition = interlockedDefinition
			.Methods.First(x =>
				x.IsStatic &&
				(x.Name == "CompareExchange") &&
				(x.GenericParameters.Count == 1) &&
				(x.Parameters.Count == 3));
		var genericCompareExchangeMethod = ModuleDefinition.ImportReference(genericCompareExchangeMethodDefinition);

		InterlockedCompareExchangeForPropChangedHandler = new(genericCompareExchangeMethod);
		InterlockedCompareExchangeForPropChangedHandler.GenericArguments.Add(PropChangedHandlerReference);
		Trigger = new(() =>
		{
			if (ModuleWeaver.TryFindTypeDefinition("Microsoft.FSharp.Control.FSharpEvent`2", out var fSharpEvent))
			{
				var trigger = fSharpEvent.Methods.Single(_ => _.Name == "Trigger");
				return ModuleDefinition.ImportReference(trigger.MakeGeneric(PropChangedHandlerReference, propChangedArgsDefinition));
			}

			return null;
		});

		var generatedCodeType = ModuleWeaver.FindTypeDefinition("System.CodeDom.Compiler.GeneratedCodeAttribute");
		var generatedCodeAttributeConstructor = generatedCodeType.GetConstructors().Single(c => (c.Parameters.Count == 2) && c.Parameters.All(p => p.ParameterType.Name == "String"));
		GeneratedCodeAttributeConstructor = ModuleDefinition.ImportReference(generatedCodeAttributeConstructor);

		var debuggerNonUserCodeType = ModuleWeaver.FindTypeDefinition("System.Diagnostics.DebuggerNonUserCodeAttribute");
		var debuggerNonUserCodeConstructor = debuggerNonUserCodeType.GetConstructors().Single(c => !c.HasParameters);
		DebuggerNonUserCodeAttributeConstructor = ModuleDefinition.ImportReference(debuggerNonUserCodeConstructor);
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
		//TODO: remove when move to only netstandard2.0
		yield return "System.Diagnostics.Tools";
		yield return "System.Diagnostics.Debug";
	}

	#endregion
}