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

	/// <summary>
	/// Union namespaces and type names. An existing type keeps its members and gains any hint values it did not already have.
	/// </summary>
	public void MergeFrom(Metadata metadata)
	{
		if (metadata == null)
		{
			return;
		}

		foreach (var ns in metadata._namespaces)
		{
			Dictionary<string, MetadataType> mine;
			if (!_namespaces.TryGetValue(ns.Key, out mine))
			{
				mine = new Dictionary<string, MetadataType>();
				_namespaces.Add(ns.Key, mine);
			}

			foreach (var pair in ns.Value)
			{
				MetadataType existing;
				if (!mine.TryGetValue(pair.Key, out existing))
				{
					mine.Add(pair.Key, pair.Value);
					if (!string.IsNullOrEmpty(pair.Value.FullName) && !_inverseNamespace.ContainsKey(pair.Value.FullName))
					{
						_inverseNamespace.Add(pair.Value.FullName, ns.Key);
					}

					continue;
				}

				UnionHints(existing, pair.Value);
			}
		}
	}

	private static void UnionHints(MetadataType target, MetadataType source)
	{
		if ((source.HintValues == null) || (source.HintValues.Length == 0))
		{
			return;
		}

		if ((target.HintValues == null) || (target.HintValues.Length == 0))
		{
			target.HintValues = source.HintValues;
			target.HasHintValues = source.HasHintValues;
			return;
		}

		var seen = new HashSet<string>(target.HintValues);
		var extra = new List<string>();
		for (var i = 0; i < source.HintValues.Length; i++)
		{
			var hint = source.HintValues[i];
			if ((hint != null) && seen.Add(hint))
			{
				extra.Add(hint);
			}
		}

		if (extra.Count == 0)
		{
			return;
		}

		var merged = new string[target.HintValues.Length + extra.Count];
		Array.Copy(target.HintValues, merged, target.HintValues.Length);
		for (var i = 0; i < extra.Count; i++)
		{
			merged[target.HintValues.Length + i] = extra[i];
		}

		target.HintValues = merged;
		target.HasHintValues = true;
	}

	public void AddType(string ns, MetadataType type)
	{
		_namespaces.GetOrCreate(ns)[type.Name] = type;
		_inverseNamespace[type.FullName] = ns;
	}

	/// <summary>
	/// True when the type is published under the given XML namespace.
	/// Missing namespaces return false; does not throw.
	/// </summary>
	public bool ContainsTypeInXmlns(string xmlNamespace, string typeName)
	{
		if (string.IsNullOrEmpty(xmlNamespace) || string.IsNullOrEmpty(typeName))
		{
			return false;
		}

		return _namespaces.TryGetValue(xmlNamespace, out var types)
			&& (types != null)
			&& types.ContainsKey(typeName);
	}

	/// <summary>
	/// True when the type is in a default XAML xmlns (Avalonia or Cornerstone).
	/// Those controls do not need a prefix or extra xmlns mapping.
	/// </summary>
	public bool IsTypeInDefaultXmlns(string typeName)
	{
		return ContainsTypeInXmlns(Utils.AvaloniaNamespace, typeName)
			|| ContainsTypeInXmlns(Utils.CornerstoneNamespace, typeName);
	}

	#endregion
}

// todo: add property for permutation annotation. A MetadataType may be defined in multiple build contexts, but have different definitions.
[DebuggerDisplay("{Name}")]
public record MetadataType(string Name)
{
	#region Properties

	public string? AssemblyQualifiedName { get; set; }
	public string BaseTypeFullName { get; set; }
	public List<MetadataEvent> Events { get; set; } = [];
	public string FullName { get; set; } = "";
	public bool HasAttachedEvents { get; set; }
	public bool HasAttachedProperties { get; set; }
	public bool HasHintValues { get; set; }
	public bool HasPseudoClasses { get; set; }
	public bool HasSetProperties { get; set; }
	public bool HasStaticGetProperties { get; set; }
	public string[]? HintValues { get; set; }
	public bool IsAbstract { get; set; } = false;
	public bool IsAvaloniaObjectType { get; set; }
	public bool IsCompositeValue { get; set; }
	public bool IsEnum { get; set; }
	public bool IsGeneric { get; set; }
	public bool IsMarkupExtension { get; set; }
	public bool IsNullable { get; set; }
	public bool IsStatic { get; set; }

	//assembly, type, property
	public Func<string?, MetadataType, MetadataProperty?, bool>? IsValidForXamlContextFunc { get; set; }
	public bool IsXamlDirective { get; set; }
	public List<MetadataProperty> Properties { get; set; } = [];

	public string[] PseudoClasses { get; set; } = [];
	public MetadataTypeCtorArgument SupportCtorArgument { get; set; }
	public List<(MetadataType Type, string Name)> TemplateParts { get; set; } = [];

	public MetadataType? UnderlyingType { get; set; }

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