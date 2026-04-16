#region References

using System;
using System.Reflection;

#endregion

namespace Cornerstone.Reflection;

[SourceReflection]
public partial class SourceFieldInfo
{
	#region Fields

	private Func<object, object> _getMethod;
	private Action<object, object> _setMethod;

	#endregion

	#region Properties

	public SourceAccessibility Accessibility { get; set; }
	public SourceAttributeInfo[] Attributes { get; set; }

	public FieldInfo FieldInfo { get; set; }

	public Func<object, object> GetValue
	{
		get => _getMethod ?? FieldInfo.GetValue;
		set => _getMethod = value;
	}

	public bool IsConstant { get; set; }
	public bool IsReadOnly { get; set; }
	public bool IsRequired { get; set; }
	public bool IsStatic { get; set; }
	public string Name { get; set; }
	public SourceNullableAnnotation NullableAnnotation { get; set; }

	public Action<object, object> SetValue
	{
		get => _setMethod ?? FieldInfo.SetValue;
		set => _setMethod = value;
	}

	#endregion
}