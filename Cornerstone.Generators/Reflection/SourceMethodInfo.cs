#region References

using System;
using System.Reflection;

#endregion

namespace Cornerstone.Reflection;

[SourceReflection]
public partial class SourceMethodInfo
{
	#region Fields

	private Func<object, object[], object> _invoke;
	private Type _returnType;

	#endregion

	#region Constructors

	public SourceMethodInfo()
	{
		Parameters = [];
	}

	#endregion

	#region Properties

	public SourceAccessibility Accessibility { get; set; }
	public SourceAttributeInfo[] Attributes { get; set; }
	public string FullyQualifiedTypeName { get; set; }

	public Func<object, object[], object> Invoke
	{
		get =>
			_invoke
			?? (MethodInfo.IsGenericMethodDefinition
				? throw new InvalidOperationException("This method has a generic parameter. Please use MethodInfo.Invoke instead.")
				: MethodInfo.Invoke);
		set => _invoke = value;
	}

	public bool IsAbstract { get; set; }
	public bool IsGenericMethod { get; set; }
	public bool IsOverride { get; set; }
	public bool IsStatic { get; set; }
	public bool IsVirtual { get; set; }
	public MethodInfo MethodInfo { get; set; }
	public string Name { get; set; }
	public SourceParameterInfo[] Parameters { get; set; }

	public Type ReturnType
	{
		get => _returnType ??= MethodInfo.ReturnType;
		set => _returnType = value;
	}

	#endregion
}