#region References

using System;
using System.Reflection;

#endregion

namespace Cornerstone.Reflection;

[SourceReflection]
public class SourceMethodInfo : SourceMemberInfo
{
	#region Fields

	public string FullyQualifiedTypeName;

	private readonly Func<object, object[], object> _invoke;
	private Type _returnType;

	#endregion

	#region Constructors

	public SourceMethodInfo()
	{
		Parameters = [];
	}

	#endregion

	#region Properties

	public SourceAccessibility Accessibility { get; init; }

	public Func<object, object[], object> Invoke
	{
		get =>
			_invoke
			?? (MethodInfo.IsGenericMethodDefinition
				? throw new InvalidOperationException("This method has a generic parameter. Please use MethodInfo.Invoke instead.")
				: MethodInfo.Invoke);
		init => _invoke = value;
	}

	public bool IsAbstract { get; init; }
	
	public bool IsGenericMethod { get; init; }
	
	public bool IsOverride { get; init; }

	public bool IsStatic { get; init; }
	
	public bool IsVirtual { get; init; }

	public MethodInfo MethodInfo { get; init; }

	public SourceParameterInfo[] Parameters { get; init; }

	public Type ReturnType
	{
		get => _returnType ??= MethodInfo.ReturnType;
		init => _returnType = value;
	}

	#endregion
}