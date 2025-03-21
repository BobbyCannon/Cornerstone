#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using CustomAttributeNamedArgument = Mono.Cecil.CustomAttributeNamedArgument;
using ICustomAttributeProvider = Mono.Cecil.ICustomAttributeProvider;
using MethodBody = Mono.Cecil.Cil.MethodBody;

#endregion

namespace Cornerstone.Weaver.Fody.FodyTools;

/// <summary>
/// A class to import code from one module to another; like e.g. ILMerge, but only imports the specified classes and their local references.
/// </summary>
/// <remarks>
/// The main task of this is to copy code fragments into another module so they can be used by the weaver.
/// It copies the types specified in the Import method, and automatically copies all required dependencies.
/// </remarks>
internal sealed class CodeImporter
{
	#region Fields

	private readonly IList<Action> _deferredActions = new List<Action>();

	// ReSharper disable once AssignNullToNotNullAttribute
	private static readonly ConstructorInfo _instructionConstructor = typeof(Instruction).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(OpCode), typeof(object)], null);
	private readonly Dictionary<string, ModuleDefinition> _sourceModuleDefinitions = new();
	private readonly Dictionary<MethodDefinition, MethodDefinition> _targetMethods = new();
	private readonly HashSet<TypeDefinition> _targetTypes = [];
	private readonly Dictionary<TypeDefinition, TypeDefinition> _targetTypesBySource = new();

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="CodeImporter" /> class.
	/// </summary>
	/// <param name="targetModule"> The target module into which the specified code will be imported. </param>
	public CodeImporter(ModuleDefinition targetModule)
	{
		TargetModule = targetModule;
		AssemblyResolver = targetModule.AssemblyResolver;
	}

	#endregion

	#region Properties

	public IAssemblyResolver AssemblyResolver { get; set; }

	public bool HideImportedTypes { get; set; } = true;

	public IModuleResolver ModuleResolver { get; set; }

	public Func<string, string> NamespaceDecorator { get; set; } = value => value;

	public ModuleDefinition TargetModule { get; }

	private TypeReference TemporaryPlaceholderType => new("temporary", "type", TargetModule, TargetModule);

	#endregion

	#region Methods

	/// <summary>
	/// Imports the specified type and it's local references from it's source module into the target module.
	/// </summary>
	/// <param name="type"> The type to import. </param>
	/// <returns>
	/// The type definition of the imported type in the target module.
	/// </returns>
	public TypeDefinition Import(Type type)
	{
		return ImportType(type);
	}

	/// <summary>
	/// Imports the specified type and it's local references from it's source module into the target module.
	/// </summary>
	/// <typeparam name="T"> The type to import. </typeparam>
	/// <returns>
	/// The type definition of the imported type in the target module.
	/// </returns>
	public TypeDefinition Import<T>()
	{
		return Import(typeof(T));
	}

	public TypeDefinition Import(TypeDefinition sourceType)
	{
		RegisterSourceModule(sourceType.Module);

		return ProcessDeferredActions(ImportTypeDefinition(sourceType));
	}

	public GenericInstanceMethod Import(GenericInstanceMethod method)
	{
		return ProcessDeferredActions(ImportGenericInstanceMethod(method));
	}

	/// <summary>
	/// Imports the event's declaring type into the target module and returns the event definition
	/// of the corresponding event in the target module.
	/// </summary>
	/// <typeparam name="T"> The event type of the event </typeparam>
	/// <param name="expression"> The event expression describing the source event. </param>
	/// <returns> The event definition of the imported event </returns>
	/// <exception cref="ArgumentException">
	/// Only a member expression is supported here. - expression
	/// or
	/// Only a event expression is supported here. - expression
	/// </exception>
	public EventDefinition ImportEvent<T>(Expression<Func<T>> expression)
	{
		if (!(expression.Body is MemberExpression memberExpression))
		{
			throw new ArgumentException("Only a member expression is supported here.", nameof(expression));
		}

		var member = memberExpression.Member;
		if (!(member is EventInfo))
		{
			throw new ArgumentException("Only a event expression is supported here.", nameof(expression));
		}

		var targetType = Import(member.GetDeclaringType());
		var eventName = member.Name;

		return targetType.Events.Single(m => m.Name == eventName);
	}

	/// <summary>
	/// Imports the field's declaring type into the target module and returns the field definition
	/// of the corresponding field in the target module.
	/// </summary>
	/// <typeparam name="T"> The field type of the field </typeparam>
	/// <param name="expression"> The field expression describing the source field. </param>
	/// <returns> The field definition of the imported field </returns>
	/// <exception cref="ArgumentException">
	/// Only a member expression is supported here. - expression
	/// or
	/// Only a field expression is supported here. - expression
	/// </exception>
	public FieldDefinition ImportField<T>(Expression<Func<T>> expression)
	{
		if (!(expression.Body is MemberExpression memberExpression))
		{
			throw new ArgumentException("Only a member expression is supported here.", nameof(expression));
		}

		var member = memberExpression.Member;
		if (!(member is FieldInfo))
		{
			throw new ArgumentException("Only a field expression is supported here.", nameof(expression));
		}

		var targetType = Import(member.GetDeclaringType());
		var fieldName = member.Name;

		return targetType.Fields.Single(m => m.Name == fieldName);
	}

	/// <summary>
	/// Imports the methods declaring type into the target module and returns the method definition
	/// of the corresponding method in the target module.
	/// </summary>
	/// <typeparam name="T"> The methods return value. </typeparam>
	/// <param name="expression"> The method call expression describing the source method. </param>
	/// <returns> The method definition of the imported method. </returns>
	/// <exception cref="ArgumentException"> Only method call expression is supported. - expression </exception>
	/// <exception cref="InvalidOperationException"> Importing method failed. </exception>
	public MethodDefinition ImportMethod<T>(Expression<Func<T>> expression)
	{
		return ImportMethodInternal(expression);
	}

	/// <summary>
	/// Imports the methods declaring type into the target module and returns the method definition
	/// of the corresponding method in the target module.
	/// </summary>
	/// <param name="expression"> The method call expression describing the source method. </param>
	/// <returns> The method definition of the imported method. </returns>
	/// <exception cref="ArgumentException"> Only method call expression is supported. - expression </exception>
	/// <exception cref="InvalidOperationException"> Importing method failed. </exception>
	public MethodDefinition ImportMethod(Expression<Action> expression)
	{
		return ImportMethodInternal(expression);
	}

	public MethodReference ImportMethod(MethodReference source)
	{
		switch (source)
		{
			case MethodDefinition methodDefinition:
				return ImportMethodDefinition(methodDefinition, ImportTypeDefinition(source.DeclaringType.Resolve()));

			case null:
				return null;

			default:
				return ImportMethodReference(source);
		}
	}

	/// <summary>
	/// Imports the property's declaring type into the target module and returns the property definition
	/// of the corresponding property in the target module.
	/// </summary>
	/// <typeparam name="T"> The property type of the property </typeparam>
	/// <param name="expression"> The property expression describing the source property. </param>
	/// <returns> The property definition of the imported property </returns>
	/// <exception cref="ArgumentException">
	/// Only a member expression is supported here. - expression
	/// or
	/// Only a property expression is supported here. - expression
	/// </exception>
	public PropertyDefinition ImportProperty<T>(Expression<Func<T>> expression)
	{
		if (!(expression.Body is MemberExpression memberExpression))
		{
			throw new ArgumentException("Only a member expression is supported here.", nameof(expression));
		}

		var member = memberExpression.Member;
		if (!(member is PropertyInfo))
		{
			throw new ArgumentException("Only a property expression is supported here.", nameof(expression));
		}

		var targetType = Import(member.GetDeclaringType());
		var propertyName = member.Name;

		return targetType.Properties.Single(m => m.Name == propertyName);
	}

	[return: NotNullIfNotNull("source")]
	public TypeReference ImportType(TypeReference source, MethodReference targetMethod)
	{
		return ProcessDeferredActions(InternalImportType(source, targetMethod));
	}

	public bool IsLocalOrExternalReference(TypeReference typeReference)
	{
		var scope = typeReference.Scope;

		string assemblyName;

		switch (scope)
		{
			case AssemblyNameReference assemblyNameReference:
				assemblyName = assemblyNameReference.FullName;
				break;

			case ModuleDefinition moduleDefinition:
				assemblyName = moduleDefinition.Assembly.FullName;
				break;

			default:
				return false;
		}

		if (string.Equals(TargetModule.Assembly.FullName, assemblyName, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return !_sourceModuleDefinitions.ContainsKey(assemblyName)
			&& !ResolveModule(typeReference, assemblyName);
	}

	/// <summary>
	/// Returns a collection of the imported modules.
	/// </summary>
	/// <returns> The collection of imported modules. </returns>
	public ICollection<ModuleDefinition> ListImportedModules()
	{
		return _sourceModuleDefinitions.Values;
	}

	/// <summary>
	/// Returns a collection of the imported types.
	/// </summary>
	/// <returns> The collection of imported types. </returns>
	public IDictionary<TypeDefinition, TypeDefinition> ListImportedTypes(bool includeNested = false)
	{
		return _targetTypesBySource
			.Where(t => includeNested || (t.Value?.DeclaringType == null))
			.ToDictionary(item => item.Key, item => item.Value);
	}

	public void RegisterSourceModule(ModuleDefinition sourceModule)
	{
		var assemblyName = sourceModule.Assembly.FullName;

		if (_sourceModuleDefinitions.ContainsKey(assemblyName))
		{
			return;
		}

		if (!sourceModule.HasSymbols)
		{
			try
			{
				sourceModule.ReadSymbols();
			}
			catch
			{
				// module has no symbols, just go without...
			}
		}

		_sourceModuleDefinitions[assemblyName] = sourceModule;
	}

	[SuppressMessage("ReSharper", "ImplicitlyCapturedClosure")]
	private Instruction CloneInstruction(Instruction source, MethodDefinition targetMethod, IReadOnlyDictionary<Instruction, Instruction> instructionMap)
	{
		var targetInstruction = (Instruction) _instructionConstructor.Invoke([source.OpCode, source.Operand]);

		switch (targetInstruction.Operand)
		{
			case MethodDefinition sourceMethodDefinition:
				var targetType = ImportTypeDefinition(sourceMethodDefinition.DeclaringType);
				ExecuteDeferred(Priority.Operands, () => targetInstruction.Operand = ImportMethodDefinition(sourceMethodDefinition, targetType));
				break;

			case GenericInstanceMethod genericInstanceMethod:
				ExecuteDeferred(Priority.Operands, () => targetInstruction.Operand = ImportGenericInstanceMethod(genericInstanceMethod));
				break;

			case MethodReference sourceMethodReference:
				ExecuteDeferred(Priority.Operands, () => targetInstruction.Operand = ImportMethodReference(sourceMethodReference));
				break;

			case TypeReference typeReference:
				targetInstruction.Operand = InternalImportType(typeReference, targetMethod);
				break;

			case FieldReference fieldReference:
				ExecuteDeferred(Priority.Operands, () => targetInstruction.Operand = new FieldReference(fieldReference.Name, InternalImportType(fieldReference.FieldType, targetMethod), InternalImportType(fieldReference.DeclaringType, targetMethod)));
				break;

			case Instruction instruction:
				ExecuteDeferred(Priority.Operands, () => targetInstruction.Operand = instructionMap[instruction]);
				break;

			case Instruction[] instructions:
				ExecuteDeferred(Priority.Operands, () => targetInstruction.Operand = instructions.Select(instruction => instructionMap[instruction]).ToArray());
				break;
		}

		return targetInstruction;
	}

	private static SequencePoint CloneSequencePoint(Instruction instruction, SequencePoint sequencePoint)
	{
		return new SequencePoint(instruction, sequencePoint.Document)
		{
			StartLine = sequencePoint.StartLine,
			StartColumn = sequencePoint.StartColumn,
			EndLine = sequencePoint.EndLine,
			EndColumn = sequencePoint.EndColumn
		};
	}

	private void CopyAttributes(ICustomAttributeProvider source, ICustomAttributeProvider target)
	{
		if (!source.HasCustomAttributes)
		{
			return;
		}

		foreach (var sourceAttribute in source.CustomAttributes)
		{
			var constructor = ImportMethod(sourceAttribute.Constructor);

			if (constructor == null)
			{
				continue;
			}

			var targetAttribute = new CustomAttribute(constructor);

			if (sourceAttribute.HasConstructorArguments)
			{
				foreach (var a in sourceAttribute.ConstructorArguments)
				{
					targetAttribute.ConstructorArguments.Add(new CustomAttributeArgument(InternalImportType(a.Type, null), a.Value));
				}
			}

			if (sourceAttribute.HasProperties)
			{
				foreach (var property in sourceAttribute.Properties)
				{
					targetAttribute.Properties.Add(new CustomAttributeNamedArgument(property.Name, new CustomAttributeArgument(InternalImportType(property.Argument.Type, null), property.Argument.Value)));
				}
			}

			if (sourceAttribute.HasFields)
			{
				foreach (var field in sourceAttribute.Fields)
				{
					targetAttribute.Fields.Add(new CustomAttributeNamedArgument(field.Name, new CustomAttributeArgument(InternalImportType(field.Argument.Type, null), field.Argument.Value)));
				}
			}

			target.CustomAttributes.Add(targetAttribute);
		}
	}

	private void CopyConstraints(GenericParameter sourceParameter, GenericParameter targetParameter, MethodReference targetMethod)
	{
		if (!sourceParameter.HasConstraints)
		{
			return;
		}

		foreach (var source in sourceParameter.Constraints)
		{
			var target = new GenericParameterConstraint(InternalImportType(source.ConstraintType, targetMethod));
			CopyAttributes(source, target);
			targetParameter.Constraints.Add(target);
		}
	}

	private void CopyEvents(TypeDefinition source, TypeDefinition target)
	{
		foreach (var sourceDefinition in source.Events)
		{
			var targetDefinition = new EventDefinition(sourceDefinition.Name, sourceDefinition.Attributes, InternalImportType(sourceDefinition.EventType, null))
			{
				AddMethod = ImportMethodDefinition(sourceDefinition.AddMethod, target),
				RemoveMethod = ImportMethodDefinition(sourceDefinition.RemoveMethod, target)
			};

			CopyAttributes(sourceDefinition, targetDefinition);

			target.Events.Add(targetDefinition);
		}
	}

	private void CopyExceptionHandlers(MethodBody source, MethodBody target)
	{
		if (!source.HasExceptionHandlers)
		{
			return;
		}

		foreach (var sourceHandler in source.ExceptionHandlers)
		{
			var targetHandler = new ExceptionHandler(sourceHandler.HandlerType);
			var sourceInstructions = source.Instructions;
			var targetInstructions = target.Instructions;

			if (sourceHandler.TryStart != null)
			{
				targetHandler.TryStart = targetInstructions[sourceInstructions.IndexOf(sourceHandler.TryStart)];
			}

			if (sourceHandler.TryEnd != null)
			{
				targetHandler.TryEnd = targetInstructions[sourceInstructions.IndexOf(sourceHandler.TryEnd)];
			}

			if (sourceHandler.HandlerStart != null)
			{
				targetHandler.HandlerStart = targetInstructions[sourceInstructions.IndexOf(sourceHandler.HandlerStart)];
			}

			if (sourceHandler.HandlerEnd != null)
			{
				targetHandler.HandlerEnd = targetInstructions[sourceInstructions.IndexOf(sourceHandler.HandlerEnd)];
			}

			if (sourceHandler.FilterStart != null)
			{
				targetHandler.FilterStart = targetInstructions[sourceInstructions.IndexOf(sourceHandler.FilterStart)];
			}

			if (sourceHandler.CatchType != null)
			{
				targetHandler.CatchType = InternalImportType(sourceHandler.CatchType, null);
			}

			target.ExceptionHandlers.Add(targetHandler);
		}
	}

	private void CopyFields(TypeDefinition source, TypeDefinition target)
	{
		foreach (var sourceDefinition in source.Fields)
		{
			var fieldName = sourceDefinition.Name;

			var targetDefinition = new FieldDefinition(fieldName, sourceDefinition.Attributes, InternalImportType(sourceDefinition.FieldType, null))
			{
				InitialValue = sourceDefinition.InitialValue,
				Offset = sourceDefinition.Offset
			};

			if (sourceDefinition.HasConstant)
			{
				targetDefinition.Constant = sourceDefinition.Constant;
			}

			if (sourceDefinition.HasMarshalInfo)
			{
				targetDefinition.MarshalInfo = sourceDefinition.MarshalInfo;
			}

			CopyAttributes(sourceDefinition, targetDefinition);

			target.Fields.Add(targetDefinition);
		}
	}

	private void CopyGenericParameters(TypeDefinition source, TypeDefinition target)
	{
		if (!source.HasGenericParameters)
		{
			return;
		}

		foreach (var sourceParameter in source.GenericParameters)
		{
			var targetParameter = new GenericParameter(sourceParameter.Name, InternalImportType(sourceParameter.DeclaringType, null))
			{
				Attributes = sourceParameter.Attributes
			};

			CopyAttributes(sourceParameter, targetParameter);
			CopyConstraints(sourceParameter, targetParameter, null);

			target.GenericParameters.Add(targetParameter);
		}
	}

	private void CopyGenericParameters(MethodReference source, MethodReference target)
	{
		if (!source.HasGenericParameters)
		{
			return;
		}

		foreach (var sourceParameter in source.GenericParameters)
		{
			var provider = sourceParameter.Type == GenericParameterType.Method
				? (IGenericParameterProvider) target
				: InternalImportType(sourceParameter.DeclaringType, null);

			var targetParameter = new GenericParameter(sourceParameter.Name, provider)
			{
				Attributes = sourceParameter.Attributes
			};

			CopyAttributes(sourceParameter, targetParameter);
			CopyConstraints(sourceParameter, targetParameter, target);

			target.GenericParameters.Add(targetParameter);
		}
	}

	private void CopyInstructions(MethodDefinition source, MethodDefinition target)
	{
		var targetDebugInformation = target.DebugInformation;
		var sourceDebugInformation = source.DebugInformation;

		var sourceBody = source.Body;
		var targetBody = target.Body;

		var sourceInstructions = sourceBody.Instructions;
		var targetInstructions = targetBody.Instructions;

		var instructionMap = new Dictionary<Instruction, Instruction>();

		foreach (var sourceInstruction in sourceInstructions)
		{
			var targetInstruction = CloneInstruction(sourceInstruction, target, instructionMap);

			instructionMap.Add(sourceInstruction, targetInstruction);

			targetInstructions.Add(targetInstruction);

			var sequencePoint = sourceDebugInformation?.GetSequencePoint(sourceInstruction);
			if (sequencePoint != null)
			{
				targetDebugInformation?.SequencePoints?.Add(CloneSequencePoint(targetInstruction, sequencePoint));
			}
		}

		CopyExceptionHandlers(sourceBody, targetBody);

		if ((sourceDebugInformation == null) || (true != targetDebugInformation?.HasSequencePoints))
		{
			return;
		}

		var scope = targetDebugInformation.Scope = new ScopeDebugInformation(targetInstructions.First(), targetInstructions.Last());

		foreach (var variable in sourceDebugInformation.Scope.Variables)
		{
			var targetVariable = targetBody.Variables[variable.Index];

			scope.Variables.Add(new VariableDebugInformation(targetVariable, variable.Name));
		}
	}

	private void CopyMethods(TypeDefinition source, TypeDefinition target)
	{
		foreach (var method in source.Methods)
		{
			ImportMethodDefinition(method, target);
		}
	}

	private void CopyParameters(IMethodSignature sourceMethod, MethodReference targetMethod)
	{
		foreach (var sourceParameter in sourceMethod.Parameters)
		{
			var targetParameter = new ParameterDefinition(sourceParameter.Name, sourceParameter.Attributes, InternalImportType(sourceParameter.ParameterType, targetMethod));

			CopyAttributes(sourceParameter, targetParameter);

			if (sourceParameter.HasMarshalInfo)
			{
				targetParameter.MarshalInfo = sourceParameter.MarshalInfo;
			}

			if (sourceParameter.HasConstant)
			{
				targetParameter.Constant = sourceParameter.Constant;
			}

			targetMethod.Parameters.Add(targetParameter);
		}
	}

	private void CopyProperties(TypeDefinition source, TypeDefinition target)
	{
		foreach (var sourceDefinition in source.Properties)
		{
			var targetDefinition = new PropertyDefinition(sourceDefinition.Name, sourceDefinition.Attributes, InternalImportType(sourceDefinition.PropertyType, null))
			{
				GetMethod = ImportMethodDefinition(sourceDefinition.GetMethod, target),
				SetMethod = ImportMethodDefinition(sourceDefinition.SetMethod, target)
			};

			CopyAttributes(sourceDefinition, targetDefinition);

			target.Properties.Add(targetDefinition);
		}
	}

	private void CopyReturnType(IMethodSignature source, MethodReference target)
	{
		var sourceReturnType = source.MethodReturnType;

		var targetReturnType = new MethodReturnType(target)
		{
			ReturnType = InternalImportType(source.ReturnType, target),
			MarshalInfo = sourceReturnType.MarshalInfo,
			Attributes = sourceReturnType.Attributes,
			Name = sourceReturnType.Name
		};

		if (sourceReturnType.HasConstant)
		{
			targetReturnType.Constant = sourceReturnType.Constant;
		}

		CopyAttributes(sourceReturnType, targetReturnType);

		target.MethodReturnType = targetReturnType;
	}

	private void ExecuteDeferred(Priority priority, Action action)
	{
		switch (priority)
		{
			case Priority.Instructions:
				_deferredActions.Insert(0, action);
				break;

			default:
				_deferredActions.Add(action);
				break;
		}
	}

	private GenericInstanceMethod ImportGenericInstanceMethod(GenericInstanceMethod source)
	{
		var elementMethod = source.ElementMethod;

		switch (source.ElementMethod)
		{
			case MethodDefinition sourceMethodDefinition:
				elementMethod = ImportMethodDefinition(sourceMethodDefinition, ImportTypeDefinition(sourceMethodDefinition.DeclaringType))!;
				break;

			case GenericInstanceMethod genericInstanceMethod:
				elementMethod = ImportGenericInstanceMethod(genericInstanceMethod);
				break;

			// ReSharper disable once PatternAlwaysOfType
			case MethodReference sourceMethodReference:
				elementMethod = ImportMethodReference(sourceMethodReference);
				break;
		}

		var target = new GenericInstanceMethod(elementMethod);

		foreach (var genericArgument in source.GenericArguments)
		{
			target.GenericArguments.Add(InternalImportType(genericArgument, target));
		}

		return target;
	}

	private TypeReference ImportGenericInstanceType(GenericInstanceType source, MethodReference targetMethod)
	{
		var target = new GenericInstanceType(InternalImportType(source.ElementType, targetMethod));

		foreach (var genericArgument in source.GenericArguments)
		{
			target.GenericArguments.Add(InternalImportType(genericArgument, targetMethod));
		}

		return target;
	}

	private TypeReference ImportGenericParameter(GenericParameter source, MethodReference targetMethod)
	{
		var genericParameterProvider = source.Type == GenericParameterType.Method
			? targetMethod ?? throw new InvalidOperationException("Need a method reference for generic method parameter.")
			: (IGenericParameterProvider) InternalImportType(source.DeclaringType, targetMethod)!;

		var index = source.Position;

		if (index < genericParameterProvider?.GenericParameters.Count)
		{
			return genericParameterProvider.GenericParameters[index];
		}

		return source;
	}

	private void ImportMethodBody(MethodDefinition source, MethodDefinition target)
	{
		if (!source.HasBody)
		{
			return;
		}

		var sourceMethodBody = source.Body;
		var targetMethodBody = target.Body;

		targetMethodBody.InitLocals = sourceMethodBody.InitLocals;

		foreach (var sourceVariable in sourceMethodBody.Variables)
		{
			targetMethodBody.Variables.Add(new VariableDefinition(InternalImportType(sourceVariable.VariableType, target)));
		}

		ExecuteDeferred(Priority.Instructions, () => CopyInstructions(source, target));
	}

	private MethodDefinition ImportMethodDefinition(MethodDefinition sourceDefinition, TypeDefinition targetType)
	{
		if (sourceDefinition == null)
		{
			return null;
		}

		if (IsLocalOrExternalReference(sourceDefinition.DeclaringType))
		{
			return sourceDefinition;
		}

		if (_targetMethods.TryGetValue(sourceDefinition, out var target))
		{
			return target;
		}

		target = new MethodDefinition(sourceDefinition.Name, sourceDefinition.Attributes, TemporaryPlaceholderType)
		{
			ImplAttributes = sourceDefinition.ImplAttributes
		};

		_targetMethods.Add(sourceDefinition, target);

		foreach (var sourceOverride in sourceDefinition.Overrides)
		{
			switch (sourceOverride)
			{
				case MethodDefinition methodDefinition:
					target.Overrides.Add(ImportMethodDefinition(methodDefinition, targetType));
					break;

				default:
					target.Overrides.Add(ImportMethodReference(sourceOverride));
					break;
			}
		}

		CopyAttributes(sourceDefinition, target);
		CopyGenericParameters(sourceDefinition, target);
		CopyParameters(sourceDefinition, target);

		targetType.Methods.Add(target);

		if (sourceDefinition.IsPInvokeImpl)
		{
			var moduleRef = TargetModule.ModuleReferences
				.FirstOrDefault(mr => mr.Name == sourceDefinition.PInvokeInfo.Module.Name);

			if (moduleRef == null)
			{
				moduleRef = new ModuleReference(sourceDefinition.PInvokeInfo.Module.Name);
				TargetModule.ModuleReferences.Add(moduleRef);
			}

			target.PInvokeInfo = new PInvokeInfo(sourceDefinition.PInvokeInfo.Attributes, sourceDefinition.PInvokeInfo.EntryPoint, moduleRef);
		}

		CopyReturnType(sourceDefinition, target);

		ImportMethodBody(sourceDefinition, target);

		return target;
	}

	private MethodDefinition ImportMethodInternal(LambdaExpression expression)
	{
		expression.GetMethodInfo(out var declaringType, out var methodName, out var argumentTypes);

		var targetType = Import(declaringType);

		return targetType.Methods.Single(m => (m.Name == methodName) && m.Parameters.ParametersMatch(argumentTypes)) ?? throw new InvalidOperationException("Importing method failed.");
	}

	private MethodReference ImportMethodReference(MethodReference source)
	{
		Debug.Assert(source.GetType() == typeof(MethodReference));

		var target = new MethodReference(source.Name, TemporaryPlaceholderType)
		{
			HasThis = source.HasThis,
			ExplicitThis = source.ExplicitThis,
			CallingConvention = source.CallingConvention
		};

		CopyGenericParameters(source, target);
		CopyParameters(source, target);

		target.DeclaringType = InternalImportType(source.DeclaringType, target);

		CopyReturnType(source, target);

		return target;
	}

	private TypeDefinition ImportType(Type type)
	{
		var assembly = type.Assembly;

		var sourceModule = RegisterSourceModule(assembly);

		var sourceType = sourceModule.GetType(type.GetFullName());

		var importedTypeDefinition = ImportTypeDefinition(sourceType);

		if (importedTypeDefinition == null)
		{
			throw new InvalidOperationException("Did not find type " + type.GetFullName() + " in module " + sourceModule.FileName);
		}

		return ProcessDeferredActions(importedTypeDefinition);
	}

	[return: NotNullIfNotNull("sourceType")]
	private TypeDefinition ImportTypeDefinition(TypeDefinition sourceType)
	{
		// ReSharper disable once ConditionIsAlwaysTrueOrFalse
		if (sourceType == null)
		{
			return null;
		}

		if (_targetTypesBySource.TryGetValue(sourceType, out var targetType))
		{
			return targetType;
		}

		if (_targetTypes.Contains(sourceType))
		{
			return sourceType;
		}

		if (IsLocalOrExternalReference(sourceType))
		{
			return sourceType;
		}

		RegisterSourceModule(sourceType.Module);

		string DecorateNamespace(TypeDefinition type)
		{
			return type.IsNested ? type.Namespace : NamespaceDecorator(type.Namespace);
		}

		targetType = new TypeDefinition(DecorateNamespace(sourceType), sourceType.Name, sourceType.Attributes)
		{
			ClassSize = sourceType.ClassSize,
			PackingSize = sourceType.PackingSize
		};

		_targetTypesBySource.Add(sourceType, targetType);
		_targetTypes.Add(targetType);

		targetType.DeclaringType = ImportTypeDefinition(sourceType.DeclaringType);

		CopyGenericParameters(sourceType, targetType);

		targetType.BaseType = InternalImportType(sourceType.BaseType, null);

		foreach (var sourceTypeInterface in sourceType.Interfaces)
		{
			var targetInterface = new InterfaceImplementation(InternalImportType(sourceTypeInterface.InterfaceType, null));
			CopyAttributes(sourceTypeInterface, targetInterface);
			targetType.Interfaces.Add(targetInterface);
		}

		if (targetType.IsNested)
		{
			targetType.DeclaringType.NestedTypes.Add(targetType);
		}
		else
		{
			if (HideImportedTypes)
			{
				targetType.IsPublic = false;
			}

			TargetModule.Types.Add(targetType);
		}

		CopyFields(sourceType, targetType);
		CopyMethods(sourceType, targetType);
		CopyProperties(sourceType, targetType);
		CopyEvents(sourceType, targetType);
		CopyAttributes(sourceType, targetType);

		return targetType;
	}

	private TypeReference ImportTypeReference(TypeReference source, MethodReference targetMethod)
	{
		Debug.Assert(source.GetType() == typeof(TypeReference));

		if (IsLocalOrExternalReference(source))
		{
			return TargetModule.ImportReference(source);
		}

		var typeDefinition = source.Resolve();

		if (typeDefinition == null)
		{
			throw new InvalidOperationException($"Unable to resolve type {source}");
		}

		return InternalImportType(typeDefinition, targetMethod);
	}

	[return: NotNullIfNotNull("source")]
	private TypeReference InternalImportType(TypeReference source, MethodReference targetMethod)
	{
		switch (source)
		{
			case null:
				return null;

			case TypeDefinition typeDefinition:
				return ImportTypeDefinition(typeDefinition);

			case GenericParameter genericParameter:
				return ImportGenericParameter(genericParameter, targetMethod);

			case GenericInstanceType genericInstanceType:
				return ImportGenericInstanceType(genericInstanceType, targetMethod);

			case ByReferenceType byReferenceType:
				return new ByReferenceType(InternalImportType(byReferenceType.ElementType, targetMethod));

			case ArrayType arrayType:
				return new ArrayType(InternalImportType(arrayType.ElementType, targetMethod), arrayType.Rank);

			case RequiredModifierType requiredModifierType:
				return new RequiredModifierType(InternalImportType(requiredModifierType.ModifierType, targetMethod), InternalImportType(requiredModifierType.ElementType, targetMethod));

			default:
				return ImportTypeReference(source, targetMethod);
		}
	}

	[return: NotNullIfNotNull("target")]
	private T ProcessDeferredActions<T>(T target)
		where T : class
	{
		while (true)
		{
			var action = _deferredActions.FirstOrDefault();
			if (action == null)
			{
				break;
			}

			_deferredActions.RemoveAt(0);
			action();
		}

		return target;
	}

	private ModuleDefinition RegisterSourceModule(Assembly assembly)
	{
		var assemblyName = assembly.FullName;

		if (_sourceModuleDefinitions.TryGetValue(assemblyName, out var sourceModule) && (sourceModule != null))
		{
			return sourceModule;
		}

		var fileName = new Uri(assembly.CodeBase, UriKind.Absolute).LocalPath;
		if (string.IsNullOrEmpty(fileName))
		{
			throw new InvalidOperationException("Unable get location of assembly " + assembly);
		}

		sourceModule = ModuleDefinition.ReadModule(fileName, new ReaderParameters { AssemblyResolver = AssemblyResolver });

		try
		{
			sourceModule.ReadSymbols();
		}
		catch
		{
			// module has no symbols, just go without...
		}

		_sourceModuleDefinitions[assemblyName] = sourceModule;

		// ReSharper disable once AssignNullToNotNullAttribute
		return sourceModule;
	}

	private bool ResolveModule(TypeReference typeReference, string assemblyName)
	{
		var module = ModuleResolver?.Resolve(typeReference, assemblyName);

		if (module == null)
		{
			return false;
		}

		RegisterSourceModule(module);
		return true;
	}

	#endregion

	#region Enumerations

	private enum Priority
	{
		Instructions,
		Operands
	}

	#endregion
}

internal static class CodeImporterExtensions
{
	#region Methods

	public static void ILMerge(this CodeImporter codeImporter)
	{
		var module = codeImporter.TargetModule;

		var existingTypes = module.GetTypes()
			.Except(codeImporter.ListImportedTypes(true).Values)
			.ToList();

		MergeAttributes(codeImporter, module);
		MergeAttributes(codeImporter, module.Assembly);

		foreach (var typeDefinition in existingTypes)
		{
			MergeAttributes(codeImporter, typeDefinition);
			MergeGenericParameters(codeImporter, typeDefinition);

			typeDefinition.BaseType = codeImporter.ImportType(typeDefinition.BaseType, null);

			if (typeDefinition.HasInterfaces)
			{
				foreach (var interfaceImplementation in typeDefinition.Interfaces)
				{
					MergeAttributes(codeImporter, interfaceImplementation);
					interfaceImplementation.InterfaceType = codeImporter.ImportType(interfaceImplementation.InterfaceType, null);
				}
			}

			if (typeDefinition.HasFields)
			{
				foreach (var fieldDefinition in typeDefinition.Fields)
				{
					MergeAttributes(codeImporter, fieldDefinition);
					fieldDefinition.FieldType = codeImporter.ImportType(fieldDefinition.FieldType, null);
				}
			}

			if (typeDefinition.HasEvents)
			{
				foreach (var eventDefinition in typeDefinition.Events)
				{
					MergeAttributes(codeImporter, eventDefinition);
					eventDefinition.EventType = codeImporter.ImportType(eventDefinition.EventType, null);
				}
			}

			if (typeDefinition.HasProperties)
			{
				foreach (var propertyDefinition in typeDefinition.Properties)
				{
					MergeAttributes(codeImporter, propertyDefinition);

					propertyDefinition.PropertyType = codeImporter.ImportType(propertyDefinition.PropertyType, null);

					if (!propertyDefinition.HasParameters)
					{
						continue;
					}

					foreach (var parameter in propertyDefinition.Parameters)
					{
						MergeAttributes(codeImporter, parameter);
						parameter.ParameterType = codeImporter.ImportType(parameter.ParameterType, null);
					}
				}
			}

			if (typeDefinition.HasMethods)
			{
				foreach (var methodDefinition in typeDefinition.Methods)
				{
					MergeAttributes(codeImporter, methodDefinition);
					MergeGenericParameters(codeImporter, methodDefinition);

					methodDefinition.ReturnType = codeImporter.ImportType(methodDefinition.ReturnType, methodDefinition);

					if (methodDefinition.HasOverrides)
					{
						foreach (var methodOverride in methodDefinition.Overrides)
						{
							if (methodOverride is MethodDefinition)
							{
								if (!codeImporter.IsLocalOrExternalReference(methodOverride.DeclaringType))
								{
									throw new NotImplementedException("Method overrides using MethodDefinition is not supported");
								}
							}
							else
							{
								MergeMethodReference(codeImporter, methodOverride, methodDefinition);
							}
						}
					}

					if (methodDefinition.HasParameters)
					{
						foreach (var parameter in methodDefinition.Parameters)
						{
							MergeAttributes(codeImporter, parameter);
							parameter.ParameterType = codeImporter.ImportType(parameter.ParameterType, methodDefinition);
						}
					}

					var methodBody = methodDefinition.Body;
					if (methodBody == null)
					{
						continue;
					}

					if (methodBody.HasVariables)
					{
						foreach (var variable in methodBody.Variables)
						{
							variable.VariableType = codeImporter.ImportType(variable.VariableType, methodDefinition);
						}
					}

					foreach (var instruction in methodBody.Instructions)
					{
						switch (instruction.Operand)
						{
							case MethodDefinition _:
								break;

							case GenericInstanceMethod genericInstanceMethod:
								instruction.Operand = codeImporter.Import(genericInstanceMethod);
								break;

							case MethodReference methodReference:
								MergeMethodReference(codeImporter, methodReference, methodDefinition);
								break;

							case TypeDefinition _:
								break;

							case TypeReference typeReference:
								instruction.Operand = codeImporter.ImportType(typeReference, methodDefinition);
								break;

							case FieldReference fieldReference:
								fieldReference.FieldType = codeImporter.ImportType(fieldReference.FieldType, methodDefinition);
								fieldReference.DeclaringType = codeImporter.ImportType(fieldReference.DeclaringType, methodDefinition);
								break;
						}
					}

					if (methodBody.HasExceptionHandlers)
					{
						foreach (var exceptionHandler in methodBody.ExceptionHandlers)
						{
							exceptionHandler.CatchType = codeImporter.ImportType(exceptionHandler.CatchType, methodDefinition);
						}
					}
				}
			}
		}

		var importedAssemblyNames = new HashSet<string>(codeImporter.ListImportedModules().Select(m => m.Assembly.FullName));

		module.AssemblyReferences.RemoveAll(ar => importedAssemblyNames.Contains(ar.FullName));
	}

	private static void MergeAttributes(CodeImporter codeImporter, ICustomAttributeProvider attributeProvider)
	{
		if (attributeProvider?.HasCustomAttributes != true)
		{
			return;
		}

		foreach (var attribute in attributeProvider.CustomAttributes)
		{
			attribute.Constructor = codeImporter.ImportMethod(attribute.Constructor);

			if (attribute.HasConstructorArguments)
			{
				attribute.ConstructorArguments.ReplaceItems(arg => new CustomAttributeArgument(codeImporter.ImportType(arg.Type, null), arg.Value));
			}

			if (attribute.HasFields)
			{
				attribute.Fields.ReplaceItems(arg => new CustomAttributeNamedArgument(arg.Name, new CustomAttributeArgument(codeImporter.ImportType(arg.Argument.Type, null), arg.Argument.Value)));
			}

			if (attribute.HasProperties)
			{
				attribute.Properties.ReplaceItems(arg => new CustomAttributeNamedArgument(arg.Name, new CustomAttributeArgument(codeImporter.ImportType(arg.Argument.Type, null), arg.Argument.Value)));
			}
		}
	}

	private static void MergeGenericParameters(CodeImporter codeImporter, IGenericParameterProvider provider)
	{
		if (provider?.HasGenericParameters != true)
		{
			return;
		}

		foreach (var parameter in provider.GenericParameters)
		{
			foreach (var constraint in parameter.Constraints)
			{
				constraint.ConstraintType = codeImporter.ImportType(constraint.ConstraintType, provider as MethodReference);
			}
		}
	}

	private static void MergeMethodReference(CodeImporter codeImporter, MethodReference methodReference, MethodDefinition methodDefinition)
	{
		methodReference.DeclaringType = codeImporter.ImportType(methodReference.DeclaringType, methodDefinition);
		methodReference.ReturnType = codeImporter.ImportType(methodReference.ReturnType, methodDefinition);

		if (methodReference.HasParameters)
		{
			foreach (var parameter in methodReference.Parameters)
			{
				parameter.ParameterType = codeImporter.ImportType(parameter.ParameterType, methodDefinition);
			}
		}

		if (methodReference.HasGenericParameters)
		{
			MergeGenericParameters(codeImporter, methodReference);
		}
	}

	#endregion
}

internal interface IModuleResolver
{
	#region Methods

	ModuleDefinition Resolve(TypeReference typeReference, string assemblyName);

	#endregion
}

internal class AssemblyModuleResolver : IModuleResolver
{
	#region Fields

	private readonly HashSet<string> _assemblyNames;

	#endregion

	#region Constructors

	public AssemblyModuleResolver(params Assembly[] assemblies)
	{
		_assemblyNames = new HashSet<string>(assemblies.Select(a => a.FullName));
	}

	public AssemblyModuleResolver(params string[] assemblyNames)
	{
		_assemblyNames = new HashSet<string>(assemblyNames);
	}

	#endregion

	#region Methods

	public ModuleDefinition Resolve(TypeReference typeReference, string assemblyName)
	{
		return _assemblyNames.Contains(assemblyName) ? typeReference.Resolve()?.Module : null;
	}

	#endregion
}

internal class LocalReferenceModuleResolver : IModuleResolver
{
	#region Fields

	private readonly HashSet<string> _ignoredAssemblyNames = [];

	#endregion

	#region Methods

	public ModuleDefinition Resolve(TypeReference typeReference, string assemblyName)
	{
		if (_ignoredAssemblyNames.Contains(assemblyName))
		{
			return null;
		}

		try
		{
			var typeDefinition = typeReference.Resolve();
			if (typeDefinition != null)
			{
				var module = typeDefinition.Module;
				var moduleFileName = module.FileName;

				if (Path.GetDirectoryName(moduleFileName) == @".")
				{
					return module;
				}
			}
		}
		catch (AssemblyResolutionException)
		{
			// fall through...
		}

		_ignoredAssemblyNames.Add(assemblyName);
		return null;
	}

	#endregion
}