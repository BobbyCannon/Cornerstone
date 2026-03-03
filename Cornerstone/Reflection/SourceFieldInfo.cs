#region References

using System;
using System.Reflection;

#endregion

namespace Cornerstone.Reflection;

[SourceReflection]
public class SourceFieldInfo : SourceMemberInfo
{
	#region Fields

	private readonly Func<object, object> _getMethod;
	private readonly Action<object, object> _setMethod;

	#endregion

	#region Properties

	public SourceAccessibility Accessibility { get; init; }

	public FieldInfo FieldInfo { get; init; }

	public Func<object, object> GetValue
	{
		get => _getMethod ?? FieldInfo.GetValue;
		init => _getMethod = value;
	}

	public bool IsConstant { get; init; }

	public bool IsReadOnly { get; init; }

	public bool IsRequired { get; init; }

	public bool IsStatic { get; init; }

	public SourceNullableAnnotation NullableAnnotation { get; init; }

	public Action<object, object> SetValue
	{
		get => _setMethod ?? FieldInfo.SetValue;
		init => _setMethod = value;
	}

	#endregion
}