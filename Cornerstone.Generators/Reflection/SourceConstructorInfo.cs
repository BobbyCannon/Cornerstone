#region References

using System;
using System.Reflection;

#endregion

namespace Cornerstone.Reflection;

[SourceReflection]
public partial class SourceConstructorInfo
{
	#region Fields

	private Func<object[], object> _invoke;

	#endregion

	#region Properties

	public SourceAccessibility Accessibility { get; set; }
	public SourceAttributeInfo[] Attributes { get; set; }

	public ConstructorInfo ConstructorInfo { get; set; }

	public Func<object[], object> Invoke
	{
		get => _invoke ?? ConstructorInfo.Invoke;
		set => _invoke = value;
	}

	public bool IsDependencyConstructor { get; set; }
	public bool IsStatic { get; set; }
	public string Name { get; set; }
	public SourceParameterInfo[] Parameters { get; set; }

	#endregion
}