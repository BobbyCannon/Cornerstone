#region References

using System;
using System.Linq;
using System.Reflection;

#endregion

namespace Cornerstone.Reflection;

[SourceReflection]
public class SourcePropertyInfo : SourceMemberInfo
{
	#region Fields

	private readonly Func<object, object[], object> _getIndexerMethod;
	private readonly Func<object, object> _getMethod;
	private readonly Action<object, object, object[]> _setIndexerMethod;
	private readonly Action<object, object> _setMethod;

	#endregion

	#region Properties

	public SourceAccessibility Accessibility { get; init; }
	public SourceAccessibility AccessibilityForGet { get; init; }
	public SourceAccessibility AccessibilityForSet { get; init; }
	public bool CanRead { get; init; }
	public bool CanWrite { get; init; }

	public Func<object, object[], object> GetIndexerValue
	{
		get => _getIndexerMethod ?? PropertyInfo.GetValue;
		init => _getIndexerMethod = value;
	}

	public Func<object, object> GetValue
	{
		get => _getMethod ?? PropertyInfo.GetValue;
		init => _getMethod = value;
	}

	public SourceParameterInfo[] IndexerParameters { get; init; } = [];
	public bool IsAbstract { get; init; }
	public bool IsDependencyInjected { get; init; }
	public bool IsIndexer { get; init; }
	public bool IsInitOnly { get; init; }
	public bool IsReadOnly { get; init; }
	public bool IsRequired { get; init; }
	public bool IsStatic { get; init; }
	public bool IsVirtual { get; init; }
	public PropertyInfo PropertyInfo { get; init; }

	public Action<object, object, object[]> SetIndexerValue
	{
		get => _setIndexerMethod ?? PropertyInfo.SetValue;
		init => _setIndexerMethod = value;
	}

	public Action<object, object> SetValue
	{
		get => _setMethod ?? PropertyInfo.SetValue;
		init => _setMethod = value;
	}

	#endregion

	#region Methods

	public T GetAttribute<T>()
	{
		var attributeInfo = Attributes.FirstOrDefault(x => x.Type == typeof(T));
		if (attributeInfo == null)
		{
			return default;
		}
		var response = (T) SourceReflector.CreateInstance(attributeInfo.Type, attributeInfo.ConstructorArguments);
		var attributeType = SourceReflector.GetRequiredSourceType(attributeInfo.Type);
		
		foreach (var named in attributeInfo.NamedArguments)
		{
			var property = attributeType.GetProperty(named.Key);
			if (property is { CanWrite: true })
			{
				property.PropertyInfo.SetValue(response, named.Value);
			}
		}

		return response;
	}

	#endregion
}