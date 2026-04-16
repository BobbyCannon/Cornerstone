#region References

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

#endregion

namespace Cornerstone.Generators.Models;

public class SourcePropertyInfo : Reflection.SourcePropertyInfo
{
	#region Constructors

	public SourcePropertyInfo()
	{
		Parameters = [];
	}

	#endregion

	#region Properties

	public new Accessibility Accessibility { get; set; }
	public new List<SourceAttributeInfo> Attributes { get; } = [];
	public string FullyQualifiedName { get; set; }
	public Accessibility GetMethodAccessibility { get; set; }
	public string GlobalFullyQualifiedName { get; set; }
	public bool IsPartial { get; set; }
	public List<SourceParameterInfo> Parameters { get; }
	public IPropertySymbol PropertySymbol { get; set; }
	public Accessibility SetMethodAccessibility { get; set; }

	#endregion
}