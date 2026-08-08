#region References

using System;
using System.Collections.Generic;
using System.Diagnostics;

#endregion

namespace Cornerstone.VisualStudio.Core.AssemblyMetadata;

public class Metadata
{
	#region Fields

	private readonly Dictionary<string, string> _inverseNamespace;
	private readonly Dictionary<string, Dictionary<string, MetadataType>> _namespaces;

	#endregion

	#region Constructors

	public Metadata()
	{
		_inverseNamespace = new();
		_namespaces = new();
	}

	#endregion

	#region Properties

	public IReadOnlyDictionary<string, string> InverseNamespace => _inverseNamespace;

	public IReadOnlyDictionary<string, Dictionary<string, MetadataType>> Namespaces => _namespaces;

	#endregion

	#region Methods

	/// <summary>
	/// Add new metadata. Keys are added and existing keys are unchanged.
	/// </summary>
	public void AddMetadata(Metadata metadata)
	{
		foreach (var x in metadata._namespaces)
		{
			if (!_namespaces.ContainsKey(x.Key))
			{
				_namespaces.Add(x.Key, x.Value);
			}
		}
		foreach (var x in metadata._inverseNamespace)
		{
			if (!_inverseNamespace.ContainsKey(x.Key))
			{
				_inverseNamespace.Add(x.Key, x.Value);
			}
		}
	}

	public void AddType(string ns, MetadataType type)
	{
		_namespaces.GetOrCreate(ns)[type.Name] = type;
		_inverseNamespace[type.FullName] = ns;
	}

	#endregion
}

// todo: add property for permutation annotation. A MetadataType may be defined in multiple build contexts, but have different definitions.
[DebuggerDisplay("{Name}")]
public record MetadataType(string Name)
{
	#region Properties

	public string? AssemblyQualifiedName { get; set; }
	public List<MetadataEvent> Events { get; set; } = [];
	public string FullName { get; set; } = "";
	public bool HasAttachedEvents { get; set; }
	public bool HasAttachedProperties { get; set; }
	public bool HasHintValues { get; set; }
	public bool HasPseudoClasses { get; set; }
	public bool HasSetProperties { get; set; }
	public bool HasStaticGetProperties { get; set; }
	public string[]? HintValues { get; set; }
	public bool IsAbstract { get; internal set; } = false;
	public bool IsAvaloniaObjectType { get; set; }
	public bool IsCompositeValue { get; set; }
	public bool IsEnum { get; set; }
	public bool IsGeneric { get; set; }
	public bool IsMarkupExtension { get; set; }
	public bool IsNullable { get; init; }
	public bool IsStatic { get; set; }

	//assembly, type, property
	public Func<string?, MetadataType, MetadataProperty?, bool>? IsValidForXamlContextFunc { get; set; }
	public bool IsXamlDirective { get; set; }
	public List<MetadataProperty> Properties { get; set; } = [];

	public string[] PseudoClasses { get; set; } = [];
	public MetadataTypeCtorArgument SupportCtorArgument { get; set; }
	public List<(MetadataType Type, string Name)> TemplateParts { get; set; } = [];

	public MetadataType? UnderlyingType { get; init; }

	//assembly, type, property
	public Func<string?, MetadataType, MetadataProperty?, IEnumerable<string>>? XamlContextHintValuesFunc { get; set; }

	#endregion
}

public enum MetadataTypeCtorArgument
{
	None,
	Type,
	Object,
	TypeAndObject,
	HintValues
}

[DebuggerDisplay("{Name} from {DeclaringType}")]
public record MetadataProperty(string Name, MetadataType? Type, MetadataType? DeclaringType, bool IsAttached, bool IsStatic, bool HasGetter, bool HasSetter);

public record MetadataEvent(string Name, MetadataType? Type, MetadataType? DeclaringType, bool IsAttached);