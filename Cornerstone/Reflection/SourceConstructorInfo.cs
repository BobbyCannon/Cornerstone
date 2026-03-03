#region References

using System;
using System.Reflection;

#endregion

namespace Cornerstone.Reflection;

[SourceReflection]
public class SourceConstructorInfo : SourceMemberInfo
{
	#region Fields

	private readonly Func<object[], object> _invoke;

	#endregion

	#region Properties

	public SourceAccessibility Accessibility { get; init; }

	public ConstructorInfo ConstructorInfo { get; init; }

	public Func<object[], object> Invoke
	{
		get => _invoke ?? ConstructorInfo.Invoke;
		init => _invoke = value;
	}

	public bool IsDependencyConstructor { get; init; }

	public bool IsStatic { get; init; }

	public SourceParameterInfo[] Parameters { get; init; }

	#endregion
}