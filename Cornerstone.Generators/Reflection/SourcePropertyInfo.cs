#region References

using System;
using System.Reflection;

#endregion

namespace Cornerstone.Reflection;

[SourceReflection]
public partial class SourcePropertyInfo
{
	#region Fields

	private Func<object, object[], object> _getIndexerMethod;
	private Func<object, object> _getMethod;
	private Action<object, object, object[]> _setIndexerMethod;
	private Action<object, object> _setMethod;

	#endregion

	#region Properties

	public SourceAccessibility Accessibility { get; set; }
	public SourceAccessibility AccessibilityForGet { get; set; }
	public SourceAccessibility AccessibilityForSet { get; set; }
	public SourceAttributeInfo[] Attributes { get; set; }
	public bool CanRead { get; set; }
	public bool CanWrite { get; set; }

	public Func<object, object[], object> GetIndexerValue
	{
		get => _getIndexerMethod ?? PropertyInfo.GetValue;
		set => _getIndexerMethod = value;
	}

	public Func<object, object> GetValue
	{
		get => _getMethod ?? PropertyInfo.GetValue;
		set => _getMethod = value;
	}

	public SourceParameterInfo[] IndexerParameters { get; set; } = [];
	public bool IsAbstract { get; set; }
	public bool IsDependencyInjected { get; set; }
	public bool IsIndexer { get; set; }
	public bool IsInitOnly { get; set; }
	public bool IsReadOnly { get; set; }
	public bool IsRequired { get; set; }
	public bool IsStatic { get; set; }
	public bool IsVirtual { get; set; }
	public string Name { get; set; }
	public PropertyInfo PropertyInfo { get; set; }

	public Action<object, object, object[]> SetIndexerValue
	{
		get => _setIndexerMethod ?? PropertyInfo.SetValue;
		set => _setIndexerMethod = value;
	}

	public Action<object, object> SetValue
	{
		get => _setMethod ?? PropertyInfo.SetValue;
		set => _setMethod = value;
	}

	#endregion
}