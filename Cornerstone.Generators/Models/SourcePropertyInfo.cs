#region References

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

#endregion

namespace Cornerstone.Generators.Models;

public class SourcePropertyInfo : SourceMemberInfo
{
	#region Constructors

	public SourcePropertyInfo()
	{
		Parameters = [];
	}

	#endregion

	#region Properties

	public Accessibility Accessibility { get; set; }
	public bool CanRead { get; set; }
	public bool CanWrite { get; set; }
	public string FullyQualifiedName { get; set; }
	public Accessibility GetMethodAccessibility { get; set; }
	public string GlobalFullyQualifiedName { get; set; }
	public bool IsAbstract { get; set; }
	public bool IsDependencyInjected { get; set; }
	public bool IsIndexer { get; set; }
	public bool IsInitOnly { get; set; }
	public bool IsPartial { get; set; }
	public bool IsReadOnly { get; set; }
	public bool IsRequired { get; set; }
	public bool IsStatic { get; set; }
	public bool IsVirtual { get; set; }
	public List<SourceParameterInfo> Parameters { get; }
	public IPropertySymbol PropertySymbol { get; set; }
	public Accessibility SetMethodAccessibility { get; set; }

	#endregion
}